using TournamentTool.Utils;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Modules.Controller;

public class APIUpdaterService : ServiceUpdater
{
    private readonly ControllerViewModel _controller;
    private readonly APIDataSaver _api;

    
    public APIUpdaterService(ControllerViewModel controller)
    {
        _controller = controller;
        
        _api = new APIDataSaver();
    }
    public void OnEnable()
    {
        _controller.ManagementPanel?.InitializeAPI(_api);
    }
    public void OnDisable()
    {
        
    }

    public Task UpdateAsync(CancellationToken token)
    {
        _controller.ManagementPanel?.UpdateAPI(_api);
        return Task.CompletedTask;
    }
}