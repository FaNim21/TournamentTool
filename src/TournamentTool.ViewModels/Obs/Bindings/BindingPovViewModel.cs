using System.Collections.ObjectModel;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs.Bindings;

public class BindingPovViewModel : BindingViewModelBase
{
    public ObservableCollection<string> PovNames { get; } = [];
    
    private string _chosenPovName = string.Empty;
    public string ChosenPovName
    {
        get => _chosenPovName;
        set
        {
            _chosenPovName = value;
            OnPropertyChanged();
        }
    }


    public BindingPovViewModel(IReadOnlyList<BindingPOVSchema> povSchemas, SceneViewModel sceneViewModel, BindingKey? bindingKey, IDispatcherService dispatcher) : base(dispatcher)
    {
        PovNames = [.. sceneViewModel.SceneItems.OfType<PointOfViewViewModel>().Select(p => p.SourceName)];
        Fields = [.. povSchemas.Select(f => f.Field)];

        if (bindingKey is not BindingKeyPOV povKey) return;
        if (povKey.IsEmpty()) return;
        
        ChosenPovName = PovNames.FirstOrDefault(name => name.Equals(povKey.PovName, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        ChosenField = Fields.FirstOrDefault(field => field.Equals(povKey.Field, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    public override BindingKey GetBindingKey()
    {
        string? povName = string.IsNullOrEmpty(ChosenPovName) ? null : ChosenPovName;
        return string.IsNullOrEmpty(povName) 
            ? BindingKey.CreateEmpty() 
            : BindingKey.CreatePov(ChosenField, povName);
    }
}