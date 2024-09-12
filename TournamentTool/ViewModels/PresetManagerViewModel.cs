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
    public ObservableCollection<TournamentPreset> Presets { get; set; } = [];

    private TournamentPreset? _currentChosen;
    public TournamentPreset? CurrentChosen
    {
        get => _currentChosen;
        set
        {
            _currentChosen = value;

            bool isEmpty = _currentChosen == null;
            if (!isEmpty)
            {
                SaveLastOpened(_currentChosen!.Name);
            }

            IsPresetOpened = !isEmpty;
            LoadCurrentPreset(isEmpty ? string.Empty : _currentChosen!.Name);
            OnPropertyChanged(nameof(CurrentChosen));
        }
    }

    private Tournament? _loadedPreset;
    public Tournament? LoadedPreset
    {
        get => _loadedPreset;
        set
        {
            _loadedPreset = value;
            MainViewModel.Configuration = _loadedPreset;
            OnPropertyChanged(nameof(LoadedPreset));
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

    public ICommand ClearCurrentPresetCommand { get; set; }
    public ICommand DuplicateCurrentPresetCommand { get; set; }
    public ICommand RenameItemCommand { get; set; }
    public ICommand RemoveCurrentPresetCommand { get; set; }

    public ICommand SetRankedDataPathCommand { get; set; }


    public PresetManagerViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        LoadPresetsList();

        OpenControllerCommand = new RelayCommand(() => MainViewModel.SelectViewModel("Controller"));

        AddNewPresetCommand = new AddNewPresetCommand(this);
        SavePresetCommand = new RelayCommand(() => SavePreset());
        OnItemListClickCommand = new OnItemListClickCommand(this);

        ClearCurrentPresetCommand = new ClearPresetCommand(this);
        DuplicateCurrentPresetCommand = new DuplicatePresetCommand(this, mainViewModel);
        RenameItemCommand = new RenamePresetCommand(this);
        RemoveCurrentPresetCommand = new RemovePresetCommand(this);

        SetRankedDataPathCommand = new RelayCommand(SetRankedDataPath);

        LoadStartupPreset();
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

    private void LoadStartupPreset()
    {
        string lastOpened = Properties.Settings.Default.LastOpenedPresetName;
        for (int i = 0; i < Presets.Count; i++)
        {
            if (Presets[i].Name.Equals(lastOpened))
            {
                CurrentChosen = Presets[i];
            }
        }
    }
    private void LoadCurrentPreset(string opened)
    {
        if (string.IsNullOrEmpty(opened)) return;

        string filePath = Path.Combine(Consts.PresetsPath, opened + ".json");
        string text = File.ReadAllText(filePath) ?? string.Empty;
        try
        {
            if (string.IsNullOrEmpty(text)) return;
            Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
            if (data == null) return;
            data.Validate();

            LoadedPreset = data;
        }
        catch { }
    }
    private void LoadPresetsList()
    {
        var presets = Directory.GetFiles(Consts.PresetsPath, "*.json", SearchOption.TopDirectoryOnly).AsSpan();
        for (int i = presets.Length - 1; i >= 0; i--)
        {
            string text = File.ReadAllText(presets[i]) ?? string.Empty;
            try
            {
                if (string.IsNullOrEmpty(text)) continue;

                TournamentPreset? data = JsonSerializer.Deserialize<TournamentPreset>(text);
                if (data == null) continue;

                data.Setup(this);
                Presets.Add(data);
            }
            catch { }
        }
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

    public void AddItem(TournamentPreset item, bool save = true)
    {
        if (save) MainViewModel.SavePreset(item);
        item.Setup(this);
        Presets.Add(item);
    }
    public void RemoveItem(TournamentPreset item)
    {
        Presets.Remove(item);
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

        LoadedPreset!.RankedRoomDataPath = path;
    }

    public void SavePreset(IPreset? preset = null)
    {
        MainViewModel.SavePreset(preset);
    }
    public void SaveLastOpened(string presetName)
    {
        Properties.Settings.Default.LastOpenedPresetName = presetName;
        Properties.Settings.Default.Save();
    }
}
