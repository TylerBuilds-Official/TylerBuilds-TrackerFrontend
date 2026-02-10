namespace JobTrackerFrontend.Models;

public class UpdateLineItemRequest
{
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
