using TournamentTool.Interfaces;
using TournamentTool.Modules.Lua;

namespace TournamentTool.ViewModels.Entities;

public class LuaCustomVariableViewModel : BaseViewModel
{
    private readonly LuaCustomVariable _model;
    private readonly INotifyPresetModification _notifyPresetModification;

    public string Name => _model.Name;
    public string Type => _model.Type;
    public string DefaultValue => _model.DefaultValue;

    public string Value
    {
        get => _model.Value;
        set
        {
            if (Equals(_model.Value, value)) return;
            _model.Value = value;
            _notifyPresetModification.PresetIsModified();
            OnPropertyChanged(nameof(Value));
        }
    }

    public LuaCustomVariableViewModel(LuaCustomVariable model, INotifyPresetModification notifyPresetModification)
    {
        _model = model;
        _notifyPresetModification = notifyPresetModification;
    }

    public void Update()
    {
        OnPropertyChanged(nameof(Value));
    }
}