using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;

namespace ReportGroups.Blazor.Models.Nodes;

public class HeaderNode : NodeModel
{
    public HeaderNode(Point? position = null) : base(position) 
    {
       
    }

    public string ReportName { get; set; } = string.Empty;

}