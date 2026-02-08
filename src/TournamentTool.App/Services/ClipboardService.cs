using System.Windows;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Logging;

namespace TournamentTool.App.Services;

public class ClipboardService : IClipboardService
{
    private readonly IPopupNotificationService _popupNotificationService;
    private readonly ILoggingService _logger;


    public ClipboardService(IPopupNotificationService popupNotificationService, ILoggingService logger)
    {
        _popupNotificationService = popupNotificationService;
        _logger = logger;
    }
    
    public string GetText()
    {
        string clipboard = string.Empty;
        
        try
        {
            clipboard = Clipboard.GetText();
        }
        catch (Exception ex)
        {
            _logger.Error($"Clipboard get error: {ex}");
        }

        return clipboard;
    }
    public void SetText(string text)
    {
        try
        {
            Clipboard.SetText(text);
            _popupNotificationService.ShowPopupOnMouse("Copied to clipboard", TimeSpan.FromMilliseconds(700));
        }
        catch (Exception ex)
        {
            _logger.Error($"Clipboard set error: {ex}");
        }
    }

    public void Clear()
    {
        try
        {
            Clipboard.Clear();
        }
        catch (Exception ex)
        {
            _logger.Error($"Clipboard clear error: {ex}");
        }
    }
}