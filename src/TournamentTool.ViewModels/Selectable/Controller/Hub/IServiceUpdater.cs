namespace TournamentTool.ViewModels.Selectable.Controller.Hub;

public interface IServiceUpdater
{
    Task UpdateAsync(CancellationToken token);
    void OnEnable();
    void OnDisable();
}