namespace JobTrackerFrontend.Models;

public class NoteModel
{
    public int NoteId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public bool IsGlobal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? AuthorName { get; set; }

    public string ScopeDisplay => IsGlobal ? "Global" : "Private";
}
