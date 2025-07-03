using System.Collections.ObjectModel;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Selectable;

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


    public SettingsViewModel(ICoordinator coordinator) : base(coordinator) { }

    public override bool CanEnable() => true;

    public override void OnEnable(object? parameter)
    {
        Hotkeys = new ObservableCollection<Hotkey>(InputController.Instance.GetHotkeys());
    }

    public override bool OnDisable()
    {
        Hotkeys?.Clear();
        return true;
    }
}
