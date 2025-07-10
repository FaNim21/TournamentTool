using System.Windows.Media.Imaging;
using TournamentTool.Modules.OBS;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class StatusBarViewModel : BaseViewModel
{
    public ObsController OBS { get; }
    public TournamentViewModel Tournament { get; }
    public IBackgroundCoordinator BackgroundCoordinator { get; }

    private readonly Dictionary<string, BitmapImage> _cachedImages = [];

    private BitmapImage? _bellImage;
    public BitmapImage? BellImage
    {
        get => _bellImage;
        set
        {
            _bellImage = value;
            OnPropertyChanged(nameof(BellImage));
        }
    }
    
    private BitmapImage? _twitchImage;
    public BitmapImage? TwitchImage
    {
        get => _twitchImage;
        set
        {
            _twitchImage = value;
            OnPropertyChanged(nameof(TwitchImage));
        }
    }
    
    private BitmapImage? _obsImage;
    public BitmapImage? ObsImage
    {
        get => _obsImage;
        set
        {
            _obsImage = value;
            OnPropertyChanged(nameof(ObsImage));
        }
    }
    
    private BitmapImage? _backgroundServiceImage;
    public BitmapImage? BackgroundServiceImage
    {
        get => _backgroundServiceImage;
        set
        {
            _backgroundServiceImage = value;
            OnPropertyChanged(nameof(BackgroundServiceImage));
        }
    }
    
    public StatusBarViewModel(TournamentViewModel tournament, ObsController obs, IBackgroundCoordinator backgroundCoordinator)
    {
        Tournament = tournament;
        OBS = obs;
        BackgroundCoordinator = backgroundCoordinator;

        CacheImages();
        Initialize();
    } 
    ~StatusBarViewModel()
    {
        OnDestroy();
    }

    private void CacheImages()
    {
        BellImage = GetImage("StatusBarIcons/bell-off.png")!;
        GetImage("StatusBarIcons/bell-on.png");
        
        TwitchImage = GetImage("StatusBarIcons/twitch-off.png")!;
        GetImage("StatusBarIcons/twitch-wait.png");
        GetImage("StatusBarIcons/twitch-on.png");
        
        ObsImage = GetImage("StatusBarIcons/obs-off.png")!;
        GetImage("StatusBarIcons/obs-wait.png");
        GetImage("StatusBarIcons/obs-on.png");
        
        //TODO: 1 tu brakuje none obrazka
        BackgroundServiceImage = GetImage("StatusBarIcons/ranked-icon.png")!;
        GetImage("StatusBarIcons/paceman-icon.png");
    }
    
    private void Initialize()
    {
        OBS.ConnectionStateChanged += OnConnectionStateChanged;
    }
    private void OnDestroy()
    {
        OBS.ConnectionStateChanged -= OnConnectionStateChanged;
    }
    
    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        switch (e.NewState)
        {
            case OBSConnectionState.Connected: OnOBSConnected(); break;
            case OBSConnectionState.Disconnected: OnOBSDisconnected(); break;
            case OBSConnectionState.Connecting:
            case OBSConnectionState.Disconnecting: break;
        }
    }
    
    private void OnOBSConnected()
    {
        
    }
    private void OnOBSDisconnected()
    {
        
    }

    private BitmapImage? GetImage(string url)
    {
        if (_cachedImages.TryGetValue(url, out var image)) return image;

        var loadedImage = Helper.LoadImageFromResources(url);
        if (loadedImage == null) return null;

        _cachedImages[url] = loadedImage;
        return loadedImage;
    }
}