namespace TournamentTool.Domain.Interfaces;

public interface ISettingsTab
{
    bool IsChosen { get; }
    string Name { get; }
   
    void OnOpen();
    void OnClose();
}

public interface ISettingsProvider
{
    T Get<T>() where T : class;
}

public interface ISettingsSaver
{
    void Save();
    void Load();
}