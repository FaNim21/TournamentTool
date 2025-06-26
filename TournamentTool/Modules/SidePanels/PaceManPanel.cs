using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using MethodTimer;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.SidePanels;

//TODO: 0 Zrobic tutaj abstract pod ogolnie side panel pace'y, ale na razie robie oddzielnie pod szybkie wykonczenia bazy
public class PaceManGroup : BaseViewModel
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
    
    private SplitType _splitType;
    public SplitType SplitType
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

    private ObservableCollection<PaceManViewModel> _paces = [];
    public ObservableCollection<PaceManViewModel> Paces
    {
        get => _paces;
        set
        {
            _paces = value;
            OnPropertyChanged(nameof(Paces));
        }
    }

    public PaceManGroup(SplitType splitType)
    {
        SplitType = splitType;
    }

    public void AddPace(PaceManViewModel pace)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.Add(pace); });
        UpdatePacesAmount();
    }
    public void RemovePace(PaceManViewModel pace)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.Remove(pace); });
        UpdatePacesAmount();
    }
    public void RemovePaceAt(int index)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.RemoveAt(index); });
    }
    public void InsertPace(int index, PaceManViewModel pace)
    {
        Application.Current.Dispatcher.Invoke(() => { Paces.Insert(index, pace); });
        UpdatePacesAmount();
    }

    private void UpdatePacesAmount()
    {
        PacesAmount = Paces.Count;
    }
}

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

    private ObservableCollection<PaceManGroup> _groups = [];
    public ObservableCollection<PaceManGroup> Groups
    {
        get => _groups;
        set
        {
            _groups = value;
            OnPropertyChanged(nameof(Groups));
        }
    }

    private List<PaceManViewModel> _allPaces = [];
    private bool _lastOutput;
    
    public PaceManPanel(ControllerViewModel controller) : base(controller)
    {
        Mode = ControllerMode.Paceman;
    }
    public override bool OnDisable()
    {
        base.OnDisable();

        _allPaces.Clear();
        Groups.Clear();
        SelectedPlayer = null;

        return true;
    }

    public void AddPace(Paceman paceman)
    {
        var duplicate = _allPaces.FirstOrDefault(p => p.InGameName == paceman.Nickname);
        if (duplicate != null) return;
        
        var viewModel = new PaceManViewModel(paceman);
        _allPaces.Add(viewModel);
        AddPaceToGroup(viewModel);
        RefreshGroup(_allPaces.Count == 0);
    }
    public void Remove(Paceman paceman)
    {
        var paceToRemove = _allPaces.FirstOrDefault(p => p.InGameName == paceman.Nickname);
        if (paceToRemove == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            _allPaces.Remove(paceToRemove);
        });
        RemovePaceFromGroup(paceToRemove);
        RefreshGroup(_allPaces.Count == 0);
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

                bool IsSplitTypeChanged = pace.ModelSplitType != oldSplitType;
                bool IsSplitTimeChanged = pace.ModelCurrentSplitTimeMiliseconds != oldTime;
                
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

    private void AddPaceToGroup(PaceManViewModel pace)
    {
        var group = Groups.FirstOrDefault(g => g.SplitType == pace.SplitType);
        if (group == null)
        {
            group = new PaceManGroup(pace.SplitType);
            
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

    private void RemovePaceFromGroup(PaceManViewModel pace)
    {
        var group = Groups.FirstOrDefault(g => g.SplitType == pace.SplitType);
        if (group == null) return;
        
        group.RemovePace(pace);
            
        if (group.Paces.Count == 0)
        {
            RemoveGroup(group);
        }
    }

    private void MovePaceToNewGroup(PaceManViewModel pace, SplitType oldSplitType)
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

    private void ResortPaceInGroup(PaceManViewModel pace)
    {
        var group = Groups.FirstOrDefault(g => g.SplitType == pace.SplitType);
        if (group == null || group.Paces.Count <= 1) return;

        var currentIndex = group.Paces.IndexOf(pace);
        if (currentIndex == -1) return;

        group.RemovePaceAt(currentIndex);
        InsertPaceSorted(group, pace);
    }

    private void InsertPaceSorted(PaceManGroup group, PaceManViewModel pace)
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

    public void FilterItems()
    {
        RefreshControllerPlayers();
    }

    public void RefreshGroup(bool isEmpty)
    {
        if (isEmpty == _lastOutput) return;
        EmptyRunsTitle = isEmpty ? "No active pace or stream" : string.Empty;
        _lastOutput = isEmpty;
    }

    private void InsertGroup(int index, PaceManGroup group)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Groups.Insert(index, group);
        });
    }
    private void RemoveGroup(PaceManGroup group)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Groups.Remove(group);
        });
    }
}
