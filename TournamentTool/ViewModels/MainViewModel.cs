using TournamentTool.ViewModels.Controller;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    public List<BaseViewModel> baseViewModels = [];

    public PresetManagerViewModel PresetManager { get; private set; }

    private BaseViewModel? _selectedViewModel;
    public BaseViewModel? SelectedViewModel
    {
        get => _selectedViewModel;
        set
        {
            _selectedViewModel = value;
            OnPropertyChanged(nameof(SelectedViewModel));
        }
    }


    public MainViewModel()
    {
        PresetManager = new(this);

        baseViewModels.Add(PresetManager);
        baseViewModels.Add(new PlayerManagerViewModel(this));
        baseViewModels.Add(new ControllerViewModel(this));

        SelectedViewModel = PresetManager;
        SelectedViewModel.OnEnable(null);
    }

    public T? GetViewModel<T>() where T : BaseViewModel
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

    public void Open<T>() where T : BaseViewModel
    {
        for (int i = 0; i < baseViewModels.Count; i++)
        {
            var current = baseViewModels[i];

            if (current is T viewModel)
            {
                if (SelectedViewModel != null && !SelectedViewModel.OnDisable()) return;
                SelectedViewModel = viewModel;
                break;
            }
        }

        SelectedViewModel?.OnEnable(PresetManager.CurrentChosen);
    }

    public void SavePreset()
    {
        PresetManager.SavePreset();
    }
}