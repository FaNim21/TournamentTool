using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using TournamentTool.Models;
using TournamentTool.Services;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.SidePanels;

public class PaceManPanel : SidePanel
{
    private readonly PaceManService _paceManService;

    public ICollectionView? GroupedPaceManPlayers { get; set; }


    public PaceManPanel(ControllerViewModel controller) : base(controller)
    {
        _paceManService = new(controller);
    }

    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);
        SetupPaceManGrouping();

        _paceManService.OnRefreshGroup += RefreshGroup;
        _paceManService.OnEnable(parameter);
    }
    public override bool OnDisable()
    {
        base.OnDisable();
        _paceManService.OnRefreshGroup -= RefreshGroup;
        _paceManService.OnDisable();

        SelectedPlayer = null;
        GroupedPaceManPlayers = null;

        return true;
    }

    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = _paceManService.PaceManPlayers };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PaceMan.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedPaceManPlayers = collectionViewSource.View;
    }

    public void RefreshGroup()
    {
        Application.Current.Dispatcher.Invoke(() => { GroupedPaceManPlayers?.Refresh(); });
    }
}
