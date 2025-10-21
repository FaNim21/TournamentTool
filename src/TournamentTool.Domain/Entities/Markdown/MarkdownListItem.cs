namespace TournamentTool.Domain.Entities.Markdown;

public class MarkdownListItem : MarkdownElement
{
    public string Text { get; set; } = string.Empty;
    public int IndentLevel { get; set; }
}