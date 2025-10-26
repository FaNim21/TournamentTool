using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using TournamentTool.Domain.Entities.Markdown;

namespace TournamentTool.App.Rendering;

public static class MarkdownRenderer
{
    public static IEnumerable<Inline> RenderInlines(IEnumerable<MarkdownElement> elements)
    {
        List<Inline> inlines = [];
        
        //TODO; 0 Naprawic formatowanie textu markdown, poniewaz nie dziala to tak samo dobrze jak stary system
        foreach (var element in elements)
        {
            switch (element)
            {
                case MarkdownHeader header:
                    inlines.Add(new LineBreak());
                    inlines.Add(new Run(header.Text)
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 24 - (header.Level * 2)
                    });
                    break;

                case MarkdownListItem listItem:
                    inlines.Add(new LineBreak());
                    inlines.Add(new Run(new string(' ', listItem.IndentLevel * 4) + "• " + listItem.Text));
                    break;

                case MarkdownText text:
                    var run = new Run(text.Text);
                    if (text.IsBold) run.FontWeight = FontWeights.Bold;
                    if (text.IsItalic) run.FontStyle = FontStyles.Italic;
                    inlines.Add(run);
                    break;
            }
        }

        return inlines;
    }
    
    public static TextBlock RenderTextBlock(IEnumerable<MarkdownElement> elements)
    {
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230))
        };

        foreach (var element in elements)
        {
            switch (element)
            {
                case MarkdownHeader header:
                    textBlock.Inlines.Add(new LineBreak());
                    textBlock.Inlines.Add(new Run(header.Text)
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 24 - (header.Level * 2)
                    });
                    break;

                case MarkdownListItem listItem:
                    textBlock.Inlines.Add(new LineBreak());
                    textBlock.Inlines.Add(new Run(new string(' ', listItem.IndentLevel * 4) + "• " + listItem.Text));
                    break;

                case MarkdownText text:
                    var run = new Run(text.Text);
                    if (text.IsBold) run.FontWeight = FontWeights.Bold;
                    if (text.IsItalic) run.FontStyle = FontStyles.Italic;
                    textBlock.Inlines.Add(run);
                    break;
            }
        }

        return textBlock;
    }
}