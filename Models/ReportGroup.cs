using Blazor.Diagrams.Core.Models;

namespace ReportGroups.Blazor.Models;

public class ReportGroup : GroupModel
{
    public Report Report { get; }

    public ReportGroup(Report report, IEnumerable<NodeModel> children, byte padding = 30, bool autoSize = true) : base(children, padding, autoSize)
    {
        Report = report;
        Title = report.Name;
    }
}