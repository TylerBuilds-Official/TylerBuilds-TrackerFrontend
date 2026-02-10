namespace JobTrackerFrontend.Models;

public class UpdateExpenseCategoryRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
