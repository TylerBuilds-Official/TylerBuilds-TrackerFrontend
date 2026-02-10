namespace JobTrackerFrontend.Models;

public class ClockOutRequest
{
    public string EmployeeId { get; set; } = "";
    public string Passcode { get; set; } = "";
    public string? Notes { get; set; }
}
