namespace JobTrackerFrontend.Models;

public class JobModel
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int? PrimaryContactId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "";
    public string? BillingType { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? FixedPrice { get; set; }
    public decimal? RetainerAmount { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }
    public string? ClientName { get; set; }
    public string? ClientType { get; set; }
    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? ContactEmail { get; set; }
    public int? InvoiceCount { get; set; }
    public decimal? TotalPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
