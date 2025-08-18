namespace ReportGroups.Blazor.Models;

public class Position
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string FurtherInformation { get; set; }

    public Position(string name, string id, string furtherInformation)
    {
        Name = name;
        Id = id;
        FurtherInformation = furtherInformation;
    }
}