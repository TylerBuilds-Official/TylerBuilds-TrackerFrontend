namespace JobTrackerFrontend.Models;

public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime PaidDate { get; set; }
    public string? Notes { get; set; }
}
