using OBSStudioClient.Classes;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MethodTimer;
using TournamentTool.Commands.Controller;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.Controller;
using TournamentTool.Modules.Logging;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Controller;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.OBS;

public enum SceneType
{
    Main,
    Preview
}

public class Scene : BaseViewModel
{
    private readonly IPointOfViewOBSController _povController;
    private readonly Lock _lock = new();
    protected SceneType Type { get; set; }

    public SceneControllerViewmodel SceneController { get; }
    protected ILoggingService Logger { get; }

    private ObservableCollection<PointOfView> _povs = [];
    public ObservableCollection<PointOfView> POVs
    {
        get => _povs; 
        set
        { 
            _povs = value;
            OnPropertyChanged(nameof(POVs));
        }
    }

    public string SceneName { get; set; } = string.Empty;

    private float _canvasWidth;
    public float CanvasWidth
    {
        get => _canvasWidth;
        set
        {
            if (_canvasWidth == value) return;

            _canvasWidth = value;
            OnPropertyChanged(nameof(CanvasWidth));
        }
    }

    private float _canvasHeight;
    public float CanvasHeight
    {
        get => _canvasHeight;
        set
        {
            if (_canvasHeight == value) return;

            _canvasHeight = value;
            OnPropertyChanged(nameof(CanvasHeight));
        }
    }

    private float _fontSizeSceneName;
    public float FontSizeSceneName
    {
        get => _fontSizeSceneName;
        set
        {
            _fontSizeSceneName = value;
            OnPropertyChanged(nameof(FontSizeSceneName));
        }
    }

    public float BaseWidth {  get; set; } 
    public float ProportionsRatio => BaseWidth / CanvasWidth;

    public ICommand ClearPOVCommand { get; set; }
    public ICommand RefreshPOVCommand { get; set; }
    public ICommand ShowInfoWindowCommand { get; set; }


    public Scene(SceneType type, SceneControllerViewmodel sceneController, IPointOfViewOBSController povController, IDialogWindow dialogWindow, ILoggingService logger)
    {
        SceneController = sceneController;
        Logger = logger;
        _povController = povController;
        
        SetSceneType(type);
        
        ClearPOVCommand = new ClearPOVCommand();
        RefreshPOVCommand = new RefreshPOVCommand();
        ShowInfoWindowCommand = new ShowPOVInfoWindowCommand(dialogWindow, this);
    }

    public void Swap(Scene other)
    {
        var povs = other.POVs;
        float baseWidth = other.BaseWidth;
        string sceneName = other.SceneName;

        other.SetSceneName(SceneName);
        other.POVs = POVs;
        other.BaseWidth = BaseWidth;

        SetSceneName(sceneName);
        POVs = povs;
        BaseWidth = baseWidth;
    }
    
    public void OnPOVClick(PointOfView clickedPov)
    {
        SceneController.CurrentChosenPOV?.UnFocus();
        if (SceneController.CurrentChosenPOV is { } && SceneController.CurrentChosenPOV == clickedPov)
        {
            SceneController.CurrentChosenPOV = null;
            return;
        }
        
        PointOfView? previousPOV = SceneController.CurrentChosenPOV;
        SceneController.CurrentChosenPOV = clickedPov;
        
        if (SceneController.Controller.CurrentChosenPlayer == null)
        {
            if (previousPOV is { IsEmpty: true } && SceneController.CurrentChosenPOV!.IsEmpty)
            {
                previousPOV.UnFocus();
            }
            else if (SceneController.CurrentChosenPOV!.Swap(previousPOV))
            {
                SceneController.CurrentChosenPOV = null;
            }
            
            SceneController.CurrentChosenPOV?.Focus();
            return;
        }

        if (!IsPlayerInPov(SceneController.Controller.CurrentChosenPlayer.StreamDisplayInfo))
        {
            clickedPov.SetPOV(SceneController.Controller.CurrentChosenPlayer);
        }

        SceneController.CurrentChosenPOV.UnFocus();
        SceneController.Controller.UnSelectItems(true);
    }

    public void SetStudioMode(bool option)
    {
        if (option)
        {
            CanvasWidth = 210;
            CanvasHeight = 118.12f;
            FontSizeSceneName = 4;
            return;
        }

        CanvasWidth = 426;
        CanvasHeight = 239.625f;
        FontSizeSceneName = 10;
    } 

    public async Task GetCurrentSceneItems(string scene, bool force = false, bool updatePlayersInPov = true)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (scene.Equals(SceneName) && !force) return;

        SetSceneName(scene);
        ClearPovs();

        SceneItem[] sceneItems = await SceneController.OBS.GetSceneItemList(scene);
        List<SceneItem> additionals = [];
        List<(SceneItem, SceneItem?)> povItems = [];

        foreach (var item in sceneItems)
        {
            if (item.IsGroup == true)
            {
                SceneItem[] groupItems = await SceneController.OBS.GetGroupSceneItemList(item.SourceName);
                foreach (var groupItem in groupItems)
                {
                    if (string.IsNullOrEmpty(groupItem.InputKind)) continue;
                    if (CheckForAdditionals(additionals, groupItem)) continue;

                    if (groupItem.InputKind!.Equals("browser_source") &&
                        groupItem.SourceName.StartsWith(SceneController.Tournament.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase))
                    {
                        povItems.Add((groupItem, item));
                    }
                }
            }

            if (string.IsNullOrEmpty(item.InputKind)) continue;
            if (CheckForAdditionals(additionals, item)) continue;

            if (item.InputKind!.Equals("browser_source") &&
                item.SourceName.StartsWith(SceneController.Tournament.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase))
            {
                povItems.Add((item, null));
            }
        }

        if (updatePlayersInPov) SceneController.Tournament.ClearPlayersFromPOVS();

        for (int i = povItems.Count - 1; i >= 0; i--)
        {
            (SceneItem, SceneItem?) current = povItems[i];
            await SetupPovFromSceneItem(additionals, current.Item1, current.Item2);
        }
    }
    private bool CheckForAdditionals(List<SceneItem> additionals, SceneItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.InputKind)) return false;

        if ((item.InputKind.Equals("browser_source") && item.SourceName.StartsWith("head", StringComparison.OrdinalIgnoreCase)) || item.InputKind.StartsWith("text"))
        {
            additionals.Add(item);
            return true;
        }

        return false;
    }
    private async Task SetupPovFromSceneItem(List<SceneItem> additionals, SceneItem item, SceneItem? group = null)
    {
        float positionX = item.SceneItemTransform.PositionX;
        float positionY = item.SceneItemTransform.PositionY;

        float width = item.SceneItemTransform.Width;
        float height = item.SceneItemTransform.Height;

        string groupName = string.Empty;
        if (group != null)
        {
            groupName = group.SourceName;

            positionX *= group.SceneItemTransform.ScaleX;
            positionY *= group.SceneItemTransform.ScaleY;

            positionX += group.SceneItemTransform.PositionX;
            positionY += group.SceneItemTransform.PositionY;

            width *= group.SceneItemTransform.ScaleX;
            height *= group.SceneItemTransform.ScaleY;
        }

        PointOfViewOBSData povData = new(item.SceneItemId, groupName, SceneName, item.SourceName);
        PointOfView pov = new(_povController, povData, Type)
        {
            OriginX = (int)positionX,
            OriginY = (int)positionY,

            OriginWidth = (int)width,
            OriginHeight = (int)height,
        };
        pov.UpdateTransform(ProportionsRatio);

        foreach (var additional in additionals)
        {
            if (additional.SourceName.StartsWith(pov.Data.SceneItemName, StringComparison.CurrentCultureIgnoreCase))
            {
                pov.Data.TextFieldItemName = additional.SourceName;
            }
            else if (additional.SourceName.StartsWith("head" + pov.Data.SceneItemName, StringComparison.OrdinalIgnoreCase))
            {
                pov.Data.HeadItemName = additional.SourceName;
            }
            else if (additional.SourceName.StartsWith("personalbest" + pov.Data.SceneItemName, StringComparison.OrdinalIgnoreCase))
            {
                pov.Data.PersonalBestItemName = additional.SourceName;
            }
        }
        AddPov(pov);
        
        (string? currentName, int volume, StreamType type) = await SceneController.OBS.GetBrowserURLStreamInfo(pov.Data.SceneItemName);
        if (string.IsNullOrEmpty(currentName) || IsPlayerInPov(currentName, type))
        {
            pov.Clear(true);
            return;
        }
        
        PlayerViewModel? player = SceneController.Tournament.GetPlayerByStreamName(currentName, type);
        pov.ChangeVolume(volume);
        
        if (player != null)
        {
            pov.SetPOV(player);
        }
        else
        {
            pov.CustomStreamType = type;
            pov.CustomStreamName = currentName;
            pov.SetCustomPOV();
        }
    }

    public void RefreshItems()
    {
        for (int i = 0; i < POVs.Count; i++)
        {
            POVs[i].UpdateTransform(ProportionsRatio);
        }
    }
    public async Task Refresh()
    {
        await GetCurrentSceneItems(SceneName!, true, false);
    }
    public async Task RefreshPovs() 
    {
        for (int i = 0; i < POVs.Count; i++)
        {
            await POVs[i].Refresh();
        }
    }

    public void AddPov(PointOfView pov)
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            lock (_lock)
            {
                POVs.Add(pov);
            }
        });
    }
    public void RemovePov(PointOfView pov)
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            lock (_lock)
            {
                POVs.Remove(pov);
            }
        }));
    }
    public void ClearPovs()
    {
        Application.Current.Dispatcher.Invoke(POVs.Clear);
    }

    public bool IsPlayerInPov(StreamDisplayInfo streamInfo)
    {
        return IsPlayerInPov(streamInfo.Name, streamInfo.Type);
    }
    public bool IsPlayerInPov(string streamName, StreamType streamType)
    {
        if (string.IsNullOrEmpty(streamName)) return false;

        for (int i = 0; i < POVs.Count; i++)
        {
            var current = POVs[i];
            if (current.StreamDisplayInfo.Name.Equals(streamName, StringComparison.OrdinalIgnoreCase) &&
                current.StreamDisplayInfo.Type == streamType)
                return true;
        }
        return false;
    }

    public void SetSceneName(string scene)
    {
        SceneName = scene;
        OnPropertyChanged(nameof(SceneName));
    }
    private void SetSceneType(SceneType type)
    {
        Type = type;
    }

    public void CalculateProportionsRatio(float baseWidth)
    {
        BaseWidth = baseWidth;
    }

    public void ResizeCanvas()
    {
        if (CanvasWidth == 0 || CanvasHeight == 0) return;

        float calculatedHeight = CanvasWidth / Consts.AspectRatio;
        float calculatedWidth = CanvasHeight * Consts.AspectRatio;

        if (float.IsNaN(calculatedHeight) || float.IsInfinity(calculatedHeight) || float.IsNaN(calculatedWidth) || float.IsInfinity(calculatedWidth)) return;

        if (calculatedHeight > CanvasHeight)
            CanvasWidth = calculatedWidth;
        else
            CanvasHeight = calculatedHeight;

        for (int i = 0; i < POVs.Count; i++)
        {
            var pov = POVs[i];
            pov.UpdateTransform(ProportionsRatio);
        }
    }

    public void Clear()
    {
        SetSceneName(string.Empty);
        ClearPovs();
    }
}
