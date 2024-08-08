using System.Collections.ObjectModel;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels;

public class SettingsViewModel : SelectableViewModel
{
    public ObservableCollection<Hotkey>? Hotkeys { get; set; }

    private Hotkey? _selectedHotkey;
    public Hotkey? SelectedHotkey
    {
        get => _selectedHotkey;
        set
        {
            _selectedHotkey = value;
            OnPropertyChanged(nameof(SelectedHotkey));
        }
    }


    public SettingsViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        CanBeDestroyed = true;
    }

    public override bool CanEnable(Tournament tournament)
    {
        return true;
    }

    public override void OnEnable(object? parameter)
    {
        Hotkeys = new(InputController.Instance.GetHotkeys());
    }

    public override bool OnDisable()
    {
        Hotkeys?.Clear();
        return true;
    }
}
