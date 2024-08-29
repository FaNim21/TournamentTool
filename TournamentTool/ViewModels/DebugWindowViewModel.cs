using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace TournamentTool.ViewModels;

public class DebugWindowViewModel : BaseViewModel
{
    private INotifyPropertyChanged? _notifyingSelectedViewModel;

    private SelectableViewModel? _selectedViewModel;
    public SelectableViewModel? SelectedViewModel
    {
        get => _selectedViewModel;
        set
        {
            if (_selectedViewModel == value) return;
            if (_notifyingSelectedViewModel != null)
            {
                _notifyingSelectedViewModel.PropertyChanged -= OnSelectedViewModelPropertyChanged;
            }

            _selectedViewModel = value;

            if (_selectedViewModel is INotifyPropertyChanged notifyingVM)
            {
                _notifyingSelectedViewModel = notifyingVM;
                _notifyingSelectedViewModel.PropertyChanged += OnSelectedViewModelPropertyChanged;
            }

            RefreshVariables();
            OnPropertyChanged(nameof(SelectedViewModel));
            SelectedViewModelName = value.GetType().Name;
        }
    }

    private ObservableCollection<string> _variables = [];
    public ObservableCollection<string> Variables
    {
        get => _variables;
        set
        {
            _variables = value;
            OnPropertyChanged(nameof(Variables));
        }
    }

    private string _selectedViewModelName = string.Empty;
    public string SelectedViewModelName
    {
        get => _selectedViewModelName;
        set
        {
            _selectedViewModelName = value;
            OnPropertyChanged(nameof(SelectedViewModelName));
        }
    }


    public DebugWindowViewModel()
    {
        Variables = [];
    }

    private void RefreshVariables()
    {
        Variables.Clear();

        if (SelectedViewModel != null)
        {
            var properties = SelectedViewModel.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (typeof(ICommand).IsAssignableFrom(prop.PropertyType)) continue; 

                var value = prop.GetValue(SelectedViewModel)?.ToString() ?? "null";
                Variables.Add($"{prop.Name}: {value}");
            }
        }
    }

    private void OnSelectedViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var property = SelectedViewModel.GetType().GetProperty(e.PropertyName);

        if (property != null)
        {
            if (typeof(ICommand).IsAssignableFrom(property.PropertyType)) return; 

            var value = property.GetValue(SelectedViewModel)?.ToString() ?? "null";
            var existingVariable = Variables.FirstOrDefault(v => v.StartsWith(e.PropertyName));

            if (existingVariable != null)
            {
                int index = Variables.IndexOf(existingVariable);
                Variables[index] = $"{e.PropertyName}: {value}";
            }
            else
            {
                Variables.Add($"{e.PropertyName}: {value}");
            }
        }
    }
}
