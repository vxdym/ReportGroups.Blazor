namespace ReportGroups.Blazor.Models
{
    public class Report
    {
        public string Name { get; set; }
        public List<Position> Positions { get; set; } = new();
        public string Id { get; set; }

        public Report(string name, string id)
        {
            Name = name;
            Id = id;
        }
    }
}
