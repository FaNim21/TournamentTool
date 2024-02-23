using MultiOpener.Entities.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class Tournament : BaseViewModel, IRenameItem
{
    public string Name { get; set; } = string.Empty;

    public ObservableCollection<Player> Players { get; set; } = [];

    [JsonIgnore]
    public MainViewModel? MainViewModel;

    public int Port { get; set; } = 4455;
    public string Password { get; set; } = string.Empty;
    public string SceneCollection { get; set; } = string.Empty;
    public string Scene { get; set; } = string.Empty;

    public string FilterNameAtStartForSceneItems { get; set; } = "pov";
    public bool IsUsingPaceMan { get; set; } = true;
    public bool IsUsingWhitelistOnPaceMan { get; set; } = true;

    private int _paceManRefreshRateMiliseconds = 10000;
    public int PaceManRefreshRateMiliseconds
    {
        get => _paceManRefreshRateMiliseconds;
        set
        {
            if (value < 10000)
                _paceManRefreshRateMiliseconds = 10000;
            else
                _paceManRefreshRateMiliseconds = value;
            OnPropertyChanged(nameof(PaceManRefreshRateMiliseconds));
        }
    }


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

    public void UpdatePlayers()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            Players[i].LoadHead();
        }
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

    public void AddPlayer(Player player)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Players.Add(player);
        });
    }

    public bool IsNameDuplicate(string? twitchName)
    {
        if (string.IsNullOrEmpty(twitchName)) return false;
        return Players.Any(player => player.TwitchName!.Equals(twitchName, StringComparison.OrdinalIgnoreCase));
    }

    public void Clear()
    {
        Port = 4455;
        Password = string.Empty;
        SceneCollection = string.Empty;
        Scene = string.Empty;

        Players = [];
        FilterNameAtStartForSceneItems = "pov";
        IsUsingPaceMan = true;
        PaceManRefreshRateMiliseconds = 10000;
    }
}
