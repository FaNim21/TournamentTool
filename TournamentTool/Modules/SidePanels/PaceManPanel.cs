using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Services;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.SidePanels;

public class PaceManPanel : SidePanel
{
    private readonly PaceManService _paceManService;

    private string _emptyRunsTitle = string.Empty;
    public string EmptyRunsTitle
    {
        get => _emptyRunsTitle;
        set
        {
            _emptyRunsTitle = value;
            OnPropertyChanged(nameof(EmptyRunsTitle));
        }
    }

    public ICollectionView? GroupedPaceManPlayers { get; set; }

    private bool _lastOutput;


    public PaceManPanel(ControllerViewModel controller, TournamentViewModel tournamentViewModel, IPresetSaver presetSaver) : base(controller)
    {
        _paceManService = new PaceManService(this, tournamentViewModel);
        Mode = ControllerMode.Paceman;
    }

    public override void Initialize()
    {
        _paceManService.OnRefreshGroup += RefreshGroup;
        _paceManService.OnEnable(null);
    }

    public override void UnInitialize()
    {
        _paceManService.OnRefreshGroup -= RefreshGroup;
        _paceManService.OnDisable();
    }
    
    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);
        
        SetupPaceManGrouping();
    }
    public override bool OnDisable()
    {
        base.OnDisable();

        SelectedPlayer = null;
        GroupedPaceManPlayers = null;

        return true;
    }

    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = _paceManService.PaceManPlayers };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PaceManViewModel.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceManViewModel.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceManViewModel.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedPaceManPlayers = collectionViewSource.View;
    }

    public void RefreshGroup(bool isEmpty)
    {
        if (GroupedPaceManPlayers != null)
        {
            Application.Current.Dispatcher.Invoke(() => { GroupedPaceManPlayers.Refresh(); });
        }

        if (isEmpty == _lastOutput) return;
        EmptyRunsTitle = isEmpty ? "No active pace or stream" : string.Empty;
        _lastOutput = isEmpty;
    }
}
