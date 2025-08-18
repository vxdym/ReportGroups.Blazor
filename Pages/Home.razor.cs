using Microsoft.AspNetCore.Components;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Options;
using ReportGroups.Blazor.Models;
using ReportGroups.Blazor.Models.Groups;
using ReportGroups.Blazor.Models.Nodes;
using ReportGroups.Blazor.Components;

namespace ReportGroups.Blazor.Pages
{
    public partial class Home : ComponentBase
    {
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
        }

        private void OnReportClick(Report report)
        {
            selectedReport = report;
            
            // Clear existing groups AND their nodes
            Diagram.Groups.Clear();
            Diagram.Nodes.Clear();
            
            // Create header node and test nodes
            var centerX = 200; // Center of diagram
            var centerY = 200;
            
            var headerNode = new HeaderNode(new Point(centerX - 75, centerY - 200)) 
            { 
                ReportName = report.Name, 
                Locked = true 
            };
            
            var testNodes = new List<NodeModel>
            {
                headerNode,
                new NodeModel(new Point(centerX, centerY)) { Title = "Test Node 1", Locked = true },
                new NodeModel(new Point(centerX, centerY - 100)) { Title = "Test Node 2", Locked = true },
                new NodeModel(new Point(centerX, centerY - 150)) { Title = "Test Node 3", Locked = true }
            };
            
            // Add nodes to diagram first before putting them in group
            foreach (var node in testNodes)
            {
                Diagram.Nodes.Add(node);
            }
            
            // Create new ReportGroup for the selected report
            var reportGroup = new ReportGroup(report, testNodes);
            Diagram.Groups.Add(reportGroup);
        }

        private List<Report> reportPackage = new List<Report>
        {
            new Report("GuV Report", "GUV001")
            {
                Positions = new List<Position>
                {
                    new Position("Umsatzerlöse", "UE001", "Erlöse aus Verkäufen"),
                    new Position("Materialaufwand", "MA001", "Kosten für Rohstoffe"),
                    new Position("Personalkosten", "PK001", "Löhne und Gehälter")
                }
            },
            new Report("Verbindlichkeiten Report", "VB001")
            {
                Positions = new List<Position>
                {
                    new Position("Lieferantenverbindlichkeiten", "LV001", "Offene Rechnungen"),
                    new Position("Bankverbindlichkeiten", "BV001", "Kredite und Darlehen"),
                    new Position("Steuerverbindlichkeiten", "SV001", "Zu zahlende Steuern")
                }
            },
            new Report("Forderungen Report", "FO001")
            {
                Positions = new List<Position>
                {
                    new Position("Kundenforderungen", "KF001", "Ausstehende Rechnungen"),
                    new Position("Sonstige Forderungen", "SF001", "Andere Forderungen"),
                    new Position("Vorsteuer", "VS001", "Erstattbare Vorsteuer")
                }
            },
            new Report("Kosten Report", "KO001")
            {
                Positions = new List<Position>
                {
                    new Position("Betriebskosten", "BK001", "Laufende Betriebsausgaben"),
                    new Position("Abschreibungen", "AB001", "Wertminderungen"),
                    new Position("Finanzierungskosten", "FK001", "Zinsen und Gebühren")
                }
            }
        };
    }
}
