using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.ManagementPanels;

public abstract class ManagementPanel : BaseViewModel
{
    public abstract void InitializeAPI(APIDataSaver api);
    public abstract void UpdateAPI(APIDataSaver api);
}
