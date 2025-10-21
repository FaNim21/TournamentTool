namespace TournamentTool.Domain.Entities.Markdown;

public class MarkdownHeader : MarkdownElement
{
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;   
}