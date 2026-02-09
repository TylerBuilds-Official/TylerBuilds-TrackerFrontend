namespace JobTrackerFrontend.Models;

public class PaymentModel
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
