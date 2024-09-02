using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Services;

public interface INavigationService
{
    SelectableViewModel SelectedView { get; }
    void NavigateTo<T>(Tournament tournament) where T : SelectableViewModel;
}

public class NavigationService : BaseViewModel, INavigationService
{
    private readonly Func<Type, SelectableViewModel> _viewModelFactory;

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

    public void NavigateTo<T>(Tournament tournament) where T : SelectableViewModel
    {
        if (SelectedView != null && SelectedView.GetType() == typeof(T)) return;

        SelectableViewModel selectedViewModel = _viewModelFactory.Invoke(typeof(T));

        if (SelectedView != null && !SelectedView.OnDisable()) return;
        if (!selectedViewModel.CanEnable(tournament)) return;

        SelectedView = selectedViewModel;
        SelectedView.OnEnable(null);
    }
}
