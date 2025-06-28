using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.SidePanels;

public class PaceManPanel : GroupedPanelBase<PaceManViewModel, Paceman>, IPacemanDataReceiver
{
    public PaceManPanel(ControllerViewModel controller) : base(controller)
    {
        Mode = ControllerMode.Paceman;
    }

    public void AddPace(Paceman paceman)
    {
        AddItem(paceman);
    }
    public void Remove(Paceman paceman)
    {
        RemoveItem(paceman.Nickname);
    }

    protected override PaceManViewModel CreateViewModel(Paceman model)
    {
        return new PaceManViewModel(model);
    }

    public void FilterItems()
    {
        RefreshControllerPlayers();
    }
}
