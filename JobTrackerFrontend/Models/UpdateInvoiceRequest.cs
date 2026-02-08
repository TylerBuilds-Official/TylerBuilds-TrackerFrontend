namespace JobTrackerFrontend.Models;

public class UpdateInvoiceRequest
{
    public decimal Amount { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}
