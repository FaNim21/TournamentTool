using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels;

public class WhitelistPlayerWindowViewModel : BaseWindowViewModel
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

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            _isSaving = value;
            OnPropertyChanged(nameof(IsSaving));
        }
    }

    public ICommand SaveCommand { get; set; }


    public WhitelistPlayerWindowViewModel(IPlayerViewModelFactory playerViewModelFactory, PlayerManagerViewModel playerManager, 
        PlayerViewModel? playerViewModel, IDispatcherService dispatcher) : base(dispatcher)
    {
        _playerManager = playerManager;

        SaveCommand = new RelayCommand(async () => { await Save(); });

        if (playerViewModel != null)
        {
            IsEditing = true;
            PlayerViewModel = playerViewModel;
            return;
        }

        IPlayerViewModel createdPlayer = playerViewModelFactory.Create();
        if (createdPlayer is not PlayerViewModel player) return;
        
        PlayerViewModel = player;
    }

    private async Task Save()
    {
        IsSaving = true;
        bool success = await _playerManager.SavePlayer(PlayerViewModel, IsEditing);
        if (!success)
        {
            IsSaving = false;
            return;
        }
        RequestClose?.Invoke();
        IsSaving = false;
    }
}
