namespace JobTrackerFrontend.Models;

public class ClockInRequest
{
    public string EmployeeId { get; set; } = "";
    public string Passcode { get; set; } = "";
    public int JobCodeId { get; set; }
    public int? JobId { get; set; }
    public string? Notes { get; set; }
}
