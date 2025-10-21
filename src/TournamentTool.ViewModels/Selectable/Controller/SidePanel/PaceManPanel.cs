using TournamentTool.Core.Extensions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable.Controller.SidePanel;

public class PaceManPanel : GroupedPanelBase<PaceManViewModel, Paceman>, IPacemanDataReceiver
{
    private const int UiSendBatchSize = 2;
    
    
    public PaceManPanel(ControllerViewModel controller, IDispatcherService dispatcher) : base(controller, dispatcher)
    {
        Mode = ControllerMode.Paceman;
    }

    public void AddPace(Paceman paceman)
    {
        Dispatcher.Invoke(() => { AddItem(paceman); });
    }
    public void AddPaces(IEnumerable<Paceman> pacemans)
    {
        foreach (var batch in pacemans.Batch(UiSendBatchSize))
        {
            Dispatcher.InvokeAsync(() =>
            {
                foreach (var player in batch)
                {
                    AddItem(player);
                }
            }, CustomDispatcherPriority.Background);
        }
    }
    public void Remove(Paceman paceman)
    {
        RemoveItem(paceman.Nickname);
    }

    protected override PaceManViewModel CreateViewModel(Paceman model)
    {
        return new PaceManViewModel(model, Dispatcher);
    }

    public void FilterItems()
    {
        RefreshControllerPlayers();
    }
}
