using OBSStudioClient.Classes;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands.Controller;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
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
    private readonly Lock _lock = new();
    public SceneType Type { get; protected init; }

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

    private string _mainText { get; set; } = string.Empty;
    public string MainText
    {
        get => _mainText;
        set
        {
            _mainText = value;
            OnPropertyChanged(nameof(MainText));
        }
    }

    public ICommand ClearPOVCommand { get; set; }
    public ICommand RefreshPOVCommand { get; set; }
    public ICommand ShowInfoWindowCommand { get; set; }


    public Scene(SceneControllerViewmodel sceneController, IDialogWindow dialogWindow, ILoggingService logger)
    {
        SceneController = sceneController;
        Logger = logger;
        Type = SceneType.Main;
        
        ClearPOVCommand = new ClearPOVCommand();
        RefreshPOVCommand = new RefreshPOVCommand();
        ShowInfoWindowCommand = new ShowPOVInfoWindowCommand(dialogWindow);

        CanvasWidth = 426;
        CanvasHeight = 239.625f;
        FontSizeSceneName = 10;
    }

    public void OnPOVClick(PointOfView clickedPov)
    {
        SceneController.CurrentChosenPOV?.UnFocus();
        PointOfView? previousPOV = SceneController.CurrentChosenPOV;
        SceneController.CurrentChosenPOV = clickedPov;
        SceneController.CurrentChosenPOV.Focus();

        if (SceneController.Controller.CurrentChosenPlayer == null)
        {
            if (previousPOV == null || previousPOV.Scene.Type != Type) return;

            SceneController.CurrentChosenPOV.Swap(previousPOV);
            SceneController.CurrentChosenPOV = null;
            return;
        }

        clickedPov.SetPOV(SceneController.Controller.CurrentChosenPlayer);

        SceneController.CurrentChosenPOV.UnFocus();
        SceneController.Controller.UnSelectItems(true);
    }

    public void SetStudioMode(bool option)
    {
        string output = option?"<Smaller>" : "<Bigger>";
        Logger.Log($"Resizing scene ([{Type}] - {SceneName}) to {output}");
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

    public virtual async Task GetCurrentSceneItems(string scene, bool force = false, bool updatePlayersInPov = true)
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

        povItems.Reverse();
        for (int i = 0; i < povItems.Count; i++)
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

        PointOfView pov = new(SceneController.OBS, SceneController.Tournament, this, groupName)
        {
            SceneName = SceneName,
            SceneItemName = item.SourceName,
            ID = item.SceneItemId,

            OriginX = (int)positionX,
            OriginY = (int)positionY,

            OriginWidth = (int)width,
            OriginHeight = (int)height,

            Text = item.SourceName
        };
        pov.UpdateTransform(ProportionsRatio);

        (string? currentName, int volume, StreamType type) = await SceneController.OBS.GetBrowserURLStreamInfo(pov.SceneItemName);

        foreach (var additional in additionals)
        {
            if (additional.SourceName.StartsWith(pov.SceneItemName!, StringComparison.CurrentCultureIgnoreCase))
            {
                pov.TextFieldItemName = additional.SourceName;
            }
            else if (additional.SourceName.StartsWith("head" + pov.SceneItemName!, StringComparison.OrdinalIgnoreCase))
            {
                pov.HeadItemName = additional.SourceName;
            }
            else if (additional.SourceName.StartsWith("personalbest" + pov.SceneItemName!, StringComparison.OrdinalIgnoreCase))
            {
                pov.PersonalBestItemName = additional.SourceName;
            }
        }

        // pov.Clear(true);
        pov.ChangeVolume(volume);

        if (!string.IsNullOrEmpty(currentName))
        {
            PlayerViewModel? player = SceneController.Tournament.GetPlayerByStreamName(currentName, type);
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

        AddPov(pov);
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

    public bool IsPlayerInPov(string twitchName)
    {
        if (string.IsNullOrEmpty(twitchName)) return false;

        for (int i = 0; i < POVs.Count; i++)
        {
            var current = POVs[i];
            if (current.StreamDisplayInfo.Name.Equals(twitchName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public void SetSceneName(string scene)
    {
        SceneName = scene;
        OnPropertyChanged(nameof(SceneName));
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
    }

    public void Clear()
    {
        SetSceneName(string.Empty);
        ClearPovs();
    }
}
