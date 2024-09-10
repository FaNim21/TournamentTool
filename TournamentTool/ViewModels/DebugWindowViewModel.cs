using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
    public MainViewModel MainViewModel { get; set; }

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

    private Dictionary<object, List<DebugVariable>> _viewModelToVariablesMap = [];
    private HashSet<object> _visitedInstances = [];

    public ICommand ToggleExpandCommand { get; set; }


    public DebugWindowViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;

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
    private void UpdateVariable(string variableName, string newValue)
    {
        var variable = Variables.FirstOrDefault(v => v.Name == variableName);
        if (variable == null) return;

        variable.Value = newValue;
    }

    private void AddVariables(object viewModel, string parentName, bool isFirstLevel)
    {
        if (viewModel == null || _visitedInstances.Contains(viewModel)) return;

        _visitedInstances.Add(viewModel);
        var properties = viewModel.GetType().GetProperties();

        foreach (var prop in properties)
        {
            if (typeof(ICommand).IsAssignableFrom(prop.PropertyType)) continue;

            var value = prop.GetValue(viewModel);
            var displayValue = value?.ToString() ?? "null";
            var variableName = string.IsNullOrEmpty(parentName) ? prop.Name : $"{parentName}.{prop.Name}";

            int indentLevel = string.IsNullOrEmpty(parentName) ? 0 : 1;
            bool isExpandable = value != null && (typeof(BaseViewModel).IsAssignableFrom(prop.PropertyType) || typeof(INotifyCollectionChanged).IsAssignableFrom(prop.PropertyType));

            var debugVariable = new DebugVariable
            {
                Name = variableName,
                Value = displayValue,
                IndentLevel = indentLevel,
                IsExpandable = isExpandable,
                IsExpanded = false
            };

            Variables.Add(debugVariable);

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
    private void AddNestedVariables(DebugVariable parentVariable)
    {
        var parentIndex = Variables.IndexOf(parentVariable);
        var parentName = parentVariable.Name;

        var parentViewModel = FindViewModelByVariableName(SelectedViewModel, parentName);
        if (parentViewModel == null) return;

        if (!_visitedInstances.Contains(parentViewModel))
            _visitedInstances.Add(parentViewModel);

        var property = parentViewModel.GetType();
        if(property.Name.StartsWith("ObservableCollection"))
        {
            var prop = parentViewModel.GetType().GetProperties()[1];
            var collection = parentViewModel as IEnumerable;
            if (collection != null)
            {
                var collectionName = $"{parentName}.{prop.Name}";
                AddCollectionItems(collection.Cast<object>().ToList(), collectionName, parentVariable.IndentLevel + 1, parentIndex, true);

                if (collection is INotifyCollectionChanged notifyCollection)
                {
                    Trace.WriteLine($"Added Collection changed to: {collectionName}");
                    notifyCollection.CollectionChanged += OnCollectionChanged;
                    if (!_viewModelToVariablesMap.ContainsKey(notifyCollection))
                    {
                        _viewModelToVariablesMap[notifyCollection] = [];
                    }
                    _viewModelToVariablesMap[notifyCollection].Add(parentVariable);
                }
            }
            return;
        }
        var properties = parentViewModel.GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (typeof(ICommand).IsAssignableFrom(prop.PropertyType)) continue;

            var value = prop.GetValue(parentViewModel);
            var displayValue = value?.ToString() ?? "null";
            var variableName = $"{parentName}.{prop.Name}";

            int indentLevel = parentVariable.IndentLevel + 1;
            bool isExpandable = value != null && (typeof(BaseViewModel).IsAssignableFrom(prop.PropertyType) || typeof(INotifyCollectionChanged).IsAssignableFrom(prop.PropertyType));
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

            if (value is INotifyCollectionChanged collection)
            {
                Trace.WriteLine($"Added Collection changed to: {variableName}");
                collection.CollectionChanged += OnCollectionChanged;
            }

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
            var nestedViewModel = FindViewModelByVariableName(SelectedViewModel, variableToRemove.Name);

            _visitedInstances.Remove(nestedViewModel!);

            if (nestedViewModel is INotifyPropertyChanged notifyingVM && _viewModelToVariablesMap.ContainsKey(notifyingVM))
            {
                notifyingVM.PropertyChanged -= OnNestedViewModelPropertyChanged;
                _viewModelToVariablesMap.Remove(notifyingVM);
            }

            if (nestedViewModel is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= OnCollectionChanged;
            }

            if (variableToRemove.IsExpanded)
            {
                RemoveNestedVariables(variableToRemove);
            }

            Variables.RemoveAt(i);
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
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not IEnumerable collection) return;
        if (!_viewModelToVariablesMap.TryGetValue(sender, out var parentVariables)) return;

        var parent = parentVariables[0];
        if (!parent.IsExpanded) return;

        var parentIndex = Variables.IndexOf(parent);

        var parentName = parentVariables.FirstOrDefault()?.Name + ".Item" ?? string.Empty;
        var parentIndentLevel = parentVariables.FirstOrDefault()?.IndentLevel ?? 0;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    AddCollectionItems(e.NewItems, parentName, parentIndentLevel + 1, parentIndex);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    RemoveCollectionItems(e.OldItems, parentName);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                IList variables = Variables.Where(v => v.Name.StartsWith(parentName)).ToList();
                RemoveCollectionItems(variables, parentName);
                break;

                // case NotifyCollectionChangedAction.Move:
                // case NotifyCollectionChangedAction.Replace:
        }
    }

    private void AddCollectionItems(IList? newItems, string parentName, int indentLevel, int parentIndex, bool isExpanding = false)
    {
        if (newItems == null) return;

        int lastChildIndex = parentIndex;
        if (!isExpanding)
        {
            for (int i = parentIndex + 1; i < Variables.Count; i++)
            {
                if (Variables[i].IndentLevel != indentLevel) break;
                lastChildIndex = i;
            }
        }

        int index = lastChildIndex - parentIndex;
        foreach (var item in newItems)
        {
            var itemName = $"{parentName}[{index++}]";
            var displayValue = item?.ToString() ?? "null";

            var debugVariable = new DebugVariable
            {
                Name = itemName,
                Value = displayValue,
                IndentLevel = indentLevel,
                IsExpandable = item != null && typeof(BaseViewModel).IsAssignableFrom(item.GetType()),
                IsExpanded = false
            };

            Variables.Insert(++lastChildIndex, debugVariable);
        }
    }
    private void RemoveCollectionItems(IList? oldItems, string parentName)
    {
        if (oldItems == null) return;

        for (int i = 0; i < oldItems.Count; i++)
        {
            var itemName = $"{parentName}[{i}]";
            var variableToRemove = Variables.FirstOrDefault(v => v.Name == itemName);
            if (variableToRemove == null) continue;

            Variables.Remove(variableToRemove);
        }
    }

    private object? FindViewModelByVariableName(object rootViewModel, string variableName)
    {
        var parts = variableName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        object? currentViewModel = rootViewModel;

        foreach (var part in parts)
        {
            if (currentViewModel == null) return null;

            var indexStart = part.IndexOf('[');
            if (indexStart > 0)
            {
                var propertyName = part.Substring(0, indexStart);
                var indexEnd = part.IndexOf(']');
                if (indexEnd > indexStart && int.TryParse(part.AsSpan(indexStart + 1, indexEnd - indexStart - 1), out int index))
                {
                    if (currentViewModel is not IList collection) return null;
                    currentViewModel = collection[index];
                }
            }
            else
            {
                var prop = currentViewModel.GetType().GetProperty(part);
                currentViewModel = prop?.GetValue(currentViewModel);
            }
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

            if (kvp.Key is INotifyCollectionChanged collection)
            {
                Trace.WriteLine($"Cleared Collection changed in: {kvp.Key}");
                collection.CollectionChanged -= OnCollectionChanged;
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
        GC.SuppressFinalize(this);
    }
}
