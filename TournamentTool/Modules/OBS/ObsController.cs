using OBSStudioClient;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using OBSStudioClient.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.OBS;

public class ObsController : BaseViewModel
{
    public ControllerViewModel Controller { get; private set; }

    private ObservableCollection<string> _scenes = [];
    public ObservableCollection<string> Scenes
    {
        get { return _scenes; }
        set
        {
            if (_scenes != value)
            {
                _scenes = value;
                OnPropertyChanged(nameof(Scenes));
            }
        }
    }

    private string _selectedScene = string.Empty;
    public string SelectedScene
    {
        get => _selectedScene;
        set
        {
            if (_selectedScene != value)
            {
                LoadPreviewScene(value);
            }
        }
    }
    
    public ObsClient Client { get; set; }
    private CancellationTokenSource _cancellationTokenSource;
 
    public Brush? IsConnectedColor { get; set; }

    private bool _isConnectedToWebSocket;
    public bool IsConnectedToWebSocket
    {
        get => _isConnectedToWebSocket;
        set
        {
            _isConnectedToWebSocket = value;
            OnPropertyChanged(nameof(IsConnectedToWebSocket));
        }
    }

    public bool StudioMode { get; private set; }
 
    public ICommand RefreshOBSCommand { get; set; }
    public ICommand AddPovItemToOBSCommand { get; set; }
    public ICommand SwitchStudioModeCommand {  get; set; }
    public ICommand StudioModeTransitionCommand { get; set; }

    private bool _startedTransition;


    public ObsController(ControllerViewModel controller)
    {
        Controller = controller;

        _cancellationTokenSource = new();
        Client = new() { RequestTimeout = 10000 };

        RefreshOBSCommand = new RelayCommand(async () => { await Refresh(); });
        AddPovItemToOBSCommand = new RelayCommand(async () => { await CreateNestedSceneItem("PovSceneLOL"); });
        SwitchStudioModeCommand = new RelayCommand(() =>
        {
            if (!IsConnectedToWebSocket) return;

            Client.SetStudioModeEnabled(!StudioMode);
        });
        StudioModeTransitionCommand = new RelayCommand(() =>
        {
            if (!IsConnectedToWebSocket || Controller.MainScene.SceneName!.Equals(Controller.PreviewScene.SceneName)) return;

            Client.TriggerStudioModeTransition();
        });
    }
 
    public override void OnEnable(object? parameter)
    {
        _cancellationTokenSource = new();
        Task.Factory.StartNew(async () =>
        {
            await Connect(Controller.Configuration.Password!, Controller.Configuration.Port);
        }, _cancellationTokenSource.Token);
    }
    public override bool OnDisable()
    {
        Task.Run(Disconnect);
        Controller.MainScene.Clear();
        Controller.PreviewScene.Clear();
        SelectedScene = string.Empty;
        StudioMode = false;
        Scenes.Clear();

        return true;
    }

    public async Task Connect(string password, int port)
    {
        if (Controller.Configuration == null) return;

        EventSubscriptions subscription = EventSubscriptions.All;
        await Client.ConnectAsync(true, password, "localhost", port, subscription);
        Client.PropertyChanged += OnPropertyChanged;
    }

    private async Task OnConnected()
    {
        try
        {
            await Client.SetCurrentSceneCollection(Controller.Configuration.SceneCollection!);

            var settings = await Client.GetVideoSettings();
            Controller.MainScene.CalculateProportionsRatio(settings.BaseWidth);
            Controller.PreviewScene.CalculateProportionsRatio(settings.BaseWidth);

            string mainScene = await Client.GetCurrentProgramScene();
            await Controller.MainScene.GetCurrentSceneItems(mainScene);

            bool studioMode = await Client.GetStudioModeEnabled();
            ChangeStudioMode(studioMode, false);

            Controller.MainScene.RefreshItems();

            Client.StudioModeStateChanged += OnStudioModeStateChanged;
            Client.SceneItemListReindexed += OnSceneItemListReindexed;
            Client.SceneItemCreated += OnSceneItemCreated;
            Client.SceneItemRemoved += OnSceneItemRemoved;
            Client.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
            Client.CurrentPreviewSceneChanged += OnCurrentPreviewSceneChanged;
            Client.SceneTransitionStarted += Client_SceneTransitionStarted;
        }

        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            await Disconnect();
            return;
        }
    }
 
    public async Task Disconnect()
    {
        if (!IsConnectedToWebSocket) return;

        Client.PropertyChanged -= OnPropertyChanged;

        Client.StudioModeStateChanged -= OnStudioModeStateChanged;
        Client.SceneItemListReindexed -= OnSceneItemListReindexed;
        Client.SceneItemCreated -= OnSceneItemCreated;
        Client.SceneItemRemoved -= OnSceneItemRemoved;
        Client.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
        Client.CurrentPreviewSceneChanged -= OnCurrentPreviewSceneChanged;
        Client.SceneTransitionStarted -= Client_SceneTransitionStarted;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        Client.Disconnect();
        Client.Dispose();

        while (Client.ConnectionState != ConnectionState.Disconnected)
            await Task.Delay(100);
        Client = new();
    }

    public async Task Refresh()
    {
        if(!IsConnectedToWebSocket)
        {
            await Connect(Controller.Configuration.Password!, Controller.Configuration.Port);
        }

        Controller.Configuration.ClearPlayersFromPOVS();
        await Controller.RefreshScenes();
    }

    private async Task UpdateSceneItems(string scene)
    {
        if(scene.Equals(Controller.MainScene.SceneName))
        {
            await Controller.MainScene.GetCurrentSceneItems(scene, true);
            return;
        }
        await Controller.PreviewScene.GetCurrentSceneItems(scene, true);
    }

    public void SetBrowserURL(PointOfView pov)
    {
        if (pov == null) return;
        if (!SetBrowserURL(pov.SceneItemName!, pov.GetURL())) return;

        if (Controller.Configuration.SetPovHeadsInBrowser) pov.UpdateHead();
        if (Controller.Configuration.DisplayedNameType != DisplayedNameType.None) pov.UpdateNameTextField();
        if (Controller.Configuration.SetPovPBText) pov.UpdatePersonalBestTextField();
    }
    public bool SetBrowserURL(string sceneItemName, string path)
    {
        if (!IsConnectedToWebSocket || string.IsNullOrEmpty(sceneItemName)) return false;

        Dictionary<string, object> input = new() { { "url", path }, };
        Client.SetInputSettings(sceneItemName, input);
        return true;
    }
    public void SetTextField(string sceneItemName, string text)
    {
        if (!IsConnectedToWebSocket || string.IsNullOrEmpty(sceneItemName)) return;

        Dictionary<string, object> input = new() { { "text", text }, };
        Client.SetInputSettings(sceneItemName, input);
    }

    private async Task LoadScenesForStudioMode()
    {
        Application.Current.Dispatcher.Invoke(Scenes.Clear);

        var loadedScenes = await Client.GetSceneList();

        for (int i = 0; i < loadedScenes.Scenes.Length; i++)
        {
            var current = loadedScenes.Scenes[i];
            Application.Current.Dispatcher.Invoke(() => { Scenes.Add(current.SceneName); });
        }
    }

    public async Task<(string, float)> GetBrowserURLTwitchName(string sceneItemName)
    {
        if (!IsConnectedToWebSocket) return (string.Empty, 0);

        var setting = await Client.GetInputSettings(sceneItemName);
        Dictionary<string, object> input = setting.InputSettings;

        string patternPlayerName = @"channel=([^&]+)";
        string patternVolume = @"volume=(\d+(\.\d+)?)";

        input.TryGetValue("url", out var address);
        if (address == null) return (string.Empty, 0);

        string url = address!.ToString()!;
        if (string.IsNullOrEmpty(url)) return (string.Empty, 0);

        string name = string.Empty;
        float volume = 0;

        Match matchName = Regex.Match(address!.ToString()!, patternPlayerName);
        Match matchVolume = Regex.Match(address!.ToString()!, patternVolume);

        if (matchName.Success)
        {
            name = matchName.Groups[1].Value;
        }
        if (matchVolume.Success)
        {
            if (float.TryParse(matchVolume.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float volumeValue))
            {
                volume = volumeValue;
            }
        }

        return (name, volume);
    }

    private async Task CreateNewSceneItem(string sceneName, string newSceneItemName, string inputKind)
    {
        if (Client.ConnectionState == ConnectionState.Disconnected) return;

        try
        {

            var existingItem = await Client.GetSceneItemId(sceneName, newSceneItemName);
            if (existingItem > 0)
            {
                DialogBox.Show($"Scene item '{newSceneItemName}' already exists in scene '{sceneName}'.");
                return;
            }
        }
        catch { }

        Input input = new(inputKind, newSceneItemName, inputKind);
        await Client.CreateInput(sceneName, newSceneItemName, inputKind, input);
    }

    private async Task CreateNestedSceneItem(string sceneName)
    {
        if (Client.ConnectionState == ConnectionState.Disconnected) return;

        await Client.CreateScene(sceneName);

        await CreateNewSceneItem(sceneName, "item1", "browser_source");
        await CreateNewSceneItem(sceneName, "item2", "browser_source");

        int sceneItem1 = await Client.GetSceneItemId(sceneName, "item1");
        int sceneItem2 = await Client.GetSceneItemId(sceneName, "item2");

        //Client.setscene
        //await Client.CreateSceneItem(CurrentSceneName, sceneName);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ConnectionState")
        {
            bool option = Client!.ConnectionState == ConnectionState.Connected;

            Application.Current?.Dispatcher.Invoke(async delegate
            {
                if (!option)
                {
                    IsConnectedColor = new SolidColorBrush(TwitchStreamData.offlineColor);
                    Controller.Configuration.ClearPlayersFromPOVS();
                    Controller.ClearScenes();
                }
                else
                {
                    IsConnectedColor = new SolidColorBrush(TwitchStreamData.liveColor);
                    await OnConnected();
                }
            });

            IsConnectedToWebSocket = option;
            OnPropertyChanged(nameof(IsConnectedColor));
        }
    }
    private void OnSceneItemListReindexed(object? sender, SceneItemListReindexedEventArgs e)
    {
        //TODO: 0 jezeli przeniose item w scenie to nie resetuje povy graczy z racji tej ich kropki zeby nie duplikowac ich po povach
        Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });
    }
    public void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs e)
    {
        Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        //TODO: 0 Zrobic wylapywanie dodawania wszystkich elementow tez typu head i text od povow
        /*if (!args.SourceName.StartsWith(Controller.Configuration.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase)) return;

        SceneItemTransform transform = Client!.GetSceneItemTransform(args.SceneName, args.SceneItemId).Result;
        PointOfView pov = new(this)
        {
            SceneName = args.SceneName,
            SceneItemName = args.SourceName,
            ID = args.SceneItemId,
            X = (int)(transform.PositionX / XAxisRatio),
            Y = (int)(transform.PositionY / YAxisRatio),
            Width = (int)(transform.Width / XAxisRatio),
            Height = (int)(transform.Height / YAxisRatio),
            Text = args.SourceName
        };
        Controller.AddPov(pov);*/
    }
    public void OnSceneItemRemoved(object? parametr, SceneItemRemovedEventArgs e)
    {
        Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        //TODO: 0 Zrobic wylapywanie usuwania wszystkich elementow tez typu head i text od povow
        /*if (!args.SourceName.StartsWith(Controller.Configuration.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase)) return;

        PointOfView? pov = null;
        for (int i = 0; i < Controller.POVs.Count; i++)
        {
            if (Controller.POVs[i].SceneItemName == args.SourceName)
            {
                pov = Controller.POVs[i];
                break;
            }
        }

        if (pov == null) return;
        Controller.RemovePov(pov);*/
    }

    private void OnCurrentProgramSceneChanged(object? sender, SceneNameEventArgs e)
    {
        if (StudioMode) return;

        string scene = e.SceneName;
        bool isDuplicate = scene.Equals(Controller.MainScene.SceneName);
        Trace.WriteLine($"Program scene: {scene}, duplicate: {isDuplicate}");
        if (isDuplicate) return;

        Task.Run(async () => {await Controller.MainScene.GetCurrentSceneItems(scene); });
    }
    private void OnCurrentPreviewSceneChanged(object? sender, SceneNameEventArgs e)
    {
        if (_startedTransition)
        {
            _startedTransition = false;
            return;
        }

        string sceneName = e.SceneName;
        if (sceneName.Equals(Controller.PreviewScene.SceneName)) return;

        Trace.WriteLine("Loading Preview scene: " + sceneName);
        Task.Run(async () => { await Controller.PreviewScene.GetCurrentSceneItems(sceneName); });
        LoadPreviewScene(sceneName, true);
    }

    private void OnStudioModeStateChanged(object? sender, StudioModeStateChangedEventArgs e)
    {
        ChangeStudioMode(e.StudioModeEnabled);
    }
    private void ChangeStudioMode(bool option, bool refresh = true)
    {
        Trace.WriteLine($"StudioMode: {option}");

        StudioMode = option;
        OnPropertyChanged(nameof(StudioMode));

        Controller.MainScene.SetStudioMode(option);
        Controller.PreviewScene.SetStudioMode(option);

        if (refresh)
        {
            Controller.MainScene.RefreshItems();
            Controller.PreviewScene.RefreshItems();
        }

        if (StudioMode)
        {
            Application.Current?.Dispatcher.Invoke(async () => {
                await LoadScenesForStudioMode();
                await Controller.PreviewScene.GetCurrentSceneItems(Controller.MainScene.SceneName, true);
                LoadPreviewScene(Controller.MainScene.SceneName);
            });
        }
    }

    private void LoadPreviewScene(string sceneName, bool isFromApi = false)
    {
        if(!IsConnectedToWebSocket || string.IsNullOrEmpty(sceneName)) return;
        Trace.WriteLine($"loading preview - {sceneName}");

        UpdateSelectedScene(sceneName);

        if (isFromApi || SelectedScene.Equals(Controller.PreviewScene.SceneName)) return;
        Client.SetCurrentPreviewScene(SelectedScene);
    }

    private void UpdateSelectedScene(string sceneName)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < Scenes.Count; i++)
            {
                var current = Scenes[i];
                if(current.Equals(sceneName))
                {
                    _selectedScene = current;
                    OnPropertyChanged(nameof(SelectedScene));
                }
            }
        });
    }

    private void Client_SceneTransitionStarted(object? sender, TransitionNameEventArgs e)
    {
        if (!StudioMode || Controller.MainScene.SceneName!.Equals(Controller.PreviewScene.SceneName)) return;
        _startedTransition = true;

        Trace.WriteLine("Started Transition");
        Task.Run(async () =>
        {
            string previewScene = Controller.PreviewScene.SceneName;
            string mainScene = Controller.MainScene.SceneName;

            await Controller.MainScene.GetCurrentSceneItems(previewScene);
            await Controller.PreviewScene.GetCurrentSceneItems(mainScene);

            UpdateSelectedScene(mainScene);
        });
    }
}
