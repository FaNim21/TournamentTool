using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TournamentTool.Commands;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.StatusBar;

public abstract class StatusItemViewModel : BaseViewModel
{
    protected abstract string Name { get; }

    protected readonly Dictionary<string, BitmapImage> StateImages = new();

    private BitmapImage? _currentImage;
    public BitmapImage? CurrentImage
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

    
    protected StatusItemViewModel()
    {
        ShowMenuCommand = new RelayCommand<FrameworkElement>(ShowMenu);
        
        InitializeImages();
        InitializeState();
    }

    protected abstract void InitializeImages();
    protected abstract void InitializeState();
    
    protected abstract void BuildContextMenu(ContextMenu menu);

    protected void SetToolTip(string status)
    {
        ToolTip = $"Item: {Name}\n" +
                  $"Status: {status}";
        OnPropertyChanged(nameof(ToolTip));
    }
   
    protected void AddStateImage(string state, string imagePath)
    {
        var image = Helper.LoadImageFromResources(imagePath);
        if (image != null)
        {
            StateImages[state] = image;
        }
    }
    protected virtual void SetState(string state)
    {
        if (StateImages.TryGetValue(state, out var image))
        {
            CurrentImage = image;
        }
    }

    private void ShowMenu(FrameworkElement? element)
    {
        if (element == null) return;

        var contextMenu = new ContextMenu
        {
            PlacementTarget = element,
            Placement = PlacementMode.Top,
            VerticalOffset = -5
        };
        
        if (Application.Current.FindResource("StatusMenuStyle") is Style menuStyle)
        {
            contextMenu.Style = menuStyle;
        }
        
        BuildContextMenu(contextMenu);

        if (contextMenu.Items.Count > 0)
        {
            contextMenu.IsOpen = true;
        }
    }

    protected void SetBadge(int count)
    {
        BadgeVisibility = count > 0;
        BadgeCount = count <= 9 ? count.ToString() : "9+";
    }

    protected Separator GetSeparator()
    {
        var separator = new Separator();
        if (Application.Current.FindResource("MenuSeparatorStyle") is Style sepStyle)
        {
            separator.Style = sepStyle;
        }

        return separator;
    }
}
