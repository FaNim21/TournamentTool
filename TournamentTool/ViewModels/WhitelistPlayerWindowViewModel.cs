using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class WhitelistPlayerWindowViewModel : BaseViewModel
{
    private PlayerManagerViewModel _playerManager; 

    private PlayerViewModel? _playerViewModel;
    public PlayerViewModel PlayerViewModel
    {
        get => _playerViewModel!;
        set
        {
            _playerViewModel = value;
            OnPropertyChanged(nameof(PlayerViewModel));
        }
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            OnPropertyChanged(nameof(IsEditing));
        }
    }

    public ICommand SaveCommand { get; set; }

    public event Action? closeWindow;


    public WhitelistPlayerWindowViewModel(PlayerManagerViewModel playerManager, PlayerViewModel? playerViewModel)
    {
        _playerManager = playerManager;

        SaveCommand = new RelayCommand(async () => { await Save(); });

        if (playerViewModel != null)
        {
            IsEditing = true;
            PlayerViewModel = playerViewModel;
            return;
        }

        PlayerViewModel = new PlayerViewModel();
    }

    private async Task Save()
    {
        bool success = await _playerManager.SavePlayer(PlayerViewModel, IsEditing);
        if (!success) return;
        closeWindow?.Invoke();
    }
}
