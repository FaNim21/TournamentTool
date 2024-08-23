using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly JsonSerializerOptions _serializerOptions;

    public string VersionText { get; set; }

    public List<SelectableViewModel> baseViewModels = [];

    public Tournament? Configuration { get; set; }

    private SelectableViewModel? _selectedViewModel;
    public SelectableViewModel? SelectedViewModel
    {
        get => _selectedViewModel;
        set
        {
            _selectedViewModel = value;
            OnPropertyChanged(nameof(SelectedViewModel));
        }
    }

    private bool _isHamburgerMenuOpen = false;
    public bool IsHamburgerMenuOpen
    {
        get => _isHamburgerMenuOpen;
        set
        {
            if (SelectedViewModel is UpdatesViewModel updates && updates.Downloading) return;
            if (_isHamburgerMenuOpen == value) return;

            _isHamburgerMenuOpen = value;
            OnPropertyChanged(nameof(IsHamburgerMenuOpen));
        }
    }

    private bool _newUpdate { get; set; }
    public bool NewUpdate
    {
        get => _newUpdate;
        set
        {
            _newUpdate = value;
            OnPropertyChanged(nameof(NewUpdate));
        }
    }

    public ICommand OnHamburegerClick { get; set; }
    public ICommand SelectViewModelCommand { get; set; }


    public MainViewModel()
    {
        if (!Directory.Exists(Consts.PresetsPath))
            Directory.CreateDirectory(Consts.PresetsPath);

        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        OnHamburegerClick = new RelayCommand(() => { IsHamburgerMenuOpen = !IsHamburgerMenuOpen; });
        SelectViewModelCommand = new RelayCommand<string>(SelectViewModel);

        VersionText = Consts.Version;
        OnPropertyChanged(nameof(VersionText));

        Task.Factory.StartNew(async () => await CheckForUpdate());

        Open<PresetManagerViewModel>();
    }

    public T? GetViewModel<T>() where T : SelectableViewModel
    {
        for (int i = 0; i < baseViewModels.Count; i++)
        {
            var current = baseViewModels[i];

            string currentTypeName = current.GetType().Name.ToLower();
            string genericName = typeof(T).Name.ToLower();
            if (currentTypeName.Equals(genericName))
                return (T)current;
        }

        return null;
    }

    public void Open<T>() where T : SelectableViewModel
    {
        if (SelectedViewModel != null && typeof(T) == SelectedViewModel.GetType()) return;

        T? viewModel = GetViewModel<T>();
        bool wasCreated = false;
        if (viewModel == null)
        {
            viewModel = (T)Activator.CreateInstance(typeof(T), this)!;
            wasCreated = true;
        }
        if (SelectedViewModel != null && !SelectedViewModel.OnDisable()) return;
        if (!viewModel.CanEnable(Configuration!)) return;

        object? parameter = SelectedViewModel?.parameterForNextSelectable;

        if (wasCreated) baseViewModels.Add(viewModel);

        if (SelectedViewModel != null && SelectedViewModel.CanBeDestroyed)
        {
            baseViewModels.Remove(SelectedViewModel);

            if (SelectedViewModel is IDisposable disposableViewModel)
            {
                disposableViewModel.Dispose();
            }
        }

        SelectedViewModel = viewModel;
        SelectedViewModel?.OnEnable(parameter);
        IsHamburgerMenuOpen = false;
    }

    public void SelectViewModel(string viewModelName)
    {
        switch (viewModelName)
        {
            case "Presets":
                Open<PresetManagerViewModel>();
                break;
            case "Whitelist":
                Open<PlayerManagerViewModel>();
                break;
            case "Controller":
                Open<ControllerViewModel>();
                break;
            case "Updates":
                Open<UpdatesViewModel>();
                break;
            case "Settings":
                Open<SettingsViewModel>();
                break;
        }
    }

    public void SavePreset(Tournament? configuration = null)
    {
        configuration ??= Configuration;
        if (configuration == null) return;

        var data = JsonSerializer.Serialize<object>(configuration, _serializerOptions);

        string path = configuration.GetPath();
        File.WriteAllText(path, data);

        if (SelectedViewModel is not PresetManagerViewModel presetManager) return;
        presetManager.PresetIsSaved();
    }

    private async Task CheckForUpdate()
    {
        UpdateChecker updateChecker = new();
        bool isNewUpdate = false;

        try
        {
            isNewUpdate = await updateChecker.CheckForUpdates();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }

        NewUpdate = isNewUpdate;
    }
}