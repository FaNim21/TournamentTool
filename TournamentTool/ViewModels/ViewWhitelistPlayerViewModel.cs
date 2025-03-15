using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;
using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class ViewWhitelistPlayerViewModel : BaseViewModel
{
    private IPresetSaver PresetSaver { get; set; }
    private ILoadingDialog LoadingDialog { get; set; }

    public Player Player { get; private set; }

    public ICommand CorrectPlayerUUIDCommand { get; set; }
    public ICommand OpenNameMCCommand { get; set; }

    
    public ViewWhitelistPlayerViewModel(Player player, IPresetSaver presetSaver, ILoadingDialog loadingDialog)
    {
        Player = player;
        PresetSaver = presetSaver;
        LoadingDialog = loadingDialog;

        CorrectPlayerUUIDCommand = new RelayCommand(() => { LoadingDialog.ShowLoading(CompleteUUID, true); });
        OpenNameMCCommand = new RelayCommand(OpenNameMC);
    }

    private async Task CompleteUUID(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        if (Player == null) return;
        
        cancellationToken.ThrowIfCancellationRequested();
        progress.Report(0.25f);

        logProgress.Report($"Getting uuid: {Player.InGameName}");
        await Task.Delay(500, cancellationToken);

        var data = await Player.GetDataFromInGameName();
        progress.Report(0.5f);
        if (!data.HasValue) return;

        Player.UUID ??= string.Empty;
        if (data.Value.UUID.Equals(Player.UUID))
        {
            logProgress.Report($"player {Player.UUID} is correct");
        }
        else
        {
            logProgress.Report($"incorrect in UUID based on IGN... changing to: {data.Value.UUID}");
            Player.UUID = data.Value.UUID;
        }
        
        await Task.Delay(500, cancellationToken);
        progress.Report(1.0f);
        await Task.Delay(50, cancellationToken);
        PresetSaver.SavePreset();
    }

    private void OpenNameMC()
    {
        if (string.IsNullOrEmpty(Player.UUID))
        {
            DialogBox.Show("Can't open website because player has empty uuid data", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var processStart = new ProcessStartInfo($"https://pl.namemc.com/profile/{Player.UUID}")
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(processStart);
    }
}