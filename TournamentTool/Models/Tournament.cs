using MultiOpener.Entities.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Serialization;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class Tournament : BaseViewModel, IRenameItem
{
    public string Name { get; set; } = string.Empty;

    public int Port { get; set; } = 4455;
    public string Password { get; set; }
    public string SceneCollection { get; set; }
    public string Scene { get; set; }

    public ObservableCollection<PointOfView> POVs { get; set; } = [];
    public ObservableCollection<Player> Players { get; set; } = [];

    [JsonIgnore]
    public MainViewModel MainViewModel;

    public bool IsUsingPaceMan { get; set; }


    [JsonConstructor]
    public Tournament(string name = "")
    {
        Name = name;
    }
    public Tournament(MainViewModel mainViewModel, string name = "")
    {
        MainViewModel = mainViewModel;
        Name = name;
    }

    public void ChangeName(string name)
    {
        var jsonName = name + ".json";
        var path = GetPath();

        var directoryName = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(directoryName, jsonName);

        File.Move(path, newPath);
        Name = name;
        OnPropertyChanged(nameof(Name));
        MainViewModel.SavePresetCommand.Execute(this);
    }

    public string GetPath()
    {
        return Path.Combine(Consts.PresetsPath, Name + ".json");
    }

    public void Clear()
    {
        Port = 4455;
        Password = string.Empty;
        SceneCollection = string.Empty;
        Scene = string.Empty;

        POVs = [];
        Players = [];
    }
}
