using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using TournamentTool.Commands;

namespace TournamentTool.ViewModels;

public class DebugVariable : BaseViewModel
{
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;

            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            OnPropertyChanged(nameof(Value));
        }
    }

    private int _indentLevel;
    public int IndentLevel
    {
        get => _indentLevel;
        set
        {
            _indentLevel = value;
            OnPropertyChanged(nameof(IndentLevel));
        }
    }

    private bool _isExpandable;
    public bool IsExpandable
    {
        get => _isExpandable;
        set
        {
            _isExpandable = value;
            OnPropertyChanged(nameof(IsExpandable));
        }
    }
}

public class DebugWindowViewModel : BaseViewModel
{
    private INotifyPropertyChanged? _notifyingSelectedViewModel;

    private SelectableViewModel _selectedViewModel = new(null!);
    public SelectableViewModel SelectedViewModel
    {
        get => _selectedViewModel;
        set
        {
            if (_selectedViewModel == value || value == null) return;

            UnsubscribeFromAllViewModels();

            _selectedViewModel = value;

            if (_selectedViewModel is INotifyPropertyChanged notifyingVM)
            {
                _notifyingSelectedViewModel = notifyingVM;
                _notifyingSelectedViewModel.PropertyChanged += OnSelectedViewModelPropertyChanged;
            }

            InitializeVariables();
            OnPropertyChanged(nameof(SelectedViewModel));
            SelectedViewModelName = SelectedViewModel.GetType().Name;
        }
    }

    private ObservableCollection<DebugVariable> _variables = [];
    public ObservableCollection<DebugVariable> Variables
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

    private Dictionary<object, List<DebugVariable>> _viewModelToVariablesMap = new();
    private HashSet<object> _visitedInstances = [];

    public ICommand ToggleExpandCommand { get; set; }


    public DebugWindowViewModel()
    {
        Variables = [];
        ToggleExpandCommand = new RelayCommand<DebugVariable>(ToggleExpand);
    }

    private void ToggleExpand(DebugVariable variable)
    {
        if (!variable.IsExpandable) return;

        variable.IsExpanded = !variable.IsExpanded;
        if (variable.IsExpanded)
        {
            AddNestedVariables(variable);
        }
        else
        {
            RemoveNestedVariables(variable);
        }
    }

    private void InitializeVariables()
    {
        Variables.Clear();
        _visitedInstances.Clear();

        if (SelectedViewModel == null) return;
        AddVariables(SelectedViewModel, "", true);
    }

    private void AddVariables(object viewModel, string parentName, bool isFirstLevel)
    {
        if (viewModel == null) return;

        _visitedInstances ??= [];
        if (_visitedInstances.Contains(viewModel)) return;
        _visitedInstances.Add(viewModel);

        var properties = viewModel.GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (typeof(ICommand).IsAssignableFrom(prop.PropertyType)) continue;

            var value = prop.GetValue(viewModel);
            var displayValue = value?.ToString() ?? "null";
            var variableName = string.IsNullOrEmpty(parentName) ? prop.Name : $"{parentName}.{prop.Name}";

            int indentLevel = string.IsNullOrEmpty(parentName) ? 0 : 1;
            bool isExpandable = isFirstLevel && value != null && typeof(BaseViewModel).IsAssignableFrom(prop.PropertyType);

            var existingVariable = Variables.FirstOrDefault(v => v.Name.StartsWith(variableName));
            if (existingVariable != null)
            {
                existingVariable.Value = displayValue;
                continue;
            }

            var debugVariable = new DebugVariable
            {
                Name = variableName,
                Value = displayValue,
                IndentLevel = indentLevel,
                IsExpandable = isExpandable,
                IsExpanded = false
            };

            Variables.Add(debugVariable);

            if (!isExpandable || value == null) continue;

            if (value is INotifyPropertyChanged notifyingVM)
            {
                notifyingVM.PropertyChanged += OnNestedViewModelPropertyChanged;
                if (!_viewModelToVariablesMap.ContainsKey(notifyingVM))
                {
                    _viewModelToVariablesMap[notifyingVM] = [];
                }
                _viewModelToVariablesMap[notifyingVM].Add(debugVariable);
            }

            if (!debugVariable.IsExpanded) continue;
            AddVariables(value, variableName, false);
        }
    }
    private void UpdateVariable(string variableName, string newValue)
    {
        var variable = Variables.FirstOrDefault(v => v.Name == variableName);
        if (variable == null) return;

        variable.Value = newValue;
    }

    private void AddNestedVariables(DebugVariable parentVariable)
    {
        var parentIndex = Variables.IndexOf(parentVariable);
        var parentName = parentVariable.Name;

        var parentViewModel = FindViewModelByVariableName(SelectedViewModel, parentName);
        if (parentViewModel == null) return;

        if(!_visitedInstances.Contains(parentViewModel)) 
            _visitedInstances.Add(parentViewModel);

        var properties = parentViewModel.GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (typeof(ICommand).IsAssignableFrom(prop.PropertyType)) continue;

            var value = prop.GetValue(parentViewModel);
            var displayValue = value?.ToString() ?? "null";
            var variableName = $"{parentName}.{prop.Name}";

            int indentLevel = parentVariable.IndentLevel + 1;
            bool isExpandable = value != null && typeof(BaseViewModel).IsAssignableFrom(prop.PropertyType);
            if (isExpandable && _visitedInstances.Contains(value)) continue;

            var debugVariable = new DebugVariable
            {
                Name = variableName,
                Value = displayValue,
                IndentLevel = indentLevel,
                IsExpandable = isExpandable,
                IsExpanded = false
            };

            Variables.Insert(++parentIndex, debugVariable);

            if (isExpandable && value is INotifyPropertyChanged notifyingVM)
            {
                notifyingVM.PropertyChanged += OnNestedViewModelPropertyChanged;
                if (!_viewModelToVariablesMap.ContainsKey(notifyingVM))
                {
                    _viewModelToVariablesMap[notifyingVM] = [];
                }
                _viewModelToVariablesMap[notifyingVM].Add(debugVariable);
            }
        }
    }
    private void RemoveNestedVariables(DebugVariable parentVariable)
    {
        var parentIndex = Variables.IndexOf(parentVariable);
        var indentLevel = parentVariable.IndentLevel;

        for (int i = parentIndex + 1; i < Variables.Count && Variables[i].IndentLevel > indentLevel;)
        {
            var variableToRemove = Variables[i];
            Variables.RemoveAt(i);

            var parentName = variableToRemove.Name;
            var nestedViewModel = FindViewModelByVariableName(SelectedViewModel, parentName);
            if (nestedViewModel is INotifyPropertyChanged notifyingVM && _viewModelToVariablesMap.ContainsKey(notifyingVM))
            {
                notifyingVM.PropertyChanged -= OnNestedViewModelPropertyChanged;
                _viewModelToVariablesMap.Remove(notifyingVM);
            }
        }
    }

    private void OnSelectedViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var property = _selectedViewModel.GetType().GetProperty(e.PropertyName);
        if (property == null) return;

        var value = property.GetValue(_selectedViewModel)?.ToString() ?? "null";
        UpdateVariable(e.PropertyName, value);
    }
    private void OnNestedViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var nestedViewModel = sender;
        if (nestedViewModel == null || !_viewModelToVariablesMap.ContainsKey(nestedViewModel)) return;

        var variables = _viewModelToVariablesMap[nestedViewModel];
        foreach (var variable in variables)
        {
            var variableName = $"{variable.Name}.{e.PropertyName}";
            var value = nestedViewModel.GetType().GetProperty(e.PropertyName)?.GetValue(nestedViewModel)?.ToString() ?? "null";
            UpdateVariable(variableName, value);
        }
    }

    private object? FindViewModelByVariableName(object rootViewModel, string variableName)
    {
        var parts = variableName.Split('.');
        object? currentViewModel = rootViewModel;

        foreach (var part in parts)
        {
            if (currentViewModel == null) return null;

            var prop = currentViewModel.GetType().GetProperty(part);
            currentViewModel = prop?.GetValue(currentViewModel);
        }

        return currentViewModel;
    }

    private void UnsubscribeFromAllViewModels()
    {
        foreach (var kvp in _viewModelToVariablesMap)
        {
            if (kvp.Key is INotifyPropertyChanged notifyingVM)
            {
                notifyingVM.PropertyChanged -= OnNestedViewModelPropertyChanged;
            }
        }
        _viewModelToVariablesMap.Clear();

        if (_notifyingSelectedViewModel != null)
        {
            _notifyingSelectedViewModel.PropertyChanged -= OnSelectedViewModelPropertyChanged;
            _notifyingSelectedViewModel = null;
        }
    }

    public override void Dispose()
    {
        SelectedViewModelName = null;

        UnsubscribeFromAllViewModels();
        Variables.Clear();
        _visitedInstances.Clear();
    }
}
