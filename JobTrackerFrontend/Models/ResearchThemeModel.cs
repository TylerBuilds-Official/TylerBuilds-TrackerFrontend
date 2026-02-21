namespace JobTrackerFrontend.Models;

public class ResearchThemeModel
{
    public int ResearchThemeId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
