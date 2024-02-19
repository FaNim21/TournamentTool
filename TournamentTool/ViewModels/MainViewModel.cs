using OBSStudioClient;
using OBSStudioClient.Classes;
using OBSStudioClient.Enums;
using OBSStudioClient.Messages;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Main;
using TournamentTool.Models;
using TournamentTool.Properties;
using TournamentTool.Utils;
using TournamentTool.Windows;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
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
                Settings.Default.LastOpenedPresetName = _currentChosen!.Name;
                Settings.Default.Save();
            }

            IsPresetOpened = _currentChosen != null;
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

    public ICommand AddNewPresetCommand { get; set; }
    public ICommand SavePresetCommand { get; set; }
    public ICommand OnItemListClickCommand { get; set; }

    public ICommand OpenPlayerManagerCommand { get; set; }
    public ICommand OpenCommand { get; set; }

    public ICommand ClearCurrentPresetCommand { get; set; }
    public ICommand DuplicateCurrentPresetCommand { get; set; }
    public ICommand RenameItemCommand { get; set; }
    public ICommand RemoveCurrentPresetCommand { get; set; }


    public MainViewModel()
    {
        //TODO: 0 Zrobic zabezpieczenia do tego zeby nie wychodzic bez zapisywania

        if (!Directory.Exists(Consts.PresetsPath))
            Directory.CreateDirectory(Consts.PresetsPath);

        LoadAllPresets();

        AddNewPresetCommand = new AddNewPresetCommand(this);
        SavePresetCommand = new SavePresetCommand(this);
        OnItemListClickCommand = new OnItemListClickCommand(this);

        ClearCurrentPresetCommand = new ClearPresetCommand(this);
        DuplicateCurrentPresetCommand = new DuplicatePresetCommand(this);
        RenameItemCommand = new RenamePresetCommand(this);
        RemoveCurrentPresetCommand = new RemovePresetCommand(this);

        OpenCommand = new RelayCommand(OpenPresetControlPanel);
        OpenPlayerManagerCommand = new RelayCommand(OpenPlayerManagerWindow);

        LoadCurrentPreset();
    }

    private void LoadCurrentPreset()
    {
        string lastOpened = Settings.Default.LastOpenedPresetName;
        if (string.IsNullOrEmpty(lastOpened)) return;

        for (int i = 0; i < Presets.Count; i++)
        {
            var current = Presets[i];
            if (current.Name.Equals(lastOpened, StringComparison.OrdinalIgnoreCase))
            {
                CurrentChosen = current;
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
                data.MainViewModel = this;
                Presets.Add(data);
            }
            catch { }
        }
    }

    private void OpenPresetControlPanel()
    {
        if (CurrentChosen == null) return;

        ControllerWindow window = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = new ControllerViewModel(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        window.Show();
        Application.Current.MainWindow.Hide();
    }

    private void OpenPlayerManagerWindow()
    {
        PlayerManagerWindow window = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = new PlayerManagerViewModel(this, CurrentChosen!),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        window.Show();
        Application.Current.MainWindow.Hide();
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
        Presets.Add(item);
    }

    public void SetPresetAsNotSaved()
    {
        IsCurrentPresetSaved = false;
    }
}
