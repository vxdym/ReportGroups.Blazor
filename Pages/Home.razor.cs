using Microsoft.AspNetCore.Components;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Options;
using ReportGroups.Blazor.Models;
using ReportGroups.Blazor.Components;
using ReportGroups.Blazor.Behaviors;

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
            
            // Debug: Print selection changes
            Diagram.SelectionChanged += (selectedModel) =>
            {
                var selectedCount = Diagram.GetSelectedModels().Count();
                Console.WriteLine($"Selection changed. Selected count: {selectedCount}");
                
                if (selectedModel != null && selectedModel.Selected)
                {
                    var selectedModels = Diagram.GetSelectedModels().ToList();
                    Console.WriteLine($"Current selected models: {selectedModels.Count}");
                    
                    if (selectedModels.Count > 1)
                    {
                        Console.WriteLine("Multiple selection detected, unselelcting others...");
                        // More than one selected, keep only the last selected one
                        foreach (var model in selectedModels.Where(m => m != selectedModel))
                        {
                            Console.WriteLine($"Unselecting model: {model}");
                            Diagram.UnselectModel(model);
                        }
                    }
                }
            };
            
            Diagram.RegisterComponent<ReportGroup, ReportGroupWidget>();
        }

        private void OnReportClick(Report report)
        {
            selectedReport = report;
            
            // Clear existing groups
            Diagram.Groups.Clear();
            
            // Create some test nodes centered in diagram
            var centerX = 200; // Center of diagram
            var centerY = 200;
            var testNodes = new List<NodeModel>
            {
                new NodeModel(new Point(centerX - 50, centerY - 50)) { Title = "Test Node 1", Locked = false },
                new NodeModel(new Point(centerX + 50, centerY - 25)) { Title = "Test Node 2", Locked = false },
                new NodeModel(new Point(centerX, centerY + 50)) { Title = "Test Node 3", Locked = false }
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
