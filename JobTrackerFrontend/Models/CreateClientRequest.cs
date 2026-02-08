namespace JobTrackerFrontend.Models;

public class CreateClientRequest
{
    public string Type { get; set; } = "Company";
    public string Name { get; set; } = "";
    public string? Website { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Notes { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
