using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Main;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels;

public class PresetManagerViewModel : SelectableViewModel
{
    public ObservableCollection<Tournament> Presets { get; set; } = [];

    private Tournament? _currentChosen;
    public Tournament? CurrentChosen
    {
        get => _currentChosen;
        set
        {
            _currentChosen = value;
            if (_currentChosen != null)
            {
                _currentChosen.UpdatePlayers();
                Properties.Settings.Default.LastOpenedPresetName = _currentChosen!.Name;
                Properties.Settings.Default.Save();
            }

            IsPresetOpened = _currentChosen != null;
            MainViewModel.Configuration = _currentChosen;
            OnPropertyChanged(nameof(CurrentChosen));
        }
    }

    private bool _isCurrentPresetSaved;
    public bool IsCurrentPresetSaved
    {
        get => _isCurrentPresetSaved;
        set
        {
            _isCurrentPresetSaved = value;
            OnPropertyChanged(nameof(IsCurrentPresetSaved));
        }
    }

    private bool _isPresetOpened;
    public bool IsPresetOpened
    {
        get => _isPresetOpened;
        set
        {
            _isPresetOpened = value;
            OnPropertyChanged(nameof(IsPresetOpened));
        }
    }

    public ICommand OpenControllerCommand { get; set; }

    public ICommand AddNewPresetCommand { get; set; }
    public ICommand SavePresetCommand { get; set; }
    public ICommand OnItemListClickCommand { get; set; }

    public ICommand OpenPlayerManagerCommand { get; set; }
    public ICommand OpenCommand { get; set; }

    public ICommand ClearCurrentPresetCommand { get; set; }
    public ICommand DuplicateCurrentPresetCommand { get; set; }
    public ICommand RenameItemCommand { get; set; }
    public ICommand RemoveCurrentPresetCommand { get; set; }

    public ICommand SetRankedDataPathCommand { get; set; }

    public PresetManagerViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        LoadAllPresets();

        OpenControllerCommand = new RelayCommand(() => MainViewModel.SelectViewModel("Controller"));

        AddNewPresetCommand = new AddNewPresetCommand(this);
        SavePresetCommand = new RelayCommand(SavePreset);
        OnItemListClickCommand = new OnItemListClickCommand(this);

        ClearCurrentPresetCommand = new ClearPresetCommand(this);
        DuplicateCurrentPresetCommand = new DuplicatePresetCommand(this);
        RenameItemCommand = new RenamePresetCommand(this);
        RemoveCurrentPresetCommand = new RemovePresetCommand(this);

        OpenCommand = new RelayCommand(OpenPresetControlPanel);
        OpenPlayerManagerCommand = new RelayCommand(OpenPlayerManagerWindow);

        SetRankedDataPathCommand = new RelayCommand(SetRankedDataPath);

        LoadCurrentPreset();
    }

    public override bool CanEnable(Tournament tournament)
    {
        return true;
    }
    public override void OnEnable(object? parameter)
    {

    }
    public override bool OnDisable()
    {
        SavePreset();
        return true;
    }

    private void LoadCurrentPreset()
    {
        string lastOpened = Properties.Settings.Default.LastOpenedPresetName;
        if (string.IsNullOrEmpty(lastOpened)) return;

        for (int i = 0; i < Presets.Count; i++)
        {
            var current = Presets[i];
            if (current.Name.Equals(lastOpened, StringComparison.OrdinalIgnoreCase))
            {
                CurrentChosen = current;
                CurrentChosen!.UpdateGoodPacesTexts();
                return;
            }
        }
    }
    private void LoadAllPresets()
    {
        var presets = Directory.GetFiles(Consts.PresetsPath, "*.json", SearchOption.TopDirectoryOnly).AsSpan();
        for (int i = presets.Length - 1; i >= 0; i--)
        {
            string text = File.ReadAllText(presets[i]) ?? string.Empty;
            try
            {
                if (string.IsNullOrEmpty(text)) continue;
                Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
                if (data == null) continue;
                data.Validate();
                for (int j = 0; j < data.Players.Count; j++)
                {
                    var current = data.Players[j];
                    if (!string.IsNullOrEmpty(current.TwitchName))
                    {
                        current.StreamData.Main = current.TwitchName;
                        current.TwitchName = string.Empty;
                    }
                }
                data.MainViewModel = this;
                Presets.Add(data);
            }
            catch { }
        }
    }

    private void OpenPresetControlPanel()
    {
        if (CurrentChosen == null) return;

        //MainViewModel.Open<ControllerViewModel>();
    }
    private void OpenPlayerManagerWindow()
    {
        if (CurrentChosen == null) return;

        //MainViewModel.Open<PlayerManagerViewModel>();
    }

    public bool IsPresetNameUnique(string name)
    {
        for (int i = 0; i < Presets.Count; i++)
        {
            var current = Presets[i];
            if (current.Name!.Equals(name, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    public void AddItem(Tournament item)
    {
        MainViewModel.SavePreset(item);
        Presets.Add(item);
    }

    public void SetPresetAsNotSaved()
    {
        IsCurrentPresetSaved = false;
    }
    public void PresetIsSaved()
    {
        IsCurrentPresetSaved = true;
    }

    public void SetRankedDataPath()
    {
        string path = DialogBox.ShowOpenFolder();
        if (string.IsNullOrEmpty(path)) return;

        CurrentChosen!.RankedRoomDataPath = path;
    }

    public void SavePreset()
    {
        MainViewModel.SavePreset();
    }
}
