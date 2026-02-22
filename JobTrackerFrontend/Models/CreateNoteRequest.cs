namespace JobTrackerFrontend.Models;

public class CreateNoteRequest
{
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public bool IsGlobal { get; set; }
}
