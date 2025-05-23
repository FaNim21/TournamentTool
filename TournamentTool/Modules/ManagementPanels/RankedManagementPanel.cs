﻿using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.ManagementPanels;

public class RankedManagementPanel : ManagementPanel
{
    private ControllerViewModel Controller { get; set; }
    private RankedPacePanel RankedPacePanel { get; set; }
    private RankedManagementData? RankedManagementData { get; set; }


    //TODO: 2 Dodac tutaj liste pod najlepsze czasy do splitow, zeby byl i wglad w nie i tez zeby po restarcie tt czasy sie dalej zgadzaly
    
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

    public ICommand AddRoundCommand { get; set; }
    public ICommand SubtractRoundCommand { get; set; }

    private const string _rankedPlayerCountFileName = "Ranked_players_count";
    private const string _rankedCompletedCountFileName = "Ranked_completes_count";
    private const string _rankedRoundsFileName = "Ranked_rounds";
    private const string _rankedCustomTextFileName = "Ranked_customText";


    public RankedManagementPanel(ControllerViewModel controller, RankedPacePanel rankedPacePanel)
    {
        Controller = controller;
        RankedPacePanel = rankedPacePanel;

        AddRoundCommand = new RelayCommand(() => { Rounds++; });
        SubtractRoundCommand = new RelayCommand(() => { Rounds--; });

        RankedManagementData = (RankedManagementData)controller.TournamentViewModel.ManagementData!;
    }

    public override void OnEnable(object? parameter) { }
    public override bool OnDisable()
    {
        if (RankedManagementData == null) return true;

        RankedManagementData.Rounds = Rounds;
        RankedManagementData.CustomText = CustomText;

        return true;
    }

    public override void InitializeAPI(APIDataSaver api)
    {
        if (int.TryParse(api.CheckFile(_rankedPlayerCountFileName), out int players)) { Players = players; }
        if (int.TryParse(api.CheckFile(_rankedCompletedCountFileName), out int completions)) { Completions = completions; }

        api.CheckFile(_rankedRoundsFileName);
        api.CheckFile(_rankedCustomTextFileName);

        if (RankedManagementData == null) return;
        Rounds = RankedManagementData.Rounds;
        CustomText = RankedManagementData.CustomText;
    }

    public override void UpdateAPI(APIDataSaver api)
    {
        Completions = RankedPacePanel.CompletedRunsCount;
        Players = RankedPacePanel.Paces.Count;

        api.UpdateFileContent(_rankedCompletedCountFileName, Completions);
        api.UpdateFileContent(_rankedPlayerCountFileName, Players);

        api.UpdateFileContent(_rankedRoundsFileName, Rounds);
        api.UpdateFileContent(_rankedCustomTextFileName, CustomText);
    }
}
