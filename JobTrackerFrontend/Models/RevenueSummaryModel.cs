namespace JobTrackerFrontend.Models;

public class RevenueSummaryModel
{
    public decimal TotalRevenue { get; set; }
    public decimal Outstanding { get; set; }
    public decimal Overdue { get; set; }
    public decimal Draft { get; set; }
    public int PaidInvoiceCount { get; set; }
    public int PendingInvoiceCount { get; set; }
}
