namespace JobTrackerFrontend.Models;

public class RecentActivityModel
{
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string Summary { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime ActivityDate { get; set; }
}
