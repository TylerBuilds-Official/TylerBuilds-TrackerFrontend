namespace JobTrackerFrontend.Models;

public class PunchStatusModel
{
    public int WorkerId { get; set; }
    public string EmployeeId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsClockedIn { get; set; }
    public int? PunchId { get; set; }
    public int? JobCodeId { get; set; }
    public int? JobId { get; set; }
    public DateTime? ClockIn { get; set; }
    public string? Notes { get; set; }
    public int? JobCodeCode { get; set; }
    public string? JobCodeName { get; set; }
    public string? JobTitle { get; set; }

    public string FullName => $"{FirstName} {LastName}";
    public string? JobCodeDisplay => JobCodeCode is not null ? $"{JobCodeCode} — {JobCodeName}" : null;
}
