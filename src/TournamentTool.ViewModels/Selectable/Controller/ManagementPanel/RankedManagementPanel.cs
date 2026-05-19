using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Background;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable.Controller.ManagementPanel;

public class RankedManagementPanel : ManagementPanel, IRankedManagementDataReceiver
{
    private RankedManagementData _data;
    private readonly IBindingEngine _bindingEngine;

    private BindingKey keyCustomText { get; }
    private BindingKey keyRounds { get; }
    private BindingKey keyCompletions { get; }
    private BindingKey keyPlayers { get; }
    
    public string CustomText
    {
        get => _data.CustomText; 
        set
        {
            _data.CustomText = value;
            OnPropertyChanged();
        }
    }
    public int Rounds
    {
        get => _data.Rounds; 
        set
        {
            if (_data.Rounds == value) return;
            
            _data.Rounds = value;
            OnPropertyChanged();
        }
    }
    public int Completions
    {
        get => _data.Completions; 
        set
        {
            if (_data.Completions == value) return;
            
            _data.Completions = value;
            OnPropertyChanged();
        }
    }
    public int Players
    {
        get => _data.Players; 
        set
        {
            if (_data.Players == value) return;
            
            _data.Players = value;
            OnPropertyChanged();
        }
    }
    public long StartTime 
    {
        get => _data.StartTime;
        set
        {
            _data.StartTime = value;  
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

    public ObservableCollection<RankedBestSplitViewModel> BestSplits { get; private set; } = [];

    public ICommand AddRoundCommand { get; set; }
    public ICommand SubtractRoundCommand { get; set; }

    private long _oldStartTime;
    private Action? _debounceUpdateAPI;


    /// <summary>
    /// TODO: 0 Panel powinien byc tylko jako UI, bo w koncu jest to viewmodel, takze trzeba zrobic serwis do managementu? czy cos pod controller panel caly
    ///  bo w zasadzie, side panel to jest BackgroundService, ale to managementu nie ma nic, wiec chyba trzeba cos ogarnac na rozwoj pod pacemana 
    /// </summary>
    public RankedManagementPanel(RankedManagementData managementData, IDispatcherService dispatcher, IBindingEngine bindingEngine) : base(dispatcher)
    {
        _data = managementData;
        _bindingEngine = bindingEngine;

        // _debounceUpdateAPI = ((Action)UpdateAPI).Debounce();

        keyCompletions = BindingKey.CreateRankedManagement("completions");
        keyPlayers = BindingKey.CreateRankedManagement("players");
        keyCustomText = BindingKey.CreateRankedManagement("custom_text");
        keyRounds = BindingKey.CreateRankedManagement("rounds");

        AddRoundCommand = new RelayCommand(() => { Rounds++; });
        SubtractRoundCommand = new RelayCommand(() => { Rounds--; });
    }

    public override void OnEnable(object? parameter) { }
    public override bool OnDisable() => true;

    public override void InitializeAPI()
    {
        //TODO: 0 inicjalizowac binding
        Update();
    }

    public override void UpdateAPI()
    {
        //TODO: 0 Publikowac zmiany do bindingu, ale w properties, tutaj jest samo aktualizowanie do obs'a (w sensie w tej metodzie)
        //TODO: 0 to zamiast do pliku to bedzie podpiete do aktualizacji itemow w obsie, czyli podpiete scene itemy do tych rzeczy z managementu beda aktualizowaly wartosc

        _bindingEngine.Publish(keyCustomText, CustomText);
        _bindingEngine.Publish(keyRounds, Rounds);

        _bindingEngine.Publish(keyCompletions, Completions);
        _bindingEngine.Publish(keyPlayers, Players);
    }

    public void Update()
    {
        if (StartTime != _oldStartTime)
        {
            _oldStartTime = StartTime;
            DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(StartTime).ToLocalTime().DateTime;
            TimeStartedText = date.ToString("dd MMM yyyy hh:mm:ss tt", CultureInfo.CurrentCulture);
        }

        if (_data.BestSplitsDatas.Count == 0 || _data.RefreshUI)
        {
            _data.RefreshUI = false;
            Dispatcher.Invoke(() => { BestSplits.Clear(); });
        }
        for (var i = 0; i < _data.BestSplitsDatas.Count; i++)
        {
            PrivRoomBestSplit bestSplit = _data.BestSplitsDatas[i];
            if (i < BestSplits.Count) continue;
            
            Dispatcher.Invoke(() =>
            {
                var viewModel = new RankedBestSplitViewModel(bestSplit, Dispatcher);
                BestSplits.Add(viewModel);
            });
        }

        for (int i = 0; i < _data.BestSplitsDatas.Count; i++)
        {
            PrivRoomBestSplit bestSplit = _data.BestSplitsDatas[i];
            for (int j = 0; j < bestSplit.Datas.Count; j++)
            {
                PrivRoomBestSplitData data = bestSplit.Datas[j];
                if (j < BestSplits[i].Splits.Count) continue;
                
                Dispatcher.Invoke(() =>
                {
                    RankedBestSplitDataViewModel dataViewModel = new RankedBestSplitDataViewModel(data, Dispatcher);
                    BestSplits[i].Splits.Add(dataViewModel);
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