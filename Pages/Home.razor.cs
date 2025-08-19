using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Options;
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
                AllowMultiSelection = false
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
            
            // Clear existing groups AND their nodes
            Diagram.Groups.Clear();
            Diagram.Nodes.Clear();
            
            // Create header node and column nodes for positions
            var centerX = 200; // Center of diagram
            var centerY = 200;
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

            // Create additional groups for ExtendedPositions
            var groupOffset = 300; // Distance between groups
            var currentGroupX = centerX + groupOffset;

            foreach (var position in report.Positions.OfType<ExtendedPosition>())
            {
                CreateExtendedPositionGroup(position, currentGroupX, centerY);
                currentGroupX += groupOffset;
            }
        }

        private void CreateExtendedPositionGroup(ExtendedPosition extendedPosition, int centerX, int centerY)
        {
            var nodeSpacing = 50; // Spacing between nodes
            
            // Create header node for the extended position
            var headerNode = new HeaderNode(new Point(centerX - 100, centerY - 200))
            {
                ReportName = extendedPosition.Name,
                Locked = true
            };

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
        }

        private List<Report> reportPackage = new List<Report>
        {
            new Report("GuV", "GUV001")
            {
                Positions = new List<PositionBase>
                {
                    new Position("Umsatzerlöse", "UE001", "Erlöse aus Verkäufen"),
                    new Position("Materialaufwand", "MA001", "Kosten für Rohstoffe"),
                    new Position("Personalkosten", "PK001", "Löhne und Gehälter")
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
                        new Position("Lieferantenverbindlichkeiten", "LV001", "Offene Rechnungen"),
                        new Position("Bankverbindlichkeiten", "BV001", "Kredite und Darlehen"),
                        new Position("Steuerverbindlichkeiten", "SV001", "Zu zahlende Steuern"),
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
