using TournamentTool.Interfaces;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.ManagementPanels;

public abstract class ManagementPanel : BaseViewModel, IBackgroundDataReceiver
{
    public abstract void InitializeAPI(APIDataSaver api);
    public abstract Task UpdateAPI(APIDataSaver api);
}
