using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.Windows;

namespace TournamentTool.Commands.PlayerManager;

public class ViewPlayerInfoCommand : BaseCommand
{
    private PlayerManagerViewModel PlayerManager { get; }
    private IDialogWindow DialogWindow { get; }
    private ILoadingDialog LoadingDialog { get; }
    private IPresetSaver PresetSaver { get; }


    public ViewPlayerInfoCommand(PlayerManagerViewModel playerManager, IDialogWindow dialogWindow, ILoadingDialog loadingDialog, IPresetSaver presetSaver)
    {
        PlayerManager = playerManager;
        DialogWindow = dialogWindow;
        LoadingDialog = loadingDialog;
        PresetSaver = presetSaver;
    }
    
    public override void Execute(object? parameter)
    {
        if (parameter is not Player player) return;

        ViewWhitelistPlayerViewModel viewModel = new(player, PresetSaver, LoadingDialog);
        ViewWhitelistPlayerWindow window = new() { DataContext = viewModel };
        
        DialogWindow.ShowDialog(window);
    }
}