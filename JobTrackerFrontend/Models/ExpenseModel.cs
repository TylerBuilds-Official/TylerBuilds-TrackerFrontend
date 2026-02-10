namespace JobTrackerFrontend.Models;

public class ExpenseModel
{
    public int Id { get; set; }
    public int? JobId { get; set; }
    public int CategoryId { get; set; }
    public string Vendor { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public bool IsReimbursable { get; set; }
    public string? ReceiptFilePath { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CategoryName { get; set; } = "";
    public string? JobTitle { get; set; }
    public string? ClientName { get; set; }

    public string JobDisplay => JobTitle is not null ? $"{JobTitle} ({ClientName})" : "— General —";
}
