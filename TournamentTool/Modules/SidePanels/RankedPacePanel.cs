using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.Interfaces;
using TournamentTool.Utils;

namespace TournamentTool.Modules.SidePanels;

public class RankedPaceGroup : BaseViewModel
{
    private string _splitName = string.Empty;
    public string SplitName
    {
        get => _splitName;
        set
        {
            _splitName = value;
            OnPropertyChanged(nameof(SplitName));
        }
    }
    
    private RankedSplitType _splitType;
    public RankedSplitType SplitType
    {
        get => _splitType;
        init
        {
            _splitType = value;
            SplitName = value.ToString().Replace('_', ' ').CaptalizeAll();
            OnPropertyChanged(nameof(SplitType));
        }
    }

    private int _pacesAmount;
    public int PacesAmount
    {
        get => _pacesAmount;
        set
        {
            _pacesAmount = value;
            OnPropertyChanged(nameof(PacesAmount));
        }
    }

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

    public RankedPaceGroup(RankedSplitType splitType)
    {
        SplitType = splitType;
    }

    public void AddPace(RankedPaceViewModel pace)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.Add(pace); });
        UpdatePacesAmount();
    }
    public void RemovePace(RankedPaceViewModel pace)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.Remove(pace); });
        UpdatePacesAmount();
    }
    public void RemovePaceAt(int index)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.RemoveAt(index); });
    }
    public void InsertPace(int index, RankedPaceViewModel pace)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.Insert(index, pace); });
        UpdatePacesAmount();
    }

    private void UpdatePacesAmount()
    {
        PacesAmount = Paces.Count;
    }
}

public class RankedPacePanel : SidePanel, IRankedDataReceiver
{
    private ObservableCollection<RankedPaceGroup> _groups = [];
    public ObservableCollection<RankedPaceGroup> Groups
    {
        get => _groups;
        set
        {
            _groups = value;
            OnPropertyChanged(nameof(Groups));
        }
    }

    private List<RankedPaceViewModel> _allPaces = [];

    public RankedPacePanel(ControllerViewModel controller) : base(controller)
    {
        Mode = ControllerMode.Ranked;
    }

    public override bool OnDisable()
    {
        base.OnDisable();

        _allPaces.Clear();
        Groups.Clear();
        SelectedPlayer = null;

        return true;
    }

    public void AddPace(RankedPace pace)
    {
        var duplicate = _allPaces.FirstOrDefault(p => p.InGameName == pace.InGameName);
        if (duplicate != null) return;
        
        var viewModel = new RankedPaceViewModel(pace);
        _allPaces.Add(viewModel);
        AddPaceToGroup(viewModel);
    }
    public void Update()
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            for (int i = 0; i < _allPaces.Count; i++)
            {
                var pace = _allPaces[i];
                
                var oldSplitType = pace.SplitType;
                var oldTime = pace.CurrentSplitTimeMiliseconds;

                bool IsSplitTypeChanged = pace.Data.SplitType != oldSplitType;
                bool IsSplitTimeChanged = pace.Data.CurrentSplitTimeMiliseconds != oldTime;
                
                pace.Update();
                
                if (IsSplitTypeChanged)
                {
                    MovePaceToNewGroup(pace, oldSplitType);
                }
                else if (IsSplitTimeChanged)
                {
                    ResortPaceInGroup(pace);
                }
            }
        }, DispatcherPriority.Background);
    }
    public void Clear()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _allPaces.Clear();
            Groups.Clear();
        }, DispatcherPriority.Background);
    }
    
    private void AddPaceToGroup(RankedPaceViewModel pace)
    {
        var group = Groups.FirstOrDefault(g => g.SplitType == pace.SplitType);
        if (group == null)
        {
            group = new RankedPaceGroup(pace.SplitType);
            
            //Sortowanie grup malejaco
            var insertIndex = 0;
            for (int i = 0; i < Groups.Count; i++)
            {
                if (Groups[i].SplitType < group.SplitType)
                {
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;
            }

            InsertGroup(insertIndex, group);
        }

        InsertPaceSorted(group, pace);
    }

    private void MovePaceToNewGroup(RankedPaceViewModel pace, RankedSplitType oldSplitType)
    {
        //Tutaj biore element zeby go usunac ze starej grupy
        var oldGroup = Groups.FirstOrDefault(g => g.SplitType == oldSplitType);
        if (oldGroup != null)
        {
            oldGroup.RemovePace(pace);
            
            if (oldGroup.Paces.Count == 0)
            {
                RemoveGroup(oldGroup);
            }
        }

        AddPaceToGroup(pace);
    }

    private void ResortPaceInGroup(RankedPaceViewModel pace)
    {
        var group = Groups.FirstOrDefault(g => g.SplitType == pace.SplitType);
        if (group == null || group.Paces.Count <= 1) return;

        var currentIndex = group.Paces.IndexOf(pace);
        if (currentIndex == -1) return;

        group.RemovePaceAt(currentIndex);
        InsertPaceSorted(group, pace);
    }

    private void InsertPaceSorted(RankedPaceGroup group, RankedPaceViewModel pace)
    {
        //Tutaj sortowanie w grupie (ladniej to trzeba zrobic)
        var index = 0;
        for (int i = 0; i < group.Paces.Count; i++)
        {
            var comparison = pace.CurrentSplitTimeMiliseconds.CompareTo(group.Paces[i].CurrentSplitTimeMiliseconds);
            if (comparison < 0)
            {
                index = i;
                break;
            }

            if (comparison == 0)
            {
                //Tie breaker na ign (rare, bo co do milisekund)
                if (string.Compare(pace.InGameName, group.Paces[i].InGameName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    index = i;
                    break;
                }
            }
            index = i + 1;
        }

        if (index >= group.Paces.Count)
        {
            group.AddPace(pace);
        }
        else
        {
            group.InsertPace(index, pace);
        }
    }

    private void InsertGroup(int index, RankedPaceGroup group)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Groups.Insert(index, group);
        });
    }
    private void RemoveGroup(RankedPaceGroup group)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Groups.Remove(group);
        });
    }
}
