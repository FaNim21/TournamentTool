using System.Windows;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.Interfaces;

namespace TournamentTool.Modules.SidePanels;

public class RankedPacePanel : GroupedPanelBase<RankedPaceViewModel, RankedPace>, IRankedDataReceiver
{
    public RankedPacePanel(ControllerViewModel controller) : base(controller)
    {
        Mode = ControllerMode.Ranked;
    }


    public void AddPace(RankedPace pace)
    {
        AddItem(pace);
    }
    public void Clear()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _allItems.Clear();
            Groups.Clear();
        });
    }

    protected override RankedPaceViewModel CreateViewModel(RankedPace model)
    {
        return new RankedPaceViewModel(model);
    }
}
