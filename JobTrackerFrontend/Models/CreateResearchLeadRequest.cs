namespace JobTrackerFrontend.Models;

public class CreateResearchLeadRequest
{
    public int? ClientId { get; set; }
    public int? ContactId { get; set; }
    public int? ResearchThemeId { get; set; }
    public string BusinessName { get; set; } = "";
    public DateTime DateContacted { get; set; }
    public string? PainPoints { get; set; }
    public int InterestLevel { get; set; } = 3;
    public DateTime? FollowUpDate { get; set; }
    public string? Notes { get; set; }
    public int WouldPay { get; set; }
    public int Status { get; set; }
}
