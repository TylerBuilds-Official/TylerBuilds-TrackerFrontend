namespace JobTrackerFrontend.Models;

public class CreateLineItemRequest
{
    public int InvoiceId { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
