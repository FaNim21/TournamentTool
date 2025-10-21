using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.ViewModels.StatusBar;

public abstract class StatusItemViewModel : BaseViewModel, IContextMenuBuilder
{
    private readonly IMenuService _menuService;
    private IImageService ImageService { get; }
    
    protected abstract string Name { get; }

    protected readonly Dictionary<string, object> StateImages = new();

    private object? _currentImage;
    public object? CurrentImage
    {
        get => _currentImage;
        set
        {
            _currentImage = value;
            OnPropertyChanged(nameof(CurrentImage));
        }
    }

    private bool _badgeVisibility;
    public bool BadgeVisibility
    {
        get => _badgeVisibility;
        set
        {
            if (_badgeVisibility == value) return;
            _badgeVisibility = value;
            OnPropertyChanged(nameof(BadgeVisibility));
        }
    }

    private string? _badgeCount;
    public string? BadgeCount
    {
        get => _badgeCount;
        set
        {
            if (_badgeCount == value) return;
            _badgeCount = value;
            OnPropertyChanged(nameof(BadgeCount));
        }
    }

    private string? _statusText;
    public string? StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }
        
    private bool _showText;
    public bool ShowText
    {
        get => _showText;
        set
        {
            _showText = value;
            OnPropertyChanged(nameof(ShowText));
        }
    }
    
    public string ToolTip { get; private set; } = string.Empty;

    public ICommand ShowMenuCommand { get; }

    
    protected StatusItemViewModel(IDispatcherService dispatcher, IImageService imageService, IMenuService menuService) : base(dispatcher)
    {
        _menuService = menuService;
        ImageService = imageService;
        
        ShowMenuCommand = new RelayCommand<object>(ShowMenu);
        
        InitializeImages();
        InitializeState();
    }

    protected abstract void InitializeImages();
    protected abstract void InitializeState();
    
    public abstract ContextMenuViewModel BuildContextMenu();

    protected void SetToolTip(string status)
    {
        ToolTip = $"Item: {Name}\n" +
                  $"Status: {status}";
        OnPropertyChanged(nameof(ToolTip));
    }
   
    protected void AddStateImage(string state, string imagePath)
    {
        var image = ImageService.LoadImageFromResources(imagePath);
        if (image == null) return;
        StateImages[state] = image;
    }
    protected void SetState(string state)
    {
        if (!StateImages.TryGetValue(state, out var image)) return;
        CurrentImage = image;
    }

    private void ShowMenu(object? element)
    {
        if (element == null) return;

        var contextMenu = BuildContextMenu();
        _menuService.ShowContextMenu(contextMenu, element);
    }

    protected void SetBadge(int count)
    {
        BadgeVisibility = count > 0;
        BadgeCount = count <= 9 ? count.ToString() : "9+";
    }
}
