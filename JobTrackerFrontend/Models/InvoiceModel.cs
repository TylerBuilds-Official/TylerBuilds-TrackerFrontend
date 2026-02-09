namespace JobTrackerFrontend.Models;

public class InvoiceModel
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public int Iteration { get; set; }
    public string Status { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }
    public string? JobTitle { get; set; }
    public string? BillingType { get; set; }
    public string? ClientName { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public decimal TotalPaid { get; set; }
    public string? NetworkFilePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string DisplayNumber => $"{InvoiceNumber}-{Iteration}";
    public decimal BalanceRemaining => Amount - TotalPaid;
}
