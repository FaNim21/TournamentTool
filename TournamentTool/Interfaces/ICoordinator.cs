namespace TournamentTool.Interfaces;

public interface ICoordinator : ILoadingDialog, IDialogWindow
{
    bool AvailableNewUpdate { get; }
    
    void SelectViewModel(string viewModelName);
}