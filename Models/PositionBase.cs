namespace ReportGroups.Blazor.Models;

public abstract class PositionBase
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string FurtherInformation { get; set; }

    protected PositionBase(string name, string id, string furtherInformation)
    {
        Name = name;
        Id = id;
        FurtherInformation = furtherInformation;
    }
}