namespace TournamentTool.Domain.Entities.Markdown;

public class MarkdownText : MarkdownElement
{
    public string Text { get; set; } = string.Empty;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
}