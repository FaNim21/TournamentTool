﻿using OBSStudioClient.Classes;
using System.Collections.ObjectModel;
using System.Windows;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.OBS;

public class Scene : BaseViewModel
{
    public ControllerViewModel Controller;

    public ObservableCollection<PointOfView> POVs { get; set; } = [];

    public string? SceneName { get; set; }

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

    private float _fontSizeDisplayedName;
    public float FontSizeDisplayedName
    {
        get => _fontSizeDisplayedName;
        set
        {
            _fontSizeDisplayedName = value;
            OnPropertyChanged(nameof(FontSizeDisplayedName));
        }
    }

    public float BaseWidth {  get; set; } 
    public float ProportionsRatio { get => BaseWidth / CanvasWidth; }

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


    public Scene(ControllerViewModel controllerViewModel)
    {
        Controller = controllerViewModel;

        CanvasWidth = 426;
        CanvasHeight = 239.625f;
        FontSizeSceneName = 10;
        FontSizeDisplayedName = 13;
    }

    public void OnPOVClick(PointOfView clickedPov)
    {
        Controller.CurrentChosenPOV?.UnFocus();
        PointOfView? previousPOV = Controller.CurrentChosenPOV;
        Controller.CurrentChosenPOV = clickedPov;
        Controller.CurrentChosenPOV.Focus();

        if (Controller.CurrentChosenPlayer == null)
        {
            if (previousPOV == null) return;

            Controller.CurrentChosenPOV.Swap(previousPOV);
            Controller.CurrentChosenPOV = null;
            return;
        }

        clickedPov.SetPOV(Controller.CurrentChosenPlayer);

        Controller.CurrentChosenPOV.UnFocus();
        Controller.UnSelectItems(true);
    }

    public void SetStudioMode(bool option)
    {
        if (option)
        {
            CanvasWidth = 200;
            CanvasHeight = 112.5f;
            FontSizeSceneName = 4;
            FontSizeDisplayedName = 5;
            return;
        }

        CanvasWidth = 426;
        CanvasHeight = 239.625f;
        FontSizeSceneName = 10;
        FontSizeDisplayedName = 13;
    }

    public virtual async Task GetCurrentSceneItems(string scene, bool force = false)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (scene.Equals(SceneName) && !force) return;

        SetSceneName(scene);

        Application.Current.Dispatcher.Invoke(ClearPovs);
        await Task.Delay(50);

        SceneItem[] sceneItems = await Controller.OBS.Client.GetSceneItemList(scene);
        List<SceneItem> additionals = [];
        List<(SceneItem, SceneItem?)> povs = [];

        foreach (var item in sceneItems)
        {
            if (item.IsGroup == true)
            {
                SceneItem[] groupItems = await Controller.OBS.Client.GetGroupSceneItemList(item.SourceName);
                foreach (var groupItem in groupItems)
                {
                    if (string.IsNullOrEmpty(groupItem.InputKind)) continue;
                    if (CheckForAdditionals(additionals, groupItem)) continue;

                    if (groupItem.InputKind!.Equals("browser_source") &&
                        item.SourceName.StartsWith(Controller.Configuration.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase))
                    {
                        povs.Add((groupItem, item));
                    }
                }
            }

            if (string.IsNullOrEmpty(item.InputKind)) continue;
            if (CheckForAdditionals(additionals, item)) continue;

            if (item.InputKind!.Equals("browser_source") &&
                item.SourceName.StartsWith(Controller.Configuration.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase))
            {
                povs.Add((item, null));
            }
        }

        for (int i = 0; i < povs.Count; i++)
        {
            (SceneItem, SceneItem?) current = povs[i];
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

        string groupName = string.Empty;
        if (group != null)
        {
            groupName = group.SourceName;
            positionX += group.SceneItemTransform.PositionX;
            positionY += group.SceneItemTransform.PositionY;
        }

        PointOfView pov = new(Controller.OBS, this, groupName)
        {
            SceneName = SceneName,
            SceneItemName = item.SourceName,
            ID = item.SceneItemId,

            OriginX = (int)positionX,
            OriginY = (int)positionY,

            OriginWidth = (int)item.SceneItemTransform.Width,
            OriginHeight = (int)item.SceneItemTransform.Height,

            Text = item.SourceName
        };
        pov.UpdateTransform(ProportionsRatio);

        (string? currentName, float volume) = await Controller.OBS.GetBrowserURLTwitchName(pov.SceneItemName);
        pov.ChangeVolume(volume);

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
        pov.Clear();

        if (!string.IsNullOrEmpty(currentName))
        {
            Player? player = Controller.Configuration.GetPlayerByTwitchName(currentName);

            if (player != null && !player.IsUsedInPov)
                pov.SetPOV(player);
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
        await GetCurrentSceneItems(SceneName!, true);
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
        Application.Current.Dispatcher.Invoke(delegate { POVs.Add(pov); });
        OnPropertyChanged(nameof(POVs));
    }
    public void RemovePov(PointOfView pov)
    {
        Application.Current.Dispatcher.Invoke(delegate { POVs.Remove(pov); });
        OnPropertyChanged(nameof(POVs));
    }
    public void ClearPovs()
    {
        POVs.Clear();
    }

    public bool IsPlayerInPov(string twitchName)
    {
        if (string.IsNullOrEmpty(twitchName)) return false;

        for (int i = 0; i < POVs.Count; i++)
        {
            var current = POVs[i];
            if (current.TwitchName.Equals(twitchName, StringComparison.OrdinalIgnoreCase))
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
        Application.Current.Dispatcher.Invoke(POVs.Clear);
    }
}
