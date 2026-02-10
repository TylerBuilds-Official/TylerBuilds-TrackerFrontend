namespace JobTrackerFrontend.Models;

public class UpdateExpenseRequest
{
    public int? JobId { get; set; }
    public int CategoryId { get; set; }
    public string Vendor { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public bool IsReimbursable { get; set; }
    public string? ReceiptFilePath { get; set; }
    public string? Notes { get; set; }
}
