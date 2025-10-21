using TournamentTool.Domain.Entities.Markdown;

namespace TournamentTool.Core.Parsers;

public static class MarkdownParser
{
    public static List<MarkdownElement> Parse(string markdown)
    {
        var result = new List<MarkdownElement>();
        var lines = markdown.Split('\n');

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("# "))
                result.Add(new MarkdownHeader { Level = 1, Text = line[2..] });
            else if (line.StartsWith("## "))
                result.Add(new MarkdownHeader { Level = 2, Text = line[3..] });
            else if (line.StartsWith("### "))
                result.Add(new MarkdownHeader { Level = 3, Text = line[4..] });
            else if (line.StartsWith("#### "))
                result.Add(new MarkdownHeader { Level = 4, Text = line[5..] });
            else if (line.StartsWith("- "))
                result.Add(new MarkdownListItem { Text = line[2..], IndentLevel = 0 });
            else
                result.Add(ParseInlineMarkdown(line));
        }

        return result;
    }
    private static MarkdownText ParseInlineMarkdown(string text)
    {
        var result = new MarkdownText { Text = text };

        if (text.StartsWith("**") && text.EndsWith("**"))
        {
            result.Text = text.Trim('*');
            result.IsBold = true;
        }
        else if (text.StartsWith('*') && text.EndsWith('*'))
        {
            result.Text = text.Trim('*');
            result.IsItalic = true;
        }

        return result;
    }}
