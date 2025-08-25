using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ReportGroups.Blazor.Models;

namespace ReportGroups.Blazor.Models.Nodes;

public class ExtensionNode : NodeModel
{
    public string Text { get; set; }
    public int PositionCount { get; set; }
    public ExtendedPosition? ExtendedPosition { get; set; }

    public ExtensionNode(Point position, ExtendedPosition? extendedPosition = null) : base(position)
    {
        ExtendedPosition = extendedPosition;
        PositionCount = extendedPosition?.Positions.Count ?? 0;
        Text = PositionCount == 0 ? "(Enthält keine Positionen)" : $"{PositionCount} Positionen anzeigen";
        Locked = true;
    }
}