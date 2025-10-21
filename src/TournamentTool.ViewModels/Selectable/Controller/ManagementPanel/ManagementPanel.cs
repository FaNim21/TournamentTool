using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Services.Background;

namespace TournamentTool.ViewModels.Selectable.Controller.ManagementPanel;

public abstract class ManagementPanel : BaseViewModel, IBackgroundDataReceiver
{
    protected ManagementPanel(IDispatcherService dispatcher) : base(dispatcher) { }
    
    public abstract void InitializeAPI(APIDataSaver api);
    public abstract Task UpdateAPI(APIDataSaver api);
}
