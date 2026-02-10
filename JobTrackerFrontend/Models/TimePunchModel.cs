namespace JobTrackerFrontend.Models;

public class TimePunchModel
{
    public int Id { get; set; }
    public int WorkerId { get; set; }
    public int JobCodeId { get; set; }
    public int? JobId { get; set; }
    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string WorkerFirstName { get; set; } = "";
    public string WorkerLastName { get; set; } = "";
    public int JobCodeCode { get; set; }
    public string JobCodeName { get; set; } = "";
    public string? JobTitle { get; set; }
    public int? TotalMinutes { get; set; }

    public string WorkerName => $"{WorkerFirstName} {WorkerLastName}";
    public string JobCodeDisplay => $"{JobCodeCode} — {JobCodeName}";

    public string TotalHoursDisplay
    {
        get
        {
            if (TotalMinutes is null) return "—";
            var h = TotalMinutes.Value / 60;
            var m = TotalMinutes.Value % 60;
            return h > 0 ? $"{h}h {m}m" : $"{m}m";
        }
    }
}
