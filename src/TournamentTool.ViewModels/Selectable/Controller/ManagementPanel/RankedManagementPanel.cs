using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Services.Background;
using TournamentTool.Services.Factories;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable.Controller.ManagementPanel;

public class RankedManagementPanel : ManagementPanel, IRankedManagementDataReceiver
{
    private readonly IManagementDataContext<RankedManagementData> _managementDataContext;

    public string CustomText
    {
        get => _managementDataContext.Get(m => m.CustomText); 
        set
        {
            if (CustomText.Equals(value)) return;

            _managementDataContext.Set(m => m.CustomText, value);
            OnPropertyChanged();
        }
    }
    public int Rounds
    {
        get => _managementDataContext.Get(m => m.Rounds);
        set
        {
            if (Rounds == value) return;

            _managementDataContext.Set(m => m.Rounds, value);
            OnPropertyChanged();
        }
    }
    public int Completions
    {
        get => _managementDataContext.Get(m => m.Completions); 
        set
        {
            if (Completions == value) return;

            _managementDataContext.Set(m => m.Completions, value);
            OnPropertyChanged();
        }
    }
    public int Players
    {
        get => _managementDataContext.Get(m => m.Completions);  
        set
        {
            if (Players == value) return;
            
            _managementDataContext.Set(m => m.Players, value);
            OnPropertyChanged();
        }
    }
    public long StartTime 
    {
        get => _managementDataContext.Get(m => m.StartTime);   
        set
        {
            _managementDataContext.Set(m => m.StartTime, value);
            OnPropertyChanged();
        } 
    }
    
    private string _timeStartedText = string.Empty;
    public string TimeStartedText
    {
        get => _timeStartedText; 
        set
        {
            _timeStartedText = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<RankedBestSplitViewModel> BestSplits { get; } = [];

    public ICommand AddRoundCommand { get; set; }
    public ICommand SubtractRoundCommand { get; set; }

    private long _oldStartTime;


    public RankedManagementPanel(IManagementDataContextFactory managementDataContextFactory, IDispatcherService dispatcher) : base(dispatcher)
    {
        _managementDataContext = managementDataContextFactory.Create<RankedManagementData>();

        AddRoundCommand = new RelayCommand(() => { Rounds++; });
        SubtractRoundCommand = new RelayCommand(() => { Rounds--; });
    }
    
    public void Update()
    {
        if (StartTime != _oldStartTime)
        {
            _oldStartTime = StartTime;
            DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(StartTime).ToLocalTime().DateTime;
            TimeStartedText = date.ToString("dd MMM yyyy hh:mm:ss tt", CultureInfo.CurrentCulture);
        }

        List<PrivRoomBestSplit> bestSplitsDatas = _managementDataContext.Get(m => m.BestSplitsDatas);        
        bool refreshUI = _managementDataContext.Get(m => m.RefreshUI);        
        
        if (bestSplitsDatas.Count == 0 || refreshUI)
        {
            _managementDataContext.Set(m => m.RefreshUI, false);
            Dispatcher.Invoke(() => BestSplits.Clear());
        }
        for (var i = 0; i < bestSplitsDatas.Count; i++)
        {
            PrivRoomBestSplit bestSplit = bestSplitsDatas[i];
            if (i < BestSplits.Count) continue;
            
            Dispatcher.Invoke(() =>
            {
                var viewModel = new RankedBestSplitViewModel(bestSplit, Dispatcher);
                BestSplits.Add(viewModel);
            });
        }

        for (int i = 0; i < bestSplitsDatas.Count; i++)
        {
            PrivRoomBestSplit bestSplit = bestSplitsDatas[i];
            RankedBestSplitViewModel bestSplitViewModel = BestSplits[i];
            
            for (int j = 0; j < bestSplit.Datas.Count; j++)
            {
                PrivRoomBestSplitData data = bestSplit.Datas[j];
                if (j < bestSplitViewModel.Splits.Count) continue;

                Dispatcher.Invoke(() =>
                {
                    RankedBestSplitDataViewModel dataViewModel = new RankedBestSplitDataViewModel(data, Dispatcher);
                    bestSplitViewModel.Splits.Add(dataViewModel);
                });
            }
        }
        
        OnPropertyChanged(nameof(CustomText));
        OnPropertyChanged(nameof(Rounds));
        OnPropertyChanged(nameof(Players));
        OnPropertyChanged(nameof(Completions));
        OnPropertyChanged(nameof(StartTime));
    }
}