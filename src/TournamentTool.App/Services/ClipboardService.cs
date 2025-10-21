using System.Windows;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.App.Services;

public class ClipboardService : IClipboardService
{
    public string GetText()
    {
        return Clipboard.GetText();
    }
    public void SetText(string text)
    {
        Clipboard.SetText(text);
    }

    public void Clear()
    {
        Clipboard.Clear();
    }
}