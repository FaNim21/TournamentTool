using TournamentTool.ViewModels;

namespace TournamentTool.Services;

public interface INavigationService
{
    MainViewModel MainViewModel { get; set; }
    SelectableViewModel SelectedView { get; }
    void NavigateTo<T>() where T : SelectableViewModel;
    void Startup(MainViewModel mainViewModel);
}

public class NavigationService : BaseViewModel, INavigationService
{
    private readonly Func<Type, SelectableViewModel> _viewModelFactory;

    public MainViewModel MainViewModel { get; set; }

    private SelectableViewModel _selectedView;
    public SelectableViewModel SelectedView
    {
        get => _selectedView;
        private set
        {
            _selectedView = value;
            OnPropertyChanged(nameof(SelectedView));
        }
    }


    public NavigationService(Func<Type, SelectableViewModel> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }

    public void NavigateTo<T>() where T : SelectableViewModel
    {
        if (SelectedView != null && SelectedView.GetType() == typeof(T)) return;

        SelectableViewModel selectedViewModel = _viewModelFactory.Invoke(typeof(T));

        if (SelectedView != null && !SelectedView.OnDisable()) return;
        if (!selectedViewModel.CanEnable()) return;

        MainViewModel.UpdateDebugWindowViewModel(selectedViewModel);
        SelectedView = selectedViewModel;
        SelectedView.OnEnable(null);
    }

    public void Startup(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;

        SelectableViewModel selectedViewModel = _viewModelFactory.Invoke(typeof(PresetManagerViewModel));
        SelectedView = selectedViewModel;
        SelectedView.OnEnable(null);
    }
}
