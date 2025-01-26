using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class WhitelistPlayerWindowViewModel : BaseViewModel
{
    private PlayerManagerViewModel _playerManager; 

    private Player? _player;
    public Player Player
    {
        get => _player!;
        set
        {
            _player = value;
            OnPropertyChanged(nameof(Player));
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


    public WhitelistPlayerWindowViewModel(PlayerManagerViewModel playerManager, Player? player = null)
    {
        _playerManager = playerManager;

        SaveCommand = new RelayCommand(async () => { await Save(); });

        if (player != null)
        {
            IsEditing = true;
            Player = player;
            return;
        }

        Player = new();
    }

    private async Task Save()
    {
        bool success = await _playerManager.SavePlayer(Player, IsEditing);
        if (!success) return;
        closeWindow?.Invoke();
    }
}
