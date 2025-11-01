using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Preset;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.ViewModels.Selectable.Preset;

public class TournamentPresetViewModel : BaseViewModel, IRenameItem, IPreset
{
    private readonly ITournamentState _tournamentState;
    private readonly IPresetSaver _presetSaver;
    private readonly PresetManagerViewModel _presetManager;
    
    private readonly TournamentPreset _data;

    public string Name
    {
        get => _data.Name;
        set
        {
            _data.Name = value;
            OnPropertyChanged(nameof(Name));
        }
    } 


    public TournamentPresetViewModel(TournamentPreset data, PresetManagerViewModel presetManager, ITournamentState tournamentState, IDispatcherService dispatcher,
        IPresetSaver presetSaver) : base(dispatcher)
    {
        _data = data;
        _presetManager = presetManager;
        _tournamentState = tournamentState;
        _presetSaver = presetSaver;
    }

    public string ChangeName(string name)
    {
        if (string.IsNullOrEmpty(name) || Name.Equals(name)) return string.Empty;
        if (!_presetManager!.IsPresetNameUnique(name))
        {
            return $"Preset item named '{name}' already exists";
        }
        
        var path = Path.Combine(Consts.PresetsPath, $"{Name}.json");
        var directoryName = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(directoryName, $"{name}.json");

        File.Move(path, newPath);
        _tournamentState.ChangeName(name);
        Name = name;
        _presetSaver.SavePreset();
        
        return string.Empty;
    }

    public string GetPath()
    {
        return Path.Combine(Consts.PresetsPath, Name + ".json");
    }
}