using System.ComponentModel;
using System.Windows.Data;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Diagnostics;

namespace TournamentTool.ViewModels.Controller;

public class PaceManService : BaseViewModel
{
    private ControllerViewModel Controller { get; set; }

    private BackgroundWorker? _paceManWorker;

    private PaceMan? _selectedPaceManPlayer;
    public PaceMan? SelectedPaceManPlayer
    {
        get { return _selectedPaceManPlayer; }
        set
        {
            Controller.ClearSelectedWhitelistPlayer();
            ClearSelectedPaceManPlayer();

            Controller.CurrentChosenPlayer = value;
            Controller.SetPovAfterClickedCanvas();
        }
    }

    private ObservableCollection<PaceMan> _paceManPlayers = [];
    public ObservableCollection<PaceMan> PaceManPlayers
    {
        get => _paceManPlayers;
        set
        {
            _paceManPlayers = value;
            OnPropertyChanged(nameof(PaceManPlayers));
        }
    }

    public ICollectionView? GroupedPaceManPlayers { get; set; }


    //TODO: 0 Zrobic aktualizowanie nazw twitch dla whitelisty jezeli osoba z whitelisty bez nazwy twitch jest w pacemanie
    public PaceManService(ControllerViewModel controllerViewModel)
    {
        Controller = controllerViewModel;
    }

    public override void OnEnable(object? parameter)
    {
        SetupPaceManGrouping();

        if (Controller.Configuration.IsUsingPaceMan)
        {
            _paceManWorker = new() { WorkerSupportsCancellation = true };
            _paceManWorker.DoWork += PaceManUpdate;
            _paceManWorker.RunWorkerAsync();
        }
    }
    public override bool OnDisable()
    {
        _paceManWorker?.CancelAsync();
        _paceManWorker?.Dispose();

        for (int i = 0; i < PaceManPlayers.Count; i++)
            PaceManPlayers[i].Player = null;

        PaceManPlayers.Clear();

        return true;
    }

    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = PaceManPlayers };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PaceMan.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedPaceManPlayers = collectionViewSource.View;
    }

    private async void PaceManUpdate(object? sender, DoWorkEventArgs e)
    {
        while (!_paceManWorker!.CancellationPending)
        {
            try
            {
                await RefreshPaceManAsync();
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(Controller.Configuration.PaceManRefreshRateMiliseconds));
        }
    }
    private async Task RefreshPaceManAsync()
    {
        string result = await Helper.MakeRequestAsString(Consts.PaceManAPI);
        List<PaceMan>? paceMan = JsonSerializer.Deserialize<List<PaceMan>>(result);
        if (paceMan == null) return;

        List<PaceMan> currentPaces = new(PaceManPlayers);

        for (int i = 0; i < paceMan.Count; i++)
        {
            var pace = paceMan[i];
            bool wasPaceFound = false;

            if (!pace.IsLive()) continue;

            for (int j = 0; j < currentPaces.Count; j++)
            {
                var currentPace = currentPaces[j];
                if (pace.Nickname.Equals(currentPace.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    wasPaceFound = true;
                    currentPace.Update(pace);
                    currentPaces.Remove(currentPace);
                    break;
                }
            }

            if (wasPaceFound) continue;

            int n = Controller.Configuration.Players.Count;
            for (int j = 0; j < n; j++)
            {
                var current = Controller.Configuration.Players[j];

                if (current.StreamData.ExistName(pace.User.TwitchName))
                {
                    pace.Player = current;
                    break;
                }
            }

            if (Controller.Configuration.IsUsingWhitelistOnPaceMan && pace.Player == null) continue;

            pace.Initialize(Controller, pace.Splits);
            AddPaceMan(pace);
        }

        for (int i = 0; i < currentPaces.Count; i++)
            RemovePaceMan(currentPaces[i]);

        OnPropertyChanged(nameof(PaceManPlayers));
        RefreshPaceManGroups();
    }

    public void AddPaceMan(PaceMan paceMan)
    {
        Application.Current.Dispatcher.Invoke(() => { PaceManPlayers.Add(paceMan); });

        int n = Controller.Configuration.Players.Count;
        for (int i = 0; i < n; i++)
        {
            var player = Controller.Configuration.Players[i];

            if (player.InGameName!.Equals(paceMan.Nickname))
            {
                player.StreamData.SetName(paceMan.User.TwitchName);
            }
        }
    }
    public void RemovePaceMan(PaceMan paceMan)
    {
        Application.Current.Dispatcher.Invoke(() => { PaceManPlayers.Remove(paceMan); });
    }

    private void RefreshPaceManGroups()
    {
        Application.Current.Dispatcher.Invoke(() => { GroupedPaceManPlayers?.Refresh(); });
    }

    public void ClearSelectedPaceManPlayer()
    {
        _selectedPaceManPlayer = null;
        OnPropertyChanged(nameof(SelectedPaceManPlayer));
    }
}
