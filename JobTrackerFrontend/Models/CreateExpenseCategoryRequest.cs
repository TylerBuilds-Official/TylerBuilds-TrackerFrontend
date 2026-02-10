namespace JobTrackerFrontend.Models;

public class CreateExpenseCategoryRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}
