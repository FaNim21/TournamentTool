using TournamentTool.Core.Extensions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable.Controller.SidePanel;

public class RankedPacePanel : GroupedPanelBase<RankedPaceViewModel, RankedPace>, IRankedDataReceiver
{
    private const int UiSendBatchSize = 7;

    
    public RankedPacePanel(ControllerViewModel controller, IDispatcherService dispatcher) : base(controller, dispatcher)
    {
        Mode = ControllerMode.Ranked;
    }

    public void AddPace(RankedPace pace)
    {
        Dispatcher.Invoke(() => { AddItem(pace); });
    }
    public void AddPaces(IEnumerable<RankedPace> rankedPaces)
    {
        foreach (var batch in rankedPaces.Batch(UiSendBatchSize))
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

    public void Clear()
    {
        Dispatcher.Invoke(() =>
        {
            _allItems.Clear();
            Groups.Clear();
        });
    }

    protected override RankedPaceViewModel CreateViewModel(RankedPace model)
    {
        return new RankedPaceViewModel(model, Dispatcher);
    }
}
