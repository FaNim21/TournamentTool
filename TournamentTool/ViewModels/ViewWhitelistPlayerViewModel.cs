﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class ViewWhitelistPlayerViewModel : BaseViewModel
{
    private IPresetSaver PresetSaver { get; }
    private ILoadingDialog LoadingDialog { get; }

    public PlayerViewModel PlayerViewModel { get; }

    public ICommand CorrectPlayerUUIDCommand { get; init; }
    public ICommand OpenNameMCCommand { get; init; }
    public ICommand CopyDataCommand { get; init; }


    public ViewWhitelistPlayerViewModel(PlayerViewModel playerViewModel, IPresetSaver presetSaver, ILoadingDialog loadingDialog)
    {
        PlayerViewModel = playerViewModel;
        PresetSaver = presetSaver;
        LoadingDialog = loadingDialog;

        CorrectPlayerUUIDCommand = new RelayCommand(() => { LoadingDialog.ShowLoading(CompleteUUID, true); });
        OpenNameMCCommand = new RelayCommand(OpenNameMC);
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
            DialogBox.Show("Can't open website because player has empty uuid data", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var processStart = new ProcessStartInfo($"https://pl.namemc.com/profile/{PlayerViewModel.UUID}")
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(processStart);
    }

    private void CopyToClipboard(string text)
    {
        //TODO: 9 jak bede robic popupy to dac info o skopiowaniu do clipboarda
        if (string.IsNullOrEmpty(text)) return;
        Clipboard.SetText(text);
    }
}