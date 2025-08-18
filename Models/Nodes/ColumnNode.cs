using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ReportGroups.Blazor.Models;

namespace ReportGroups.Blazor.Models.Nodes;

public class ColumnNode : NodeModel
{
    public ColumnNode(Point? position = null) : base(position) 
    {
    }

    public Position Position { get; set; } = null!;
}