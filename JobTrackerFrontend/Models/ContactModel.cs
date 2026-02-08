namespace JobTrackerFrontend.Models;

public class ContactModel
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? JobTitle { get; set; }
    public bool IsPrimary { get; set; }
    public string? ClientName { get; set; }
    public DateTime CreatedAt { get; set; }
}
