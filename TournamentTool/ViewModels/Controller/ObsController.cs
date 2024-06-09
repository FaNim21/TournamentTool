using OBSStudioClient;
using OBSStudioClient.Classes;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using OBSStudioClient.Messages;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TwitchLib.Communication.Interfaces;

namespace TournamentTool.ViewModels.Controller;

public class ObsController : BaseViewModel
{
    public ControllerViewModel Controller { get; private set; }

    public ObsClient Client { get; set; }

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

    public string CurrentSceneName { get; set; } = "";

    public float XAxisRatio { get; private set; }
    public float YAxisRatio { get; private set; }

    public float CanvasAspectRatio { get; private set; }

    public ICommand RefreshOBSCommand { get; set; }
    public ICommand AddPovItemToOBSCommand { get; set; }


    public ObsController(ControllerViewModel controller)
    {
        Controller = controller;

        Client = new() { RequestTimeout = 10000 };

        RefreshOBSCommand = new RelayCommand(async () => { await Refresh(); });
        AddPovItemToOBSCommand = new RelayCommand(async () => { await CreateNestedSceneItem("PovSceneLOL"); });
    }

    public async Task Connect(string password, int port)
    {
        if (Controller.Configuration == null) return;

        int timeoutCount = 0;
        int timeoutChecks = 35;

        EventSubscriptions subscription = EventSubscriptions.All | EventSubscriptions.Scenes | EventSubscriptions.SceneItems;
        bool isConnected = await Client.ConnectAsync(true, password, "localhost", port, subscription);
        Client.PropertyChanged += OnPropertyChanged;
        if (isConnected)
        {
            try
            {
                while (Client.ConnectionState != ConnectionState.Connected && timeoutCount <= timeoutChecks)
                {
                    await Task.Delay(100);
                    timeoutCount++;
                }

                await Client.SetCurrentSceneCollection(Controller.Configuration.SceneCollection!);

                Controller.CanvasWidth = 426;
                Controller.CanvasHeight = 240;

                var settings = await Client.GetVideoSettings();

                XAxisRatio = settings.BaseWidth / Controller.CanvasWidth;
                OnPropertyChanged(nameof(XAxisRatio));
                YAxisRatio = settings.BaseHeight / Controller.CanvasHeight;
                OnPropertyChanged(nameof(YAxisRatio));

                CanvasAspectRatio = (float)Controller.CanvasWidth / Controller.CanvasHeight;
                OnPropertyChanged(nameof(CanvasAspectRatio));

                CurrentSceneName = await Client.GetCurrentProgramScene();
                OnPropertyChanged(nameof(CurrentSceneName));
                await GetCurrentSceneitems();

                Client.SceneItemListReindexed += OnSceneItemListReindexed;
                Client.SceneItemCreated += OnSceneItemCreated;
                Client.SceneItemRemoved += OnSceneItemRemoved;
                Client.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                await Disconnect();
                return;
            }
        }
    }

    public async Task Disconnect()
    {
        if (!IsConnectedToWebSocket) return;

        Client.Disconnect();
        Client.Dispose();

        while (Client.ConnectionState != ConnectionState.Disconnected)
            await Task.Delay(100);

        Client.PropertyChanged -= OnPropertyChanged;
        Client.SceneItemListReindexed -= OnSceneItemListReindexed;
        Client.SceneItemCreated -= OnSceneItemCreated;
        Client.SceneItemRemoved -= OnSceneItemRemoved;
        Client.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
    }

    public async Task Refresh()
    {
        Controller.Configuration.ClearFromController();
        await GetCurrentSceneitems();
    }

    public async Task GetCurrentSceneitems()
    {
        if (string.IsNullOrEmpty(CurrentSceneName)) return;

        Application.Current.Dispatcher.Invoke(Controller.ClearPovs);
        await Task.Delay(50);

        SceneItem[] sceneItems = await Client.GetSceneItemList(CurrentSceneName);
        List<SceneItem> additionals = [];

        foreach (var item in sceneItems)
        {
            if (item.IsGroup == true)
            {
                SceneItem[] groupItems = await Client.GetGroupSceneItemList(item.SourceName);
                foreach (var groupItem in groupItems)
                {
                    if (string.IsNullOrEmpty(groupItem.InputKind)) continue;
                    if (CheckForAdditionals(additionals, groupItem)) continue;

                    await SetupPovFromSceneItem(groupItem, item);
                }
            }

            if (string.IsNullOrEmpty(item.InputKind)) continue;
            if (CheckForAdditionals(additionals, item)) continue;

            await SetupPovFromSceneItem(item);
        }

        if (additionals.Count == 0) return;
        foreach (var pov in Controller.POVs)
        {
            foreach (var additional in additionals)
            {
                if (additional.SourceName.StartsWith(pov.SceneItemName!, StringComparison.CurrentCultureIgnoreCase))
                {
                    pov.TextFieldItemName = additional.SourceName;
                    pov.Update();
                    pov.UpdateNameTextField();
                }
                else if (additional.SourceName.StartsWith("head" + pov.SceneItemName!, StringComparison.OrdinalIgnoreCase))
                {
                    pov.HeadItemName = additional.SourceName;
                    pov.Update();
                    pov.SetHead();
                }
                else if (additional.SourceName.StartsWith("personalbest" + pov.SceneItemName!, StringComparison.OrdinalIgnoreCase))
                {
                    pov.PersonalBestItemName = additional.SourceName;
                    pov.Update();
                    pov.UpdatePersonalBestTextField();
                }
            }
        }
    }
    private bool CheckForAdditionals(List<SceneItem> additionals, SceneItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.InputKind)) return false;

        if (item.InputKind.Equals("browser_source") && item.SourceName.StartsWith("head", StringComparison.OrdinalIgnoreCase))
        {
            if (Controller.Configuration.SetPovHeadsInBrowser)
            {
                additionals.Add(item);
                return true;
            }
        }
        else if (item.InputKind.StartsWith("text"))
        {
            if (Controller.Configuration.DisplayedNameType != DisplayedNameType.None || Controller.Configuration.SetPovPBText)
            {
                additionals.Add(item);
                return true;
            }
        }

        return false;
    }
    private async Task SetupPovFromSceneItem(SceneItem item, SceneItem? group = null)
    {
        if (!item.InputKind!.Equals("browser_source") ||
            !item.SourceName.StartsWith(Controller.Configuration.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        float positionX = item.SceneItemTransform.PositionX;
        float positionY = item.SceneItemTransform.PositionY;

        string groupName = string.Empty;
        if (group != null)
        {
            groupName = group.SourceName;
            positionX += group.SceneItemTransform.PositionX;
            positionY += group.SceneItemTransform.PositionY;
        }

        PointOfView pov = new(this, groupName)
        {
            SceneName = CurrentSceneName,
            SceneItemName = item.SourceName,
            ID = item.SceneItemId,

            X = (int)(positionX / XAxisRatio),
            Y = (int)(positionY / YAxisRatio),

            Width = (int)(item.SceneItemTransform.Width / XAxisRatio),
            Height = (int)(item.SceneItemTransform.Height / YAxisRatio),

            Text = item.SourceName
        };

        (string? currentName, float volume) = await GetBrowserURLTwitchName(pov.SceneItemName);
        pov.ChangeVolume(volume);

        if (!string.IsNullOrEmpty(currentName))
        {
            Player? player = Controller.Configuration.GetPlayerByTwitchName(currentName);
            pov.Clear();

            if (player != null && !player.IsUsedInPov)
                pov.SetPOV(player);
        }

        Controller.AddPov(pov);
    }

    public void SetBrowserURL(PointOfView pov)
    {
        if (pov == null) return;
        if (!SetBrowserURL(pov.SceneItemName!, pov.GetURL())) return;

        if (Controller.Configuration.DisplayedNameType == DisplayedNameType.None) return;
        pov.SetHead();
        pov.UpdateNameTextField();
        pov.UpdatePersonalBestTextField();
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
        await Client.CreateSceneItem(CurrentSceneName, sceneName);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ConnectionState")
        {
            bool option = Client!.ConnectionState == ConnectionState.Connected;
            if (!option)
            {
                Application.Current?.Dispatcher.Invoke(delegate { IsConnectedColor = new SolidColorBrush(TwitchStreamData.offlineColor); });
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(delegate { IsConnectedColor = new SolidColorBrush(TwitchStreamData.liveColor); });
            }

            IsConnectedToWebSocket = option;
            OnPropertyChanged(nameof(IsConnectedColor));
        }
    }
    private void OnSceneItemListReindexed(object? sender, SceneItemListReindexedEventArgs e)
    {
        Task.Run(GetCurrentSceneitems);
    }
    public void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs args)
    {
        Task.Run(GetCurrentSceneitems);

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
    public void OnSceneItemRemoved(object? parametr, SceneItemRemovedEventArgs args)
    {
        Task.Run(GetCurrentSceneitems);

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
        CurrentSceneName = e.SceneName;
        OnPropertyChanged(nameof(CurrentSceneName));

        Task.Run(GetCurrentSceneitems);
    }
}
