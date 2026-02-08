namespace JobTrackerFrontend.Models;

public class JobPipelineModel
{
    public string Status { get; set; } = "";
    public int JobCount { get; set; }
    public decimal TotalEstimatedValue { get; set; }
}
