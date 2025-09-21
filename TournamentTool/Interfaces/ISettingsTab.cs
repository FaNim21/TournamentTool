using TournamentTool.Models;

namespace TournamentTool.Interfaces;

public interface ISettingsTab
{
    bool IsChosen { get; }
    string Name { get; }
   
    void OnOpen();
    void OnClose();
}

public interface ISettings
{
    public Settings Settings { get; }
    public APIKeys APIKeys { get; }
}

public interface ISettingsSaver
{
    void Save();
    void Load();
}