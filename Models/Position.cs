namespace ReportGroups.Blazor.Models;

public class Position
{
    public string Name { get; set; }
    public string PositionId { get; set; }
    public string FurtherInformation { get; set; }

    public Position(string name, string positionId, string furtherInformation)
    {
        Name = name;
        PositionId = positionId;
        FurtherInformation = furtherInformation;
    }
}