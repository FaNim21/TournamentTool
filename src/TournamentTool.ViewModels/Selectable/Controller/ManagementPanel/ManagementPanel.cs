using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services.Background;

namespace TournamentTool.ViewModels.Selectable.Controller.ManagementPanel;

public abstract class ManagementPanel : BaseViewModel, IBackgroundDataReceiver
{
    protected ManagementPanel(IDispatcherService dispatcher) : base(dispatcher) { }

    public abstract void InitializeAPI();
    public abstract void UpdateAPI();
}
