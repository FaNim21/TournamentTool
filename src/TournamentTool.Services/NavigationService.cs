using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.Services;

public interface INavigationService
{
    SelectableViewModel SelectedView { get; }
    event Action<SelectableViewModel>? OnSelectedViewModelChanged;
    
    void NavigateTo<T>() where T : SelectableViewModel;
    void Startup(SelectableViewModel mainViewModel);
}

public class NavigationService : BaseViewModel, INavigationService
{
    private readonly Func<Type, SelectableViewModel> _viewModelFactory;

    private SelectableViewModel _selectedView = null!;
    public SelectableViewModel SelectedView
    {
        get => _selectedView;
        private set
        {
            _selectedView = value;
            OnPropertyChanged(nameof(SelectedView));
        }
    }

    public event Action<SelectableViewModel>? OnSelectedViewModelChanged;


    public NavigationService(Func<Type, SelectableViewModel> viewModelFactory, IDispatcherService dispatcher) : base(dispatcher)
    {
        _viewModelFactory = viewModelFactory;
    }

    public void NavigateTo<T>() where T : SelectableViewModel
    {
        if (SelectedView != null && SelectedView.GetType() == typeof(T)) return;

        SelectableViewModel selectedViewModel = _viewModelFactory.Invoke(typeof(T));

        if (SelectedView != null && !SelectedView.OnDisable()) return;
        if (!selectedViewModel.CanEnable()) return;

        OnSelectedViewModelChanged?.Invoke(selectedViewModel);
        SelectedView = selectedViewModel;
        SelectedView.OnEnable(null);
    }
    public void Startup(SelectableViewModel selectable)
    {
        SelectedView = selectable;
        SelectedView.OnEnable(null);
    }
}
