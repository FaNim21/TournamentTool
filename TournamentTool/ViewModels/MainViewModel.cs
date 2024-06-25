using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Controller;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    public string VersionText { get; set; }

    public List<SelectableViewModel> baseViewModels = [];

    public PresetManagerViewModel PresetManager { get; private set; }

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
            _isHamburgerMenuOpen = value;
            OnPropertyChanged(nameof(IsHamburgerMenuOpen));
        }
    }

    public ICommand OnHamburegerClick { get; set; }
    public ICommand SelectViewModelCommand { get; set; }


    public MainViewModel()
    {
        OnHamburegerClick = new RelayCommand(() => { IsHamburgerMenuOpen = !IsHamburgerMenuOpen; });
        SelectViewModelCommand = new RelayCommand<string>(SelectViewModel);

        VersionText = Consts.Version;
        OnPropertyChanged(nameof(VersionText));

        PresetManager = new(this);

        baseViewModels.Add(PresetManager);
        baseViewModels.Add(new ControllerViewModel(this));

        SelectedViewModel = PresetManager;
        SelectedViewModel.OnEnable(null);
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
        if (SelectedViewModel != null && !SelectedViewModel.OnDisable()) return;

        object? parameter = SelectedViewModel?.parameterForNextSelectable;

        T? viewModel = GetViewModel<T>();
        if (viewModel == null)
        {
            viewModel = (T)Activator.CreateInstance(typeof(T), this)!;
            baseViewModels.Add(viewModel);
        }

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
                //SelectedViewModel = new SettingsViewModel();
                break;
        }
    }

    public void SavePreset()
    {
        PresetManager.SavePreset();
    }
}