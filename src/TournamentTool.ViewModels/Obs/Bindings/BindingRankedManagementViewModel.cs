using System.Collections.ObjectModel;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Obs;

namespace TournamentTool.ViewModels.Obs.Bindings;

public class BindingRankedManagementViewModel : BindingViewModelBase
{
    public BindingRankedManagementViewModel(ObservableCollection<string> rankedManagementSchemas, BindingKey? bindingKey,
        IDispatcherService dispatcher) : base(dispatcher)
    {
        Fields = rankedManagementSchemas;

        if (bindingKey is not BindingKeyRankedManagement rankedManagementKey) return;
        if (rankedManagementKey.IsEmpty()) return;

        ChosenField = Fields.FirstOrDefault(field => field.Equals(rankedManagementKey.Field, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    public override BindingKey GetBindingKey()
    {
        return BindingKey.CreateRankedManagement(ChosenField);
    }
}