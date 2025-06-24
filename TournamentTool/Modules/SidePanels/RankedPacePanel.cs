using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.Interfaces;

namespace TournamentTool.Modules.SidePanels;

public class RankedPacePanel : SidePanel, IRankedDataReceiver
{
    private ObservableCollection<RankedPaceViewModel> _paces = [];
    public ObservableCollection<RankedPaceViewModel> Paces
    {
        get => _paces;
        set
        {
            _paces = value;
            OnPropertyChanged(nameof(Paces));
        }
    }

    public ICollectionView? GroupedRankedPaces { get; set; }


    public RankedPacePanel(ControllerViewModel controller) : base(controller)
    {
        Mode = ControllerMode.Ranked;
    }

    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);

        SetupRankedPaceGrouping();
    }
    public override bool OnDisable()
    {
        base.OnDisable();
        
        Paces.Clear();
        GroupedRankedPaces = null;
        return true;
    }

    private void SetupRankedPaceGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = Paces, IsLiveGroupingRequested = true };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RankedPaceViewModel.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(RankedPaceViewModel.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(RankedPaceViewModel.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedRankedPaces = collectionViewSource.View;
    }
    
    public void ReceiveAllPaces(List<RankedPace> paces)
    {
        for (int i = 0; i < paces.Count; i++)
        {
            RankedPaceViewModel viewModel = new(paces[i]);
            Paces.Add(viewModel);
        }
    }
    
    public void Update()
    {
        for (int i = 0; i < Paces.Count; i++)
        {
            Paces[i].Update();
        }
    }
    
    public void AddPace(RankedPace pace)
    {
        RankedPaceViewModel viewModel = new(pace);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Paces.Add(viewModel);
        });
    }
    public void RemovePace(RankedPace pace)
    {
        var paceViewModel = Paces.FirstOrDefault(p => p.Data.InGameName == pace.InGameName);
        if (paceViewModel == null) return;
        Application.Current.Dispatcher.Invoke(() =>
        {
            Paces.Remove(paceViewModel);
        });
    }
    
    public void Clear()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Paces.Clear();
        });
    }
}
