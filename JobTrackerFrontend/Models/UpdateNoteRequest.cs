namespace JobTrackerFrontend.Models;

public class UpdateNoteRequest
{
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public bool IsGlobal { get; set; }
}
