namespace TournamentTool.Services.Configuration;

internal interface IFileStorage<T> where T : class, new()
{
    T Load();
    void Save(T data);
}