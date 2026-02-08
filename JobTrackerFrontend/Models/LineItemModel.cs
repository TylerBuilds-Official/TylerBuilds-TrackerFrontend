namespace JobTrackerFrontend.Models;

public class LineItemModel
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public DateTime CreatedAt { get; set; }
}
