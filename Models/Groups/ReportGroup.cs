using Blazor.Diagrams.Core.Models;
using ReportGroups.Blazor.Models;

namespace ReportGroups.Blazor.Models.Groups;

public class ReportGroup : GroupModel
{
    public Report Report { get; }

    public ReportGroup(Report report, IEnumerable<NodeModel> children, byte padding = 0, bool autoSize = true) : base(children, padding, autoSize)
    {
        Report = report;
        Title = report.Name;
    }
}