namespace JobTrackerFrontend.Models;

public class CreateInvoiceRequest
{
    public int JobId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}
