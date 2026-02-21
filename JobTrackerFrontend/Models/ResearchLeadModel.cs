namespace JobTrackerFrontend.Models;

public class ResearchLeadModel
{
    public int ResearchLeadId { get; set; }
    public int? ClientId { get; set; }
    public int? ContactId { get; set; }
    public int? ResearchThemeId { get; set; }
    public string BusinessName { get; set; } = "";
    public DateTime DateContacted { get; set; }
    public string? PainPoints { get; set; }
    public int InterestLevel { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public string? Notes { get; set; }
    public int WouldPay { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Joined fields
    public string? ClientName { get; set; }
    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ThemeName { get; set; }

    // Display helpers
    public string ContactDisplay =>
        string.IsNullOrWhiteSpace(ContactFirstName) ? "—"
        : $"{ContactFirstName} {ContactLastName}".Trim();

    public string StatusDisplay => Status switch
    {
        0 => "New",
        1 => "Contacted",
        2 => "Following Up",
        3 => "Converted",
        4 => "Closed",
        _ => "Unknown"
    };

    public string WouldPayDisplay => WouldPay switch
    {
        0 => "No",
        1 => "Maybe",
        2 => "Yes",
        _ => "—"
    };

    public bool IsLinkedToClient => ClientId.HasValue;
}
