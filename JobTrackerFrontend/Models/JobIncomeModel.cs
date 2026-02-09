namespace JobTrackerFrontend.Models;

public class JobIncomeModel
{
    public int JobId { get; set; }
    public string JobTitle { get; set; } = "";
    public decimal PaidAmount { get; set; }
    public decimal InvoicedAmount { get; set; }
    public decimal Total => PaidAmount + InvoicedAmount;
}
