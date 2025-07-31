using System.ComponentModel;
using System.Windows.Controls;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.StatusBar;

public class BackgroundServiceStatusViewModel : StatusItemViewModel
{
    private readonly IBackgroundCoordinator _coordinator;
    private readonly IBackgroundServiceRegistry _registry;

    protected override string Name => "Background Service";
    
    private ControllerMode _mode = ControllerMode.None;
    private bool _isWorking = false;
    
    
    public BackgroundServiceStatusViewModel(IBackgroundCoordinator coordinator, IBackgroundServiceRegistry registry)
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
        }
    }
    
    protected override void BuildContextMenu(ContextMenu menu)
    {
        if (!_isWorking && _mode != ControllerMode.None)
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "Retry",
                Command = new RelayCommand(RetryDeactivatedService)
            });
        }
    }

    private void RetryDeactivatedService()
    {
        _coordinator.Initialize(_mode, true);
    }
}