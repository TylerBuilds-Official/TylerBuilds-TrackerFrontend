namespace JobTrackerFrontend.Models;

public class PromoteLeadRequest
{
    public string ClientType { get; set; } = "Company";
    public string ClientName { get; set; } = "";
    public string? Website { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? ClientNotes { get; set; }
    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactJobTitle { get; set; }
}
