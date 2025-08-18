using Microsoft.AspNetCore.Components;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using ReportGroups.Blazor.Models;
using ReportGroups.Blazor.Components;

namespace ReportGroups.Blazor.Pages
{
    public partial class Home : ComponentBase
    {
        private BlazorDiagram Diagram { get; set; } = null!;
        private Report? selectedReport = null;

        protected override void OnInitialized()
        {
            Diagram = new BlazorDiagram();
            Diagram.RegisterComponent<ReportGroup, ReportGroupWidget>();
        }

        private void OnReportClick(Report report)
        {
            selectedReport = report;
            
            // Clear existing groups
            Diagram.Groups.Clear();
            
            // Create new ReportGroup for the selected report
            var reportGroup = new ReportGroup(report, new List<NodeModel>());
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
