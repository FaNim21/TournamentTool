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
        
        OrganizePaces(players);
        RefreshGroup(false);
    }
    private void OrganizePaces(List<PaceManViewModel> receivedPaces)
    {
        List<PaceManViewModel> currentPaces = new(PaceManPlayers);
        
        foreach (var pace in receivedPaces)
        {
            bool wasPaceFound = false;
            
            for (int j = 0; j < currentPaces.Count; j++)
            {
                var currentPace = currentPaces[j];
                if (!pace.Nickname.Equals(currentPace.Nickname, StringComparison.OrdinalIgnoreCase)) continue;
                
                wasPaceFound = true;
                currentPace.Update(pace.GetData());
                currentPaces.Remove(currentPace);
                break;
            }

            if (wasPaceFound) continue;

            Application.Current.Dispatcher.Invoke(() =>
            {
                PaceManPlayers.Add(pace);
            });
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < currentPaces.Count; i++)
            {
                PaceManPlayers.Remove(currentPaces[i]);
            }
        });
    }

    public void FilterItems()
    {
        RefreshControllerPlayers();
    }
    
    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = PaceManPlayers, IsLiveGroupingRequested = true };

        collectionViewSource.GroupDescriptions.Clear();
        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PaceManViewModel.SplitName)));
        collectionViewSource.SortDescriptions.Clear();
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceManViewModel.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceManViewModel.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PlayerViewModel.isStreamLive), ListSortDirection.Descending));

        GroupedPaceManPlayers = collectionViewSource.View;
    }
    public void RefreshGroup(bool isEmpty)
    {
        if (isEmpty == _lastOutput) return;
        EmptyRunsTitle = isEmpty ? "No active pace or stream" : string.Empty;
        _lastOutput = isEmpty;
    }
}
