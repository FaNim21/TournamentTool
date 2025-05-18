using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using MethodTimer;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.SidePanels;

public class PaceManPanel : SidePanel, IPacemanDataReceiver
{
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
    
    private ObservableCollection<PaceManViewModel> _paceManPlayers = [];
    public ObservableCollection<PaceManViewModel> PaceManPlayers
    {
        get => _paceManPlayers;
        set
        {
            _paceManPlayers = value;
            OnPropertyChanged(nameof(PaceManPlayers));
        }
    }

    public ICollectionView? GroupedPaceManPlayers { get; set; }

    private bool _lastOutput;


    public PaceManPanel(ControllerViewModel controller) : base(controller)
    {
        Mode = ControllerMode.Paceman;
    }
    
    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);
        
        SetupPaceManGrouping();
    }
    public override bool OnDisable()
    {
        base.OnDisable();

        for (int i = 0; i < PaceManPlayers.Count; i++)
            PaceManPlayers[i].PlayerViewModel = null;

        PaceManPlayers.Clear();
        
        SelectedPlayer = null;
        GroupedPaceManPlayers = null;

        return true;
    }
    
    public void ReceivePlayers(List<PaceManViewModel> players)
    {
        if (players == null || players.Count == 0)
        {
            Application.Current.Dispatcher.Invoke(() => { PaceManPlayers.Clear(); });
            RefreshGroup(true);
            return;
        }
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            PaceManPlayers.Clear();
            foreach (var player in players)
            {
                player.Update();
                PaceManPlayers.Add(player);
            }
        });
        
        RefreshGroup(false);
    }

    public void FilterItems()
    {
        FilterControllerPlayers();
    }
    
    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = PaceManPlayers };

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
