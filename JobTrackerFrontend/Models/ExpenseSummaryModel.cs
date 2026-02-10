namespace JobTrackerFrontend.Models;

public class ExpenseSummaryModel
{
    public decimal ThisMonthTotal { get; set; }
    public decimal ThisMonthReimbursable { get; set; }
    public decimal ThisMonthNet { get; set; }
    public decimal YearToDateTotal { get; set; }
}
