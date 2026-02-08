namespace JobTrackerFrontend.Models;

public class UserModel
{
    public int Id { get; set; }
    public string AdObjectId { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
}
