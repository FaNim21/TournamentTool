using System.Diagnostics;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Modals;

namespace TournamentTool.ViewModels;

public class ViewWhitelistPlayerViewModel : BaseViewModel
{
    private readonly IClipboardService _clipboard;
    private readonly IDialogService _dialogService;
    private IPresetSaver PresetSaver { get; }
    private ILoggingService Logger { get; }

    public PlayerViewModel PlayerViewModel { get; }

    public ICommand CorrectPlayerUUIDCommand { get; init; }
    public ICommand OpenNameMCCommand { get; init; }
    public ICommand OpenRankedStatsCommand { get; init; }
    public ICommand OpenPacemanStatsCommand { get; init; }
    public ICommand CopyDataCommand { get; init; }


    public ViewWhitelistPlayerViewModel(PlayerViewModel playerViewModel, IPresetSaver presetSaver, ILoggingService logger, IDispatcherService dispatcher, IWindowService windowService, IClipboardService clipboard, IDialogService dialogService) : base(dispatcher)
    {
        _clipboard = clipboard;
        _dialogService = dialogService;
        PlayerViewModel = playerViewModel;
        PresetSaver = presetSaver;
        Logger = logger;

        CorrectPlayerUUIDCommand = new RelayCommand(() => { windowService.ShowLoading(CompleteUUID); });
        OpenNameMCCommand = new RelayCommand(OpenNameMC);
        OpenRankedStatsCommand = new RelayCommand(OpenRankedStats);
        OpenPacemanStatsCommand = new RelayCommand(OpenPacemanStats);
        CopyDataCommand = new RelayCommand<string>(CopyToClipboard);
    }

    private async Task CompleteUUID(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        if (PlayerViewModel == null) return;
        
        cancellationToken.ThrowIfCancellationRequested();
        progress.Report(0.25f);

        logProgress.Report($"Getting uuid: {PlayerViewModel.InGameName}");
        await Task.Delay(500, cancellationToken);

        var data = await PlayerViewModel.GetDataFromInGameName();
        progress.Report(0.5f);
        if (!data.HasValue) return;

        PlayerViewModel.UUID ??= string.Empty;
        if (data.Value.UUID.Equals(PlayerViewModel.UUID))
        {
            logProgress.Report($"player {PlayerViewModel.UUID} is correct");
        }
        else
        {
            logProgress.Report($"incorrect in UUID based on IGN... changing to: {data.Value.UUID}");
            PlayerViewModel.UUID = data.Value.UUID;
        }
        
        await Task.Delay(500, cancellationToken);
        progress.Report(1.0f);
        await Task.Delay(50, cancellationToken);
        PresetSaver.SavePreset();
    }

    private void OpenNameMC()
    {
        if (string.IsNullOrEmpty(PlayerViewModel.UUID))
        {
            _dialogService.Show("Can't open website because player has empty uuid data", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var processStart = new ProcessStartInfo($"https://pl.namemc.com/profile/{PlayerViewModel.UUID}")
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(processStart);
    }
    private void OpenRankedStats()
    {
        var processStart = new ProcessStartInfo($"https://mcsrranked.com/stats/{PlayerViewModel.InGameName}")
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(processStart);
    }
    private void OpenPacemanStats()
    {
        var processStart = new ProcessStartInfo($"https://paceman.gg/stats/player/{PlayerViewModel.InGameName}")
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(processStart);
    }
    private void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        string currentText = _clipboard.GetText();
        if (currentText.Equals(text)) return;
        
        _clipboard.SetText(text);
        Logger.Information($"copied to clipboard: {text}");
    }
}