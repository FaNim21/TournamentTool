using System.IO;
using System.Text.Json;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Controller;
using TournamentTool.ViewModels.Settings;

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
            if (_isHamburgerMenuOpen == value) return;
            _isHamburgerMenuOpen = value;
            OnPropertyChanged(nameof(IsHamburgerMenuOpen));
        }
    }

    public ICommand OnHamburegerClick { get; set; }
    public ICommand SelectViewModelCommand { get; set; }


    public MainViewModel()
    {
        _serializerOptions = new JsonSerializerOptions() { WriteIndented = true };

        OnHamburegerClick = new RelayCommand(() => { IsHamburgerMenuOpen = !IsHamburgerMenuOpen; });
        SelectViewModelCommand = new RelayCommand<string>(SelectViewModel);

        VersionText = Consts.Version;
        OnPropertyChanged(nameof(VersionText));

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
                //SelectedViewModel = new UpdatesViewModel();
                break;
            case "Settings":
                Open<SettingsViewModel>();
                break;
        }
    }

    public void SavePreset()
    {
        if (Configuration == null) return;

        var data = JsonSerializer.Serialize<object>(Configuration, _serializerOptions);

        string path = Configuration.GetPath();
        File.WriteAllText(path, data);

        if (SelectedViewModel is not PresetManagerViewModel presetManager) return;
        presetManager.PresetIsSaved();
    }
}