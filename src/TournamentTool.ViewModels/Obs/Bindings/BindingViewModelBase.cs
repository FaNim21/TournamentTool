using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Obs;

namespace TournamentTool.ViewModels.Obs.Bindings;

public abstract class BindingViewModelBase : BaseViewModel
{
    private ObservableCollection<string> _fields = [];
    public ObservableCollection<string> Fields
    {
        get => _fields;
        protected set => SetField(ref _fields, value);
    }
    
    private string _chosenField = string.Empty;
    public string ChosenField
    {
        get => _chosenField;
        set => SetField(ref _chosenField, value);
    }
    
    
    protected BindingViewModelBase(IDispatcherService dispatcher) : base(dispatcher) { }
    
    public abstract BindingKey GetBindingKey();
}