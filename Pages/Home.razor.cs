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

            Diagram.RegisterComponent<ReportGroup, ReportGroupWidget>();
            Diagram.RegisterComponent<HeaderNode, HeaderNodeWidget>();
            Diagram.RegisterComponent<ColumnNode, ColumnNodeWidget>();
            Diagram.RegisterComponent<ExtensionNode, ExtensionNodeWidget>();

            Diagram.RegisterBehavior(new HeaderNodeDragBehavior(Diagram));



            //event um single selection zu forcen
            Diagram.SelectionChanged += (selectedModel) =>
            {
                if (selectedModel != null && selectedModel.Selected)
                {
                    var selectedModels = Diagram.GetSelectedModels().ToList();
                    if (selectedModels.Count > 1)
                    {
                        foreach (var model in selectedModels.Where(m => m != selectedModel))
                        {
                            Diagram.UnselectModel(model);
                        }
                    }
                }
            };


            Diagram.PointerClick += (model, args) =>
            {
                if (model is ColumnNode clickedColumn)
                {
                    ToggleColumnExpansion(clickedColumn);
                }
                else if (model is ExtensionNode extensionNode)
                {
                    ExpandExtensionNode(extensionNode);
                }
                else if (model is HeaderNode headerNode)
                {
                    var group = Diagram.Groups.OfType<ReportGroup>().FirstOrDefault(g => g.Children.Contains(headerNode));
                    if (group != null && group.IsExpandedExtendedPosition)
                    {
                        // Gruppe ist erweitert -> zusammenklappen
                        CollapseExtendedGroup(group);
                    }
                }
            };
            

            Diagram.PointerDoubleClick += (model, args) =>
            {
                if (model is HeaderNode headerNode)
                {
                    var group = Diagram.Groups.OfType<ReportGroup>().FirstOrDefault(g => g.Children.Contains(headerNode));
                    if (group != null && !group.IsExpandedExtendedPosition)
                    {
                        // Nur Alert zeigen wenn NICHT erweitert (erweiterte werden per Single-Click eingeklappt)
                        _ = ShowAlert($"Report ID: {group.Report.Id}");
                    }
                }
                else if (model is ColumnNode columnNode)
                {
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
            var currentGroup = Diagram.Groups.OfType<ReportGroup>()
                .FirstOrDefault(g => g.Children.Contains(clickedColumn));
            if (currentGroup == null) return;

            var allColumnNodes = currentGroup.Children.OfType<ColumnNode>().OrderBy(c => c.OriginalIndex).ToList();
            
            foreach (var column in allColumnNodes)
            {
                if (column != clickedColumn)
                {
                    column.IsExpanded = false;
                }
            }

            clickedColumn.IsExpanded = !clickedColumn.IsExpanded;

            RecalculateColumnPositions(allColumnNodes, currentGroup);

            StateHasChanged();
        }


        private void RecalculateColumnPositions(List<ColumnNode> columnNodes, ReportGroup targetGroup)
        {
            if (targetGroup == null) return;

            var headerNode = targetGroup.Children.OfType<HeaderNode>().FirstOrDefault();
            if (headerNode == null) return;

            var headerX = headerNode.Position?.X ?? 100;
            var headerY = headerNode.Position?.Y ?? 0;
            
            var normalSpacing = 50;
            var expandedHeight = 100;

            var currentY = headerY + normalSpacing;

            for (int i = 0; i < columnNodes.Count; i++)
            {
                var column = columnNodes[i];
                column.SetPosition(headerX, currentY);

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
            
            Diagram.Groups.Clear();
            Diagram.Nodes.Clear();
            Diagram.Links.Clear();
            
            var centerX = 200; 
            var centerY = 300;
            
            var headerNode = new HeaderNode(new Point(centerX - 100, centerY - 200)) 
            { 
                ReportName = report.Name, 
                Locked = true 
            };
            
            var allNodes = new List<NodeModel> { headerNode };
            
            var nodeSpacing = 50;
            for (int i = 0; i < report.Positions.Count; i++)
            {
                var position = report.Positions[i];
                var columnNode = new ColumnNode(new Point(centerX - 100, centerY - 200 + (i + 1) * nodeSpacing))
                {
                    ReportPosition = position,
                    OriginalIndex = i,
                    Locked = true
                };

                
                if (position is ExtendedPosition)
                {
                    columnNode.AddPort(PortAlignment.Right);
                }

                allNodes.Add(columnNode);
            }
            
            foreach (var node in allNodes)
            {
                Diagram.Nodes.Add(node);
            }
            
            var reportGroup = new ReportGroup(report, allNodes);
            Diagram.Groups.Add(reportGroup);

            var extendedGroups = new List<(ExtendedPosition position, ReportGroup group)>();
            var extendedPositions = report.Positions.OfType<ExtendedPosition>().ToList();

            var treeLayout = CalculateTreeLayout(extendedPositions, centerX, centerY);
            
            for (int i = 0; i < extendedPositions.Count; i++)
            {
                var position = extendedPositions[i];
                var layoutPos = treeLayout[i];
                
                var extendedGroup = CreateExtendedPositionGroup(position, layoutPos.X, layoutPos.Y);
                extendedGroups.Add((position, extendedGroup));
            }

            CreateLinksToExtendedGroups(reportGroup, extendedGroups);

            // Verschachtelte Gruppen werden nicht mehr initial erstellt
            // CreateNestedExtendedGroups(extendedGroups);
        }

        private ReportGroup CreateExtendedPositionGroup(ExtendedPosition extendedPosition, int centerX, int centerY)
        {
            var nodeSpacing = 50;
           
            var headerNode = new HeaderNode(new Point(centerX - 100, centerY - 200))
            {
                ReportName = extendedPosition.Name,
                Locked = true
            };

            headerNode.AddPort(PortAlignment.Left);

            var allNodes = new List<NodeModel> { headerNode };

            // ExtensionNode hinzufügen statt ColumnNodes
            var extensionNode = new ExtensionNode(new Point(centerX - 100, centerY - 200 + 50), extendedPosition);
            allNodes.Add(extensionNode);

            foreach (var node in allNodes)
            {
                Diagram.Nodes.Add(node);
            }

            var dummyReport = new Report(extendedPosition.Name, extendedPosition.Id);
            var extendedGroup = new ReportGroup(dummyReport, allNodes);
            Diagram.Groups.Add(extendedGroup);

            return extendedGroup;
        }

        private void CreateLinksToExtendedGroups(ReportGroup mainGroup, List<(ExtendedPosition position, ReportGroup group)> extendedGroups)
        {
            foreach (var (extendedPosition, extendedGroup) in extendedGroups)
            {
                var sourceColumnNode = mainGroup.Children.OfType<ColumnNode>()
                    .FirstOrDefault(cn => cn.ReportPosition == extendedPosition);
                
                if (sourceColumnNode == null) continue;

                var targetHeaderNode = extendedGroup.Children.OfType<HeaderNode>().FirstOrDefault();
                if (targetHeaderNode == null) continue;

                var sourceAnchor = new DynamicAnchor(sourceColumnNode, new[]
                {
                    new BoundsBasedPositionProvider(1, 0.5)
                });
                
                var targetAnchor = new DynamicAnchor(targetHeaderNode, new[]
                {
                    new BoundsBasedPositionProvider(0, 0.5)
                });

                var link = Diagram.Links.Add(new LinkModel(sourceAnchor, targetAnchor));
                link.PathGenerator = new SmoothPathGenerator();
            }
        }

        private List<(int X, int Y)> CalculateTreeLayout(List<ExtendedPosition> positions, int rootX, int rootY)
        {
            var layout = new List<(int X, int Y)>();
            var levelWidth = 400;
            var nodeHeight = 300;
            
            var totalHeight = positions.Count * nodeHeight;
            var startY = rootY - (totalHeight / 2);
            
            for (int i = 0; i < positions.Count; i++)
            {
                var x = rootX + levelWidth;
                var y = startY + (i * nodeHeight);
                layout.Add((x, y));
            }
            
            return layout;
        }

        private void CreateNestedExtendedGroups(List<(ExtendedPosition position, ReportGroup group)> parentGroups)
        {
            foreach (var (parentPosition, parentGroup) in parentGroups)
            {
                var nestedExtendedPositions = parentPosition.Positions.OfType<ExtendedPosition>().ToList();
                if (!nestedExtendedPositions.Any()) continue;

                var nestedGroups = new List<(ExtendedPosition position, ReportGroup group)>();
                var parentHeaderNode = parentGroup.Children.OfType<HeaderNode>().FirstOrDefault();
                if (parentHeaderNode == null) continue;

                var parentX = (int)(parentHeaderNode.Position?.X ?? 0);
                var parentY = (int)(parentHeaderNode.Position?.Y ?? 0);
                var nestedLayout = CalculateNestedTreeLayout(nestedExtendedPositions, parentX, parentY);

                for (int i = 0; i < nestedExtendedPositions.Count; i++)
                {
                    var nestedPosition = nestedExtendedPositions[i];
                    var layoutPos = nestedLayout[i];

                    var nestedGroup = CreateExtendedPositionGroup(nestedPosition, layoutPos.X, layoutPos.Y);
                    nestedGroups.Add((nestedPosition, nestedGroup));
                }

                CreateLinksToExtendedGroups(parentGroup, nestedGroups);

                //recursion
                CreateNestedExtendedGroups(nestedGroups);
            }
        }

        private void ExpandExtensionNode(ExtensionNode extensionNode)
        {
            if (extensionNode.ExtendedPosition == null || extensionNode.PositionCount == 0) return;

            // Finde die Gruppe, die diese ExtensionNode enthält
            var parentGroup = Diagram.Groups.OfType<ReportGroup>()
                .FirstOrDefault(g => g.Children.Contains(extensionNode));
            if (parentGroup == null) return;

            // Markiere als erweitert und speichere Original
            parentGroup.IsExpandedExtendedPosition = true;
            parentGroup.OriginalExtendedPosition = extensionNode.ExtendedPosition;

            // Entferne die ExtensionNode
            Diagram.Nodes.Remove(extensionNode);
            parentGroup.RemoveChild(extensionNode);

            // Erstelle ColumnNodes für alle Positionen
            var nodeSpacing = 50;
            var extensionNodePos = extensionNode.Position;
            
            for (int i = 0; i < extensionNode.ExtendedPosition.Positions.Count; i++)
            {
                var position = extensionNode.ExtendedPosition.Positions[i];
                var columnNode = new ColumnNode(new Point(extensionNodePos.X, extensionNodePos.Y + (i * nodeSpacing)))
                {
                    ReportPosition = position,
                    OriginalIndex = i,
                    Locked = true
                };

                if (position is ExtendedPosition)
                {
                    columnNode.AddPort(PortAlignment.Right);
                }

                Diagram.Nodes.Add(columnNode);
                parentGroup.AddChild(columnNode);
            }

            // Jetzt erstelle verschachtelte ExtendedPosition-Gruppen falls welche vorhanden sind
            CreateNestedExtendedGroupsForExpansion(parentGroup, extensionNode.ExtendedPosition);

            StateHasChanged();
        }

        private void CreateNestedExtendedGroupsForExpansion(ReportGroup parentGroup, ExtendedPosition extendedPosition)
        {
            var nestedExtendedPositions = extendedPosition.Positions.OfType<ExtendedPosition>().ToList();
            if (!nestedExtendedPositions.Any()) return;

            var nestedGroups = new List<(ExtendedPosition position, ReportGroup group)>();
            var parentHeaderNode = parentGroup.Children.OfType<HeaderNode>().FirstOrDefault();
            if (parentHeaderNode == null) return;

            var parentX = (int)(parentHeaderNode.Position?.X ?? 0);
            var parentY = (int)(parentHeaderNode.Position?.Y ?? 0);
            var nestedLayout = CalculateNestedTreeLayout(nestedExtendedPositions, parentX, parentY);

            for (int i = 0; i < nestedExtendedPositions.Count; i++)
            {
                var nestedPosition = nestedExtendedPositions[i];
                var layoutPos = nestedLayout[i];

                var nestedGroup = CreateExtendedPositionGroup(nestedPosition, layoutPos.X, layoutPos.Y);
                nestedGroups.Add((nestedPosition, nestedGroup));
            }

            CreateLinksToExtendedGroups(parentGroup, nestedGroups);
        }

        private void CollapseExtendedGroup(ReportGroup group)
        {
            if (!group.IsExpandedExtendedPosition || group.OriginalExtendedPosition == null) return;

            // Entferne alle verschachtelten ExtendedPosition-Gruppen
            RemoveNestedExtendedGroups(group.OriginalExtendedPosition);

            // Entferne alle ColumnNodes
            var headerNode = group.Children.OfType<HeaderNode>().FirstOrDefault();
            var columnNodesToRemove = group.Children.OfType<ColumnNode>().ToList();

            foreach (var columnNode in columnNodesToRemove)
            {
                Diagram.Nodes.Remove(columnNode);
                group.RemoveChild(columnNode);
            }

            // Erstelle ExtensionNode wieder
            var headerPosition = headerNode?.Position ?? new Point(0, 0);
            var extensionNode = new ExtensionNode(
                new Point(headerPosition.X, headerPosition.Y + 50), 
                group.OriginalExtendedPosition
            );

            Diagram.Nodes.Add(extensionNode);
            group.AddChild(extensionNode);

            // Reset flags
            group.IsExpandedExtendedPosition = false;
            group.OriginalExtendedPosition = null;

            StateHasChanged();
        }

        private void RemoveNestedExtendedGroups(ExtendedPosition extendedPosition)
        {
            var nestedExtendedPositions = extendedPosition.Positions.OfType<ExtendedPosition>().ToList();
            
            foreach (var nestedPosition in nestedExtendedPositions)
            {
                // Finde die entsprechende Gruppe
                var groupToRemove = Diagram.Groups.OfType<ReportGroup>()
                    .FirstOrDefault(g => g.Report.Id == nestedPosition.Id);
                
                if (groupToRemove != null)
                {
                    // Rekursiv verschachtelte Gruppen entfernen
                    if (groupToRemove.IsExpandedExtendedPosition && groupToRemove.OriginalExtendedPosition != null)
                    {
                        RemoveNestedExtendedGroups(groupToRemove.OriginalExtendedPosition);
                    }
                    
                    // Entferne alle Nodes der Gruppe
                    var nodesToRemove = groupToRemove.Children.ToList();
                    foreach (var node in nodesToRemove)
                    {
                        Diagram.Nodes.Remove(node);
                    }
                    
                    // Entferne die Gruppe selbst
                    Diagram.Groups.Remove(groupToRemove);
                }
            }
        }

        private List<(int X, int Y)> CalculateNestedTreeLayout(List<ExtendedPosition> positions, int parentX, int parentY)
        {
            var layout = new List<(int X, int Y)>();
            var levelWidth = 350;
            var nodeHeight = 200;
            
            for (int i = 0; i < positions.Count; i++)
            {
                var x = parentX + levelWidth;
                var y = parentY + (i * nodeHeight) - ((positions.Count - 1) * nodeHeight / 2);
                layout.Add((x, y));
            }
            
            return layout;
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
                    
                    new ExtendedPosition("Forderungen", "FO001", "Forderungen Report", new List<PositionBase>
                    {
                        new Position("Kundenforderungen", "KF001", "Ausstehende Rechnungen"),
                        new Position("Sonstige Forderungen", "SF001", "Andere Forderungen"),
                        new Position("Vorsteuer", "VS001", "Erstattbare Vorsteuer")
                    }),
                    
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
