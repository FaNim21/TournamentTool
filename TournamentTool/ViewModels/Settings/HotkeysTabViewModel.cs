using System.Collections.ObjectModel;
using TournamentTool.Interfaces;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Settings;

public class HotkeysTabViewModel : BaseViewModel, ISettingsTab
{
    private readonly Models.Settings _settings;

    public bool IsChosen { get; private set; }
    public string Name { get; }
    
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
    
    
    //TODO: 2 Zrobic funkcjonalne hotkeys przy 1.0 lub 0.13
    public HotkeysTabViewModel(Models.Settings settings)
    {
        Name = "hotkeys";
        _settings = settings;
    }
    public void OnOpen()
    {
        IsChosen = true;
        Hotkeys = new ObservableCollection<Hotkey>(InputController.Instance.GetHotkeys());
    }
    public void OnClose()
    {
        IsChosen = false;
        Hotkeys?.Clear();
    }
}