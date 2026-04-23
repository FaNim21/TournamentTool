using System.ComponentModel;
using System.Windows.Input;
using TournamentTool.Core.Common.OBS;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;

namespace TournamentTool.ViewModels.Obs.Items;

public class PointOfViewViewModel<T> : BrowserItemViewModel<T> where T : PointOfView
{
    protected PointOfViewViewModel(T sceneItem, IDispatcherService dispatcher, ILoggingService logger) 
        : base(sceneItem, dispatcher, logger) { }
}

public class PointOfViewViewModel : PointOfViewViewModel<PointOfView>, ISwappable<PointOfViewViewModel>
{
    public SceneType Type => _sceneItem.Type;

    public override int ZIndex { get; protected set; } = 0;

    public string DisplayedPlayer
    {
        get => _sceneItem.DisplayedPlayer;
        set => _sceneItem.DisplayedPlayer = value;
    }

    public StreamDisplayInfo StreamDisplayInfo => _sceneItem.StreamDisplayInfo;

    public bool IsEmpty => _sceneItem.IsEmpty;

    public IPlayer? player
    {
        get => _sceneItem.Player;
        set => _sceneItem.Player = value;
    }

    public string CustomStreamName
    {
        get => _sceneItem.CustomStreamName;
        set => _sceneItem.CustomStreamName = value;
    }
    public StreamType CustomStreamType
    {
        get => _sceneItem.CustomStreamType;
        set => _sceneItem.CustomStreamType = value;
    }

    public string CurrentCustomStreamName
    {
        get => _sceneItem.CurrentCustomStreamName;
        set => _sceneItem.CurrentCustomStreamName = value;
    }
    public StreamType CurrentCustomStreamType
    {
        get => _sceneItem.CurrentCustomStreamType;
        set => _sceneItem.CurrentCustomStreamType = value;
    } 

    public bool IsMuted
    {
        get => _sceneItem.IsMuted;
        set => _sceneItem.IsMuted = value;
    }
    public int Volume
    {
        get => _sceneItem.Volume;
        set => _sceneItem.Volume = value;
    }

    public string TextVolume => $"{NewVolume}%";
    public int NewVolume
    {
        get => _sceneItem.NewVolume;
        set
        {
            _sceneItem.NewVolume = value;
            OnPropertyChanged(nameof(TextVolume));
        }
    }

    public ICommand ApplyVolumeCommand { get; set; }
    public ICommand RefreshCommand { get; set; }


    public PointOfViewViewModel(PointOfView sceneItem, IDispatcherService dispatcher, ILoggingService logger) 
        : base(sceneItem, dispatcher, logger)
    {
        DefaultColor = Consts.UnFocusedPovColor;

        ApplyVolumeCommand = new RelayCommand(async () => await ApplyVolume());
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());

        _sceneItem.PropertyChanged += OnModelPropertyChanged;
    }
    public override void OnDestroy()
    {
        _sceneItem.PropertyChanged -= OnModelPropertyChanged;
        
        if (player == null) return;
        
        player.IsUsedInPov = false;
        player.IsUsedInPreview = false;
        //TODO: ?
        // StreamDisplayInfo = new StreamDisplayInfo(string.Empty, StreamType.twitch);
    }

    public override void Initialize(bool inEditMode, bool isDisplaed) => base.Initialize(inEditMode, isDisplaed);

    public async Task SetCustomPOVAsync(IPovUsage? other = null) => await _sceneItem.SetCustomPOVAsync(other);
    public async Task SetPOVAsync(IPlayer? povInfo) => await _sceneItem.SetPOVAsync(povInfo);

    public async Task<bool> SwapAsync(PointOfViewViewModel? pov)
    {
        if (pov == null) return false;
        
        return await _sceneItem.SwapAsync((PointOfView)pov.SceneItem);
    }

    public async Task ApplyVolume()
    {
        Volume = NewVolume;
        IsMuted = Volume == 0;

        await UpdateAsync();
    }
    
    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)) return;
        
        OnPropertyChanged(e.PropertyName);
    }
}