namespace JobTrackerFrontend.Models;

public class WorkerModel
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Passcode { get; set; } = "";
    public decimal? HourlyRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
