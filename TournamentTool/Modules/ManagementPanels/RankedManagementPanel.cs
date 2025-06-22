using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.ManagementPanels;

public class RankedManagementPanel : ManagementPanel, IRankedManagementDataReceiver
{
    private RankedManagementData RankedManagementData { get; set; }

    //TODO: 0 przerobic to na model-viwemodel w formie na biezaco aktualizowania danych managementu
    private string _customText = string.Empty;
    public string CustomText
    {
        get => _customText; 
        set
        {
            _customText = value;
            OnPropertyChanged(nameof(CustomText));
        }
    }

    private int _rounds;
    public int Rounds
    {
        get => _rounds; 
        set
        {
            _rounds = value;
            OnPropertyChanged(nameof(Rounds));
        }
    }

    private int _completions;
    public int Completions
    {
        get => _completions; 
        set
        {
            _completions = value;
            OnPropertyChanged(nameof(Completions));
        }
    }

    private int _players;
    public int Players
    {
        get => _players; 
        set
        {
            _players = value;
            OnPropertyChanged(nameof(Players));
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

    private long _saveStartedTime;

    private const string _rankedPlayerCountFileName = "Ranked_players_count";
    private const string _rankedCompletedCountFileName = "Ranked_completes_count";
    private const string _rankedRoundsFileName = "Ranked_rounds";
    private const string _rankedCustomTextFileName = "Ranked_customText";


    public RankedManagementPanel(RankedManagementData managementData)
    {
        RankedManagementData = managementData;
        
        AddRoundCommand = new RelayCommand(() => { Rounds++; });
        SubtractRoundCommand = new RelayCommand(() => { Rounds--; });
    }

    public override void OnEnable(object? parameter) { }
    public override bool OnDisable() => true;

    public override void InitializeAPI(APIDataSaver api)
    {
        if (int.TryParse(api.CheckFile(_rankedPlayerCountFileName), out int players)) { Players = players; }
        if (int.TryParse(api.CheckFile(_rankedCompletedCountFileName), out int completions)) { Completions = completions; }

        api.CheckFile(_rankedRoundsFileName);
        api.CheckFile(_rankedCustomTextFileName);

        if (RankedManagementData == null) return;
        Rounds = RankedManagementData.Rounds;
        CustomText = RankedManagementData.CustomText;

        Application.Current.Dispatcher.Invoke(() =>
        {
            BestSplits.Clear();
            foreach (var bestSplit in RankedManagementData.BestSplits)
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

    public void UpdateManagementData(List<RankedBestSplit> bestSplits, int completedCount, long timeStarted, int playersCount)
    {
        if (_saveStartedTime != timeStarted)
        {
            _saveStartedTime = timeStarted;
            DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(timeStarted).ToLocalTime().DateTime;
            TimeStartedText = date.ToString("dd MMM yyyy hh:mm:ss tt", CultureInfo.CurrentCulture);
        }
        
        Completions = completedCount;
        Players = playersCount;
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            BestSplits.Clear();
            foreach (var bestSplit in bestSplits)
            {
                var viewModel = new RankedBestSplitViewModel(bestSplit);
                BestSplits.Add(viewModel);
            }
        });
    }
}
