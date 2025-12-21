namespace TournamentTool.Services.Configuration;

internal interface ISettingsFile<T>
{
    T Load();
    void Save(T data);
}