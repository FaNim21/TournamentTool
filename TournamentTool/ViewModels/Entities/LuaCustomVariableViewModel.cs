using System.Globalization;
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
            if (_model.Value == value) return;
            _model.Value = value;
            OnPropertyChanged(nameof(Value));
            _notifyPresetModification.PresetIsModified();
        }
    }
    public bool ValueBool
    {
        get => Convert.ToBoolean(_model.Value, CultureInfo.InvariantCulture);
        set
        {
            _model.Value = value.ToString();
            OnPropertyChanged(nameof(ValueBool));
            _notifyPresetModification.PresetIsModified();
        }
    }
    public double ValueNumeric
    {
        get => Convert.ToDouble(_model.Value, CultureInfo.InvariantCulture);
        set
        {
            _model.Value = value.ToString(CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(ValueNumeric));
            _notifyPresetModification.PresetIsModified();
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
        OnPropertyChanged(nameof(ValueBool));
        OnPropertyChanged(nameof(ValueNumeric));
    }

    public bool IsDefaultValue()
    {
        return Value == DefaultValue;
    }
}