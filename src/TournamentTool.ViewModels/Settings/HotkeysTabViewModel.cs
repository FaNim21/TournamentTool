using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities.Input;
using TournamentTool.Domain.Interfaces;

namespace TournamentTool.ViewModels.Settings;

public class HotkeysTabViewModel : BaseViewModel, ISettingsTab
{
    private readonly Domain.Entities.Settings _settings;
    private readonly IInputController _inputController;

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
    public HotkeysTabViewModel(Domain.Entities.Settings settings, IDispatcherService dispatcher, IInputController inputController) : base(dispatcher)
    {
        Name = "hotkeys";
        _settings = settings;
        _inputController = inputController;
    }
    public void OnOpen()
    {
        IsChosen = true;
        
        //TU zrobic i tak wczytywanie z pliku Hotkey i z tego zczytywac
        Hotkeys = new ObservableCollection<Hotkey>();
    }
    public void OnClose()
    {
        IsChosen = false;
        Hotkeys?.Clear();
    }
}