namespace ReportGroups.Blazor.Models;

public class ExtendedPosition : PositionBase
{
    public bool IsExtended => true;
    public List<PositionBase> Positions { get; set; } = new();

    public ExtendedPosition(string name, string id, string furtherInformation, List<PositionBase> positions) 
        : base(name, id, furtherInformation)
    {
        Positions = positions;
    }
}
