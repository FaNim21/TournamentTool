using System.Windows;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services;

namespace TournamentTool.App.Services;

public class ClipboardService : IClipboardService
{
    private readonly IPopupNotificationService _popupNotificationService;

    
    public ClipboardService(IPopupNotificationService popupNotificationService)
    {
        _popupNotificationService = popupNotificationService;
    }
    
    public string GetText()
    {
        return Clipboard.GetText();
    }
    public void SetText(string text)
    {
        Clipboard.SetText(text);
        _popupNotificationService.ShowPopupOnMouse("Copied to clipboard", TimeSpan.FromMilliseconds(700));
    }

    public void Clear()
    {
        Clipboard.Clear();
    }
}