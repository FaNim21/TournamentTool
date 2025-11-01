using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.ViewModels.StatusBar;

public class BackgroundServiceStatusViewModel : StatusItemViewModel
{
    private readonly IBackgroundCoordinator _coordinator;
    private readonly IBackgroundServiceRegistry _registry;

    protected override string Name => "Background Service";
    
    private ControllerMode _mode = ControllerMode.None;
    private bool _isWorking = false;
    
    
    public BackgroundServiceStatusViewModel(IBackgroundCoordinator coordinator, IBackgroundServiceRegistry registry, IDispatcherService dispatcher, 
        IImageService imageService, IMenuService menuService) : base(dispatcher, imageService, menuService)
    {
        _coordinator = coordinator;
        _registry = registry;
        
        _registry.ServiceChanged += RegistryOnServiceChanged;
    }
    public override void Dispose()
    {
        _registry.ServiceChanged -= RegistryOnServiceChanged;
    }

    protected override void InitializeImages()
    {
        AddStateImage("none", "none-icon.png");
        AddStateImage("ranked", "StatusBarIcons/ranked-icon.png");
        AddStateImage("paceman", "StatusBarIcons/paceman-icon.png");
    }
    protected override void InitializeState()
    {
        SetState("none");
        SetToolTip("No service active");
    }
    
    private void RegistryOnServiceChanged(object? sender, ServiceRegistryEventArgs e)
    {
        _mode = e.Mode;
        _isWorking = e.IsWorking;

        SetState(_mode.ToString().ToLower());
        if (_mode == ControllerMode.None)
        {
            SetToolTip("No background service active");
        }
        else
        {
            string state = _isWorking ? "activated" : "deactivated";
            SetToolTip($"{_mode} mode {state}");
            
            if (!_isWorking)
            {
                SetState("none");
            }
        }
    }
    
    public override ContextMenuViewModel BuildContextMenu()
    {
        var menu = new ContextMenuViewModel(Dispatcher);

        if (!_isWorking && _mode != ControllerMode.None)
        {
            menu.AddItem("Retry", new RelayCommand(RetryDeactivatedService));
        }
        
        return menu;
    }

    private void RetryDeactivatedService()
    {
        _coordinator.Initialize(_mode, true);
    }
}