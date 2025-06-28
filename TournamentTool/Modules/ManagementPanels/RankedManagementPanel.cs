using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.ManagementPanels;

public class RankedManagementPanel : ManagementPanel, IRankedManagementDataReceiver
{
    private RankedManagementData _data;

    public string CustomText
    {
        get => _data.CustomText; 
        set
        {
            _data.CustomText = value;
            OnPropertyChanged(nameof(CustomText));
        }
    }
    public int Rounds
    {
        get => _data.Rounds; 
        set
        {
            _data.Rounds = value;
            OnPropertyChanged(nameof(Rounds));
        }
    }
    public int Completions
    {
        get => _data.Completions; 
        set
        {
            _data.Completions = value;
            OnPropertyChanged(nameof(Completions));
        }
    }
    public int Players
    {
        get => _data.Players; 
        set
        {
            _data.Players = value;
            OnPropertyChanged(nameof(Players));
        }
    }
    public long StartTime 
    {
        get => _data.StartTime;
        set
        {
            _data.StartTime = value;  
            OnPropertyChanged(nameof(StartTime));
        } 
    }
    
    private string _timeStartedText = string.Empty;
    public string TimeStartedText
    {
        get => _timeStartedText; 
        set
        {
            _timeStartedText = value;
            OnPropertyChanged(nameof(TimeStartedText));
        }
    }

    private ObservableCollection<RankedBestSplitViewModel> _bestSplits = [];
    public ObservableCollection<RankedBestSplitViewModel> BestSplits
    {
        get => _bestSplits;
        set
        {
            _bestSplits = value;
            OnPropertyChanged(nameof(BestSplits));
        }
    }

    public ICommand AddRoundCommand { get; set; }
    public ICommand SubtractRoundCommand { get; set; }

    private long _oldStartTime;

    private const string _rankedPlayerCountFileName = "Ranked_players_count";
    private const string _rankedCompletedCountFileName = "Ranked_completes_count";
    private const string _rankedRoundsFileName = "Ranked_rounds";
    private const string _rankedCustomTextFileName = "Ranked_customText";


    public RankedManagementPanel(RankedManagementData managementData)
    {
        _data = managementData;
        
        AddRoundCommand = new RelayCommand(() => { Rounds++; });
        SubtractRoundCommand = new RelayCommand(() => { Rounds--; });
    }

    public override void OnEnable(object? parameter) { }
    public override bool OnDisable() => true;

    public override void InitializeAPI(APIDataSaver api)
    {
        api.CheckFile(_rankedPlayerCountFileName);
        api.CheckFile(_rankedCompletedCountFileName);
        api.CheckFile(_rankedRoundsFileName);
        api.CheckFile(_rankedCustomTextFileName);

        Update();
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            BestSplits.Clear();
            foreach (var bestSplit in _data.BestSplits)
            {
                var viewModel = new RankedBestSplitViewModel(bestSplit);
                BestSplits.Add(viewModel);
            }
        });
    }

    public override void UpdateAPI(APIDataSaver api)
    {
        api.UpdateFileContent(_rankedCompletedCountFileName, Completions);
        api.UpdateFileContent(_rankedPlayerCountFileName, Players);

        api.UpdateFileContent(_rankedRoundsFileName, Rounds);
        api.UpdateFileContent(_rankedCustomTextFileName, CustomText);
    }

    public void Update()
    {
        if (StartTime != _oldStartTime)
        {
            _oldStartTime = StartTime;
            DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(StartTime).ToLocalTime().DateTime;
            TimeStartedText = date.ToString("dd MMM yyyy hh:mm:ss tt", CultureInfo.CurrentCulture);
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            BestSplits.Clear();
            foreach (var bestSplit in _data.BestSplits)
            {
                var viewModel = new RankedBestSplitViewModel(bestSplit);
                BestSplits.Add(viewModel);
            }
        });
        
        OnPropertyChanged(nameof(CustomText));
        OnPropertyChanged(nameof(Rounds));
        OnPropertyChanged(nameof(Players));
        OnPropertyChanged(nameof(Completions));
        OnPropertyChanged(nameof(StartTime));
    }
}
