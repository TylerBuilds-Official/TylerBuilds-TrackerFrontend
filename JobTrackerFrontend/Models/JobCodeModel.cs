namespace JobTrackerFrontend.Models;

public class JobCodeModel
{
    public int Id { get; set; }
    public int Code { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public string DisplayName => $"{Code} — {Name}";
}
