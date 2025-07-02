using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TournamentTool.Utils.Parsers;

public static class MarkdownParser
{
    public static TextBlock ParseMarkdown(string markdown)
    {
        var textBlock = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(237, 234, 222)),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                textBlock.Inlines.Add(new LineBreak());
                continue;
            }

            List<Inline> inlines;
            if (line.StartsWith("#### "))
            {
                // Header level 4
                textBlock.Inlines.Add(new LineBreak());
                var headerRun = new Run(line[5..]) { FontSize = 18, FontWeight = FontWeights.Bold };
                textBlock.Inlines.Add(headerRun);
            }
            else if (line.StartsWith("### "))
            {
                // Header level 3
                textBlock.Inlines.Add(new LineBreak());
                var headerRun = new Run(line[4..]) { FontSize = 20, FontWeight = FontWeights.Bold };
                textBlock.Inlines.Add(headerRun);
            }
            else if (line.StartsWith("## "))
            {
                // Header level 2
                textBlock.Inlines.Add(new LineBreak());
                var headerRun = new Run(line[3..]) { FontSize = 22, FontWeight = FontWeights.Bold };
                textBlock.Inlines.Add(headerRun);
            }
            else if (line.StartsWith("# "))
            {
                // Header level 1
                textBlock.Inlines.Add(new LineBreak());
                var headerRun = new Run(line[2..]) { FontSize = 24, FontWeight = FontWeights.Bold };
                textBlock.Inlines.Add(headerRun);
            }
            else if (line.StartsWith("- "))
            {
                // List item
                var listItemText = line.Substring(2);
                inlines = ParseInlineMarkdown(listItemText);
                var listRun = new Run("• ");
                textBlock.Inlines.Add(listRun);

                foreach (var inline in inlines)
                    textBlock.Inlines.Add(inline);
            }
            else if (line.StartsWith("    - "))
            {
                // Sublist item
                var sublistItemText = line.Substring(6);
                inlines = ParseInlineMarkdown(sublistItemText);
                var sublistRun = new Run("    • ");
                textBlock.Inlines.Add(sublistRun);

                foreach (var inline in inlines)
                    textBlock.Inlines.Add(inline);
            }
            else
            {
                // Normal text
                inlines = ParseInlineMarkdown(line);
                foreach (var inline in inlines)
                    textBlock.Inlines.Add(inline);
            }
        }
        return textBlock;
    }

    private static List<Inline> ParseInlineMarkdown(string text)
    {
        var inlines = new List<Inline>();
        int pos = 0;

        while (pos < text.Length)
        {
            if (text[pos] == '*' && pos + 1 < text.Length && text[pos + 1] == '*') // Bold
            {
                pos += 2;
                int endPos = text.IndexOf("**", pos);
                if (endPos != -1)
                {
                    var boldText = text.Substring(pos, endPos - pos);
                    inlines.Add(new Run(boldText) { FontWeight = FontWeights.Bold });
                    pos = endPos + 2;
                }
            }
            else if (text[pos] == '*' && (pos + 1 >= text.Length || text[pos + 1] != '*')) // Italic
            {
                pos++;
                int endPos = text.IndexOf('*', pos);
                if (endPos != -1)
                {
                    var italicText = text.Substring(pos, endPos - pos);
                    inlines.Add(new Run(italicText) { FontStyle = FontStyles.Italic });
                    pos = endPos + 1;
                }
            }
            else
            {
                int endPos = text.IndexOfAny(new[] { '*', '\n' }, pos);
                if (endPos == -1) endPos = text.Length;

                var textRun = new Run(text.Substring(pos, endPos - pos));
                inlines.Add(textRun);
                pos = endPos;
            }
        }

        return inlines;
    }
}
