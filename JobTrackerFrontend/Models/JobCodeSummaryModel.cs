namespace JobTrackerFrontend.Models;

public class JobCodeSummaryModel
{
    public int JobCodeId { get; set; }
    public int JobCodeCode { get; set; }
    public string JobCodeName { get; set; } = "";
    public int PunchCount { get; set; }
    public double TotalHours { get; set; }

    public string DisplayName => $"{JobCodeCode} — {JobCodeName}";
}
