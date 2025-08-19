using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Options;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Positions;
using ReportGroups.Blazor.Models;
using ReportGroups.Blazor.Models.Groups;
using ReportGroups.Blazor.Models.Nodes;
using ReportGroups.Blazor.Components;
using ReportGroups.Blazor.Behaviors;

namespace ReportGroups.Blazor.Pages
{
    public partial class Home : ComponentBase
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
        
        private BlazorDiagram Diagram { get; set; } = null!;
        private Report? selectedReport = null;

        protected override void OnInitialized()
        {
            var options = new BlazorDiagramOptions
            {
                AllowMultiSelection = false,
                Zoom = 
                {
                    Enabled = false
                }
            };
            
            Diagram = new BlazorDiagram(options);
            
            // Force single selection by monitoring selection changes
            Diagram.SelectionChanged += (selectedModel) =>
            {
                if (selectedModel != null && selectedModel.Selected)
                {
                    var selectedModels = Diagram.GetSelectedModels().ToList();
                    if (selectedModels.Count > 1)
                    {
                        // More than one selected, keep only the last selected one
                        foreach (var model in selectedModels.Where(m => m != selectedModel))
                        {
                            Diagram.UnselectModel(model);
                        }
                    }
                }
            };
            
            Diagram.RegisterComponent<ReportGroup, ReportGroupWidget>();
            Diagram.RegisterComponent<HeaderNode, HeaderNodeWidget>();
            Diagram.RegisterComponent<ColumnNode, ColumnNodeWidget>();
            
            // Register custom drag behavior for header nodes
            Diagram.RegisterBehavior(new HeaderNodeDragBehavior(Diagram));
            
            // Handle single click on column nodes for expand/collapse
            Diagram.PointerClick += (model, args) =>
            {
                if (model is ColumnNode clickedColumn)
                {
                    ToggleColumnExpansion(clickedColumn);
                }
            };
            
            // Handle double click on header nodes and column nodes
            Diagram.PointerDoubleClick += (model, args) =>
            {
                if (model is HeaderNode headerNode)
                {
                    // Find the group that contains this header node
                    var group = Diagram.Groups.OfType<ReportGroup>().FirstOrDefault(g => g.Children.Contains(headerNode));
                    if (group != null)
                    {
                        // Show alert with report ID
                        _ = ShowAlert($"Report ID: {group.Report.Id}");
                    }
                }
                else if (model is ColumnNode columnNode)
                {
                    // Show alert with position ID
                    _ = ShowAlert($"Position ID: {columnNode.ReportPosition?.Id}");
                }
            };
        }

        private async Task ShowAlert(string message)
        {
            await JSRuntime.InvokeVoidAsync("alert", message);
        }

        private void ToggleColumnExpansion(ColumnNode clickedColumn)
        {
            // Find the group that contains the clicked column
            var currentGroup = Diagram.Groups.OfType<ReportGroup>()
                .FirstOrDefault(g => g.Children.Contains(clickedColumn));
            if (currentGroup == null) return;

            var allColumnNodes = currentGroup.Children.OfType<ColumnNode>().OrderBy(c => c.OriginalIndex).ToList();
            
            // Collapse all other columns first
            foreach (var column in allColumnNodes)
            {
                if (column != clickedColumn)
                {
                    column.IsExpanded = false;
                }
            }

            // Toggle the clicked column
            clickedColumn.IsExpanded = !clickedColumn.IsExpanded;

            // Recalculate positions
            RecalculateColumnPositions(allColumnNodes, currentGroup);

            // Trigger UI refresh
            StateHasChanged();
        }


        private void RecalculateColumnPositions(List<ColumnNode> columnNodes, ReportGroup targetGroup)
        {
            if (targetGroup == null) return;

            var headerNode = targetGroup.Children.OfType<HeaderNode>().FirstOrDefault();
            if (headerNode == null) return;

            // Use the header node's position as reference
            var headerX = headerNode.Position?.X ?? 100;
            var headerY = headerNode.Position?.Y ?? 0;
            
            var normalSpacing = 50;
            var expandedHeight = 100; // Additional height for expanded node

            var currentY = headerY + normalSpacing; // Start after header

            for (int i = 0; i < columnNodes.Count; i++)
            {
                var column = columnNodes[i];
                column.SetPosition(headerX, currentY);

                // Calculate spacing for next node
                if (column.IsExpanded)
                {
                    currentY += normalSpacing + expandedHeight;
                }
                else
                {
                    currentY += normalSpacing;
                }
            }
        }

        private void OnReportClick(Report report)
        {
            selectedReport = report;
            
            // Clear existing groups, nodes AND links
            Diagram.Groups.Clear();
            Diagram.Nodes.Clear();
            Diagram.Links.Clear();
            
            // Create header node and column nodes for positions
            var centerX = 200; // Center of diagram
            var centerY = 300; // Lower starting position
            var nodeSpacing = 50; // Spacing between nodes
            
            var headerNode = new HeaderNode(new Point(centerX - 100, centerY - 200)) 
            { 
                ReportName = report.Name, 
                Locked = true 
            };
            
            var allNodes = new List<NodeModel> { headerNode };
            
            // Create column nodes for each position in the report
            for (int i = 0; i < report.Positions.Count; i++)
            {
                var position = report.Positions[i];
                var columnNode = new ColumnNode(new Point(centerX - 100, centerY - 200 + (i + 1) * nodeSpacing))
                {
                    ReportPosition = position,
                    OriginalIndex = i,
                    Locked = true
                };

                // Add port on the right side if this is an ExtendedPosition
                if (position is ExtendedPosition)
                {
                    columnNode.AddPort(PortAlignment.Right);
                }

                allNodes.Add(columnNode);
            }
            
            // Add nodes to diagram first before putting them in group
            foreach (var node in allNodes)
            {
                Diagram.Nodes.Add(node);
            }
            
            // Create new ReportGroup for the selected report
            var reportGroup = new ReportGroup(report, allNodes);
            Diagram.Groups.Add(reportGroup);

            // Create additional groups for ExtendedPositions with dynamic positioning
            var extendedGroups = new List<(ExtendedPosition position, ReportGroup group)>();
            var extendedPositions = report.Positions.OfType<ExtendedPosition>().ToList();

            // Define specific positions for each extended group
            var groupPositions = new[]
            {
                new { X = centerX + 400, Y = centerY - 350 }, // Top right
                new { X = centerX + 400, Y = centerY - 50 },  // Middle right
                new { X = centerX + 400, Y = centerY + 250 }  // Bottom right
            };

            for (int i = 0; i < extendedPositions.Count && i < groupPositions.Length; i++)
            {
                var position = extendedPositions[i];
                var groupPos = groupPositions[i];
                
                var extendedGroup = CreateExtendedPositionGroup(position, groupPos.X, groupPos.Y);
                extendedGroups.Add((position, extendedGroup));
            }

            // Create links between ExtendedPosition ports and their corresponding group headers
            CreateLinksToExtendedGroups(reportGroup, extendedGroups);

            // Recursively create groups for nested ExtendedPositions
            CreateNestedExtendedGroups(extendedGroups);
        }

        private ReportGroup CreateExtendedPositionGroup(ExtendedPosition extendedPosition, int centerX, int centerY)
        {
            var nodeSpacing = 50; // Spacing between nodes
            
            // Create header node for the extended position
            var headerNode = new HeaderNode(new Point(centerX - 100, centerY - 200))
            {
                ReportName = extendedPosition.Name,
                Locked = true
            };

            // Add port on the left side of the header node for ExtendedPosition groups
            headerNode.AddPort(PortAlignment.Left);

            var allNodes = new List<NodeModel> { headerNode };

            // Create column nodes for each sub-position in the extended position
            for (int i = 0; i < extendedPosition.Positions.Count; i++)
            {
                var position = extendedPosition.Positions[i];
                var columnNode = new ColumnNode(new Point(centerX - 100, centerY - 200 + (i + 1) * nodeSpacing))
                {
                    ReportPosition = position,
                    OriginalIndex = i,
                    Locked = true
                };

                // Add port if this sub-position is also an ExtendedPosition (recursion)
                if (position is ExtendedPosition)
                {
                    columnNode.AddPort(PortAlignment.Right);
                }

                allNodes.Add(columnNode);
            }

            // Add nodes to diagram
            foreach (var node in allNodes)
            {
                Diagram.Nodes.Add(node);
            }

            // Create a dummy report for the extended position group
            var dummyReport = new Report(extendedPosition.Name, extendedPosition.Id);
            var extendedGroup = new ReportGroup(dummyReport, allNodes);
            Diagram.Groups.Add(extendedGroup);

            return extendedGroup;
        }

        private void CreateLinksToExtendedGroups(ReportGroup mainGroup, List<(ExtendedPosition position, ReportGroup group)> extendedGroups)
        {
            foreach (var (extendedPosition, extendedGroup) in extendedGroups)
            {
                // Find the ColumnNode in main group that represents this ExtendedPosition
                var sourceColumnNode = mainGroup.Children.OfType<ColumnNode>()
                    .FirstOrDefault(cn => cn.ReportPosition == extendedPosition);
                
                if (sourceColumnNode == null) continue;

                // Find the HeaderNode in the extended group
                var targetHeaderNode = extendedGroup.Children.OfType<HeaderNode>().FirstOrDefault();
                if (targetHeaderNode == null) continue;

                // Create dynamic anchors for port positions (this worked before!)
                var sourceAnchor = new DynamicAnchor(sourceColumnNode, new[]
                {
                    new BoundsBasedPositionProvider(1, 0.5) // Right center (port position)
                });
                
                var targetAnchor = new DynamicAnchor(targetHeaderNode, new[]
                {
                    new BoundsBasedPositionProvider(0, 0.5) // Left center (port position)
                });

                // Create link that connects at port positions and follows nodes
                var link = Diagram.Links.Add(new LinkModel(sourceAnchor, targetAnchor));
                link.PathGenerator = new SmoothPathGenerator();
            }
        }

        private void CreateNestedExtendedGroups(List<(ExtendedPosition position, ReportGroup group)> parentGroups)
        {
            foreach (var (parentPosition, parentGroup) in parentGroups)
            {
                var nestedExtendedPositions = parentPosition.Positions.OfType<ExtendedPosition>().ToList();
                if (!nestedExtendedPositions.Any()) continue;

                var nestedGroups = new List<(ExtendedPosition position, ReportGroup group)>();

                for (int i = 0; i < nestedExtendedPositions.Count; i++)
                {
                    var nestedPosition = nestedExtendedPositions[i];
                    
                    // Position nested groups to the right of parent group
                    var parentHeaderNode = parentGroup.Children.OfType<HeaderNode>().FirstOrDefault();
                    if (parentHeaderNode == null) continue;

                    var nestedX = (int)(parentHeaderNode.Position?.X ?? 0) + 350; // Right of parent
                    var nestedY = (int)(parentHeaderNode.Position?.Y ?? 0) + (i * 150); // Stacked vertically

                    var nestedGroup = CreateExtendedPositionGroup(nestedPosition, nestedX, nestedY);
                    nestedGroups.Add((nestedPosition, nestedGroup));
                }

                // Create links from parent group to nested groups
                CreateLinksToExtendedGroups(parentGroup, nestedGroups);

                // Recursively handle further nesting
                CreateNestedExtendedGroups(nestedGroups);
            }
        }

        private List<Report> reportPackage = new List<Report>
        {
            new Report("GuV", "GUV001")
            {
                Positions = new List<PositionBase>
                {
                    new Position("Umsatzerlöse", "UE001", "Erlöse aus Verkäufen"),
                    new Position("Materialaufwand", "MA001", "Kosten für Rohstoffe"),
                    new Position("Personalkosten", "PK001", "Löhne und Gehälter"),
                    
                    // Verbindlichkeiten als ExtendedPosition
                    new ExtendedPosition("Verbindlichkeiten", "VB001", "Verbindlichkeiten Report", new List<PositionBase>
                    {
                        new Position("Lieferantenverbindlichkeiten", "LV001", "Offene Rechnungen"),
                        new Position("Bankverbindlichkeiten", "BV001", "Kredite und Darlehen"),
                        new Position("Steuerverbindlichkeiten", "SV001", "Zu zahlende Steuern"),
                        new ExtendedPosition("Prüfung", "PR001", "Prüfung", new List<PositionBase>
                        {
                            new Position("Prüfung Lieferanten", "PL001", "Prüfung der Lieferantenverbindlichkeiten"),
                            new Position("Prüfung Banken", "PB001", "Prüfung der Bankverbindlichkeiten"),
                            new Position("Prüfung Steuern", "PS001", "Prüfung der Steuerverbindlichkeiten"),
                        })
                    }),
                    
                    // Forderungen als ExtendedPosition
                    new ExtendedPosition("Forderungen", "FO001", "Forderungen Report", new List<PositionBase>
                    {
                        new Position("Kundenforderungen", "KF001", "Ausstehende Rechnungen"),
                        new Position("Sonstige Forderungen", "SF001", "Andere Forderungen"),
                        new Position("Vorsteuer", "VS001", "Erstattbare Vorsteuer")
                    }),
                    
                    // Kosten als ExtendedPosition
                    new ExtendedPosition("Kosten", "KO001", "Kosten Report", new List<PositionBase>
                    {
                        new Position("Betriebskosten", "BK001", "Laufende Betriebsausgaben"),
                        new Position("Abschreibungen", "AB001", "Wertminderungen"),
                        new Position("Finanzierungskosten", "FK001", "Zinsen und Gebühren")
                    })
                }
            },
            new Report("Verbindlichkeiten", "VB001")
            {
                Positions = new List<PositionBase>
                {
                    new Position("Lieferantenverbindlichkeiten", "LV001", "Offene Rechnungen"),
                    new Position("Bankverbindlichkeiten", "BV001", "Kredite und Darlehen"),
                    new Position("Steuerverbindlichkeiten", "SV001", "Zu zahlende Steuern"),
                    new ExtendedPosition("Prüfung", "PR001", "Prüfung", new List<PositionBase>
                    {
                        new Position("Prüfung Lieferanten", "PL001", "Prüfung der Lieferantenverbindlichkeiten"),
                        new Position("Prüfung Banken", "PB001", "Prüfung der Bankverbindlichkeiten"),
                        new Position("Prüfung Steuern", "PS001", "Prüfung der Steuerverbindlichkeiten"),
                    })
                }
            },
            new Report("Forderungen", "FO001")
            {
                Positions = new List<PositionBase>
                {
                    new Position("Kundenforderungen", "KF001", "Ausstehende Rechnungen"),
                    new Position("Sonstige Forderungen", "SF001", "Andere Forderungen"),
                    new Position("Vorsteuer", "VS001", "Erstattbare Vorsteuer")
                }
            },
            new Report("Kosten", "KO001")
            {
                Positions = new List<PositionBase>
                {
                    new Position("Betriebskosten", "BK001", "Laufende Betriebsausgaben"),
                    new Position("Abschreibungen", "AB001", "Wertminderungen"),
                    new Position("Finanzierungskosten", "FK001", "Zinsen und Gebühren")
                }
            }
        };
    }
}
