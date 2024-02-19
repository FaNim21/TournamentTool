using Newtonsoft.Json.Linq;
using OBSStudioClient;
using OBSStudioClient.Classes;
using OBSStudioClient.Enums;
using OBSStudioClient.Messages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Main;
using TournamentTool.Models;
using TournamentTool.Properties;
using TournamentTool.Utils;
using TournamentTool.Windows;

namespace TournamentTool.ViewModels;

public class MainViewModel : BaseViewModel
{
    public ObsClient? Client { get; set; } = new();
    //public OBSWebsocket client;
    public ObservableCollection<Tournament> Presets { get; set; } = [];


    private Tournament? _currentChosen;
    public Tournament? CurrentChosen
    {
        get => _currentChosen;
        set
        {
            _currentChosen = value;
            if (_currentChosen != null)
            {
                Settings.Default.LastOpenedPresetName = _currentChosen!.Name;
                Settings.Default.Save();
            }

            IsPresetOpened = _currentChosen != null;
            OnPropertyChanged(nameof(CurrentChosen));
        }
    }

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

    private bool _isCurrentPresetSaved;
    public bool IsCurrentPresetSaved
    {
        get => _isCurrentPresetSaved;
        set
        {
            _isCurrentPresetSaved = value;
            OnPropertyChanged(nameof(IsCurrentPresetSaved));
        }
    }

    private bool _isPresetOpened;
    public bool IsPresetOpened
    {
        get => _isPresetOpened;
        set
        {
            _isPresetOpened = value;
            OnPropertyChanged(nameof(IsPresetOpened));
        }
    }

    public ICommand ConnectToOBSCommand { get; set; }
    public ICommand DisconnectOBSCommand { get; set; }

    public ICommand AddNewPresetCommand { get; set; }
    public ICommand SavePresetCommand { get; set; }
    public ICommand OnItemListClickCommand { get; set; }

    public ICommand OpenSceneManagerCommand { get; set; }
    public ICommand OpenPlayerManagerCommand { get; set; }
    public ICommand OpenCommand { get; set; }

    public ICommand ClearCurrentPresetCommand { get; set; }
    public ICommand DuplicateCurrentPresetCommand { get; set; }
    public ICommand RenameItemCommand { get; set; }
    public ICommand RemoveCurrentPresetCommand { get; set; }


    public MainViewModel()
    {
        //TODO: 0 Zrobic zabezpieczenia do tego zeby nie wychodzic bez zapisywania

        if (!Directory.Exists(Consts.PresetsPath))
            Directory.CreateDirectory(Consts.PresetsPath);

        LoadAllPresets();

        ConnectToOBSCommand = new RelayCommand(ConnectToWebSocket);
        DisconnectOBSCommand = new RelayCommand(Disconnect);

        AddNewPresetCommand = new AddNewPresetCommand(this);
        SavePresetCommand = new SavePresetCommand(this);
        OnItemListClickCommand = new OnItemListClickCommand(this);

        ClearCurrentPresetCommand = new ClearPresetCommand(this);
        DuplicateCurrentPresetCommand = new DuplicatePresetCommand(this);
        RenameItemCommand = new RenamePresetCommand(this);
        RemoveCurrentPresetCommand = new RemovePresetCommand(this);

        OpenCommand = new RelayCommand(OpenPresetControlPanel);
        OpenSceneManagerCommand = new RelayCommand(OpenSceneManagerWindow);
        OpenPlayerManagerCommand = new RelayCommand(OpenPlayerManagerWindow);

        LoadCurrentPreset();
    }

    private void LoadCurrentPreset()
    {
        string lastOpened = Settings.Default.LastOpenedPresetName;
        if (string.IsNullOrEmpty(lastOpened)) return;

        for (int i = 0; i < Presets.Count; i++)
        {
            var current = Presets[i];
            if (current.Name.Equals(lastOpened, StringComparison.OrdinalIgnoreCase))
            {
                CurrentChosen = current;
                return;
            }
        }
    }
    private void LoadAllPresets()
    {
        var presets = Directory.GetFiles(Consts.PresetsPath, "*.json", SearchOption.TopDirectoryOnly).AsSpan();
        for (int i = presets.Length - 1; i >= 0; i--)
        {
            string text = File.ReadAllText(presets[i]) ?? string.Empty;
            try
            {
                if (string.IsNullOrEmpty(text)) continue;
                Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
                if (data == null) continue;
                data.MainViewModel = this;
                Presets.Add(data);
            }
            catch { }
        }
    }

    private void OpenPresetControlPanel()
    {
        if (CurrentChosen == null) return;

        ControllerWindow window = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = new ControllerViewModel(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        window.Show();
        Application.Current.MainWindow.Hide();
    }

    private void OpenSceneManagerWindow()
    {
        SceneManagerWindow window = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = new SceneManagerViewModel(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        window.Show();
        Application.Current.MainWindow.Hide();
    }

    private void OpenPlayerManagerWindow()
    {
        PlayerManagerWindow window = new()
        {
            Owner = Application.Current.MainWindow,
            DataContext = new PlayerManagerViewModel(this, CurrentChosen!),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        window.Show();
        Application.Current.MainWindow.Hide();
    }

    public void ConnectToWebSocket()
    {
        Task.Run(ConnectToWebSocketAsync);
    }
    private async Task ConnectToWebSocketAsync()
    {
        if (IsConnectedToWebSocket || Client == null) return;

        /*client = new();
        client.ConnectAsync("ws://localhost:4455", CurrentChosen!.Password);
        client.Disconnected += (x, args) => { MessageBox.Show("Lost connection"); IsConnectedToWebSocket = false; };

        await Task.Delay(2000);
        try
        {
            client.SetCurrentSceneCollection(CurrentChosen.SceneCollection);
            client.SetCurrentProgramScene(CurrentChosen.Scene);

            MessageBox.Show("Connected to obs succesfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
        }*/

        bool isConnected = await Client.ConnectAsync(true, CurrentChosen!.Password, "localhost", CurrentChosen.Port);
        Client.ConnectionClosed += OnConnectionClosed;
        if (isConnected)
        {
            IsConnectedToWebSocket = true;
            try
            {
                while (Client.ConnectionState != ConnectionState.Connected)
                    await Task.Delay(100);

                await Client.SetCurrentSceneCollection(CurrentChosen.SceneCollection);
                await Client.SetCurrentProgramScene(CurrentChosen.Scene);

                MessageBox.Show("Connected to obs succesfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
                Disconnect();
                return;
            }
        }
    }

    public void Disconnect()
    {
        if (Client == null || !IsConnectedToWebSocket) return;

        Client.Disconnect();
        Client.Dispose();

        Task.Run(async () =>
        {
            while (Client.ConnectionState != ConnectionState.Disconnected)
                await Task.Delay(100);

            Client.ConnectionClosed -= OnConnectionClosed;
        });
    }

    /*public void SetBrowserURL(string sceneItemName, string player)
    {
        if (client == null || !client.IsConnected) return;

        Dictionary<string, object> input = new() { { "url", $"https://player.twitch.tv/?channel={player}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume=0" }, };
        JObject jsonSettings = JObject.FromObject(input);

        client.SetInputSettings(sceneItemName, jsonSettings);
    }*/

    public void SetBrowserURL(string sceneItemName, string player)
    {
        if (Client == null || !IsConnectedToWebSocket) return;

        Dictionary<string, object> input = new() { { "url", $"https://player.twitch.tv/?channel={player}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume=0" }, };
        Client.SetInputSettings(sceneItemName, input);
    }

    private async Task SetupSceneItem(string sceneName, string sceneItemName, int x, int y, int width, int height)
    {
        if (Client == null || Client.ConnectionState == ConnectionState.Disconnected) return;

        //await SetupSceneItem("Screen", "POV1", 150, 215, 800, 600);

        Dictionary<string, object> input = new()
        {
            { "url", $"https://player.twitch.tv/?channel=zylenox&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume=0" },
            { "width", width},
            { "height", height},
        };
        await Client.SetInputSettings("POV1", input);

        SceneItemTransform transform = new(0, 0, 1, Bounds.OBS_BOUNDS_NONE, 1, 0, 0, 0, 0, 0, width / 2 + x, height / 2 + y, 0, 1, 1, 0, 0, 0);
        int id = await Client.GetSceneItemId(sceneName, sceneItemName);
        await Client.SetSceneItemTransform(sceneName, id, transform);
    }

    private async Task CreateNewSceneItem(string sceneName, string newSceneItemName, string inputKind)
    {
        if (Client == null || Client.ConnectionState == ConnectionState.Disconnected) return;


        Input input = new(inputKind, newSceneItemName, inputKind);
        await Client.CreateInput(sceneName, newSceneItemName, inputKind, input);
    }

    public bool IsPresetNameUnique(string name)
    {
        for (int i = 0; i < Presets.Count; i++)
        {
            var current = Presets[i];
            if (current.Name!.Equals(name, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    public void AddItem(Tournament item)
    {
        Presets.Add(item);
    }

    public void AddPOV(PointOfView pov)
    {
        if (CurrentChosen == null) return;

        CurrentChosen.POVs.Add(pov);
    }

    public void SetPresetAsNotSaved()
    {
        IsCurrentPresetSaved = false;
    }

    public void OnConnectionClosed(object? parametr, EventArgs args)
    {
        MessageBox.Show("Lost connection");
        IsConnectedToWebSocket = false;
    }
}
