using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Preset;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.ViewModels.Selectable.Preset;

public class TournamentPresetViewModel : BaseViewModel, IRenameItem, IPreset
{
    private ITournamentPresetManager Tournament { get; }
    
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


    public TournamentPresetViewModel(TournamentPreset data, ITournamentPresetManager tournament, IDispatcherService dispatcher) : base(dispatcher)
    {
        _data = data;
        Tournament = tournament;
    }

    public void ChangeName(string name)
    {
        if (string.IsNullOrEmpty(name) || Name.Equals(name)) return;

        var jsonName = name + ".json";
        var path = Path.Combine(Consts.PresetsPath, Name + ".json");

        var directoryName = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(directoryName, jsonName);

        File.Move(path, newPath);
        Name = name;
    }

    public string GetPath()
    {
        return Path.Combine(Consts.PresetsPath, Name + ".json");
    }
}