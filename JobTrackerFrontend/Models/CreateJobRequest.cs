namespace JobTrackerFrontend.Models;

public class CreateJobRequest
{
    public int ClientId { get; set; }
    public int? PrimaryContactId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "Lead";
    public string? BillingType { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? FixedPrice { get; set; }
    public decimal? RetainerAmount { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
}
