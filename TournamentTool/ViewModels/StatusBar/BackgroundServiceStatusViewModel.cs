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

    protected override string Name => "Background Service";
    // public override string ToolTip => GetToolTip();
    
    
    public BackgroundServiceStatusViewModel(IBackgroundCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public override void Dispose()
    {
    }

    protected override void InitializeImages()
    {
        // AddStateImage("none", "StatusBarIcons/none-icon.png"); //brakuje ikonki na none
        AddStateImage("ranked", "StatusBarIcons/ranked-icon.png");
        AddStateImage("paceman", "StatusBarIcons/paceman-icon.png");
        
        
    }

    protected override void InitializeState()
    {
        SetState("paceman");
        /*
        var state = _tournament.ControllerMode switch
        {
            ControllerMode.None => "none",
            ControllerMode.Ranked => "ranked",
            ControllerMode.Paceman => "paceman",
            _ => "none"
        };

        SetState(state);
        OnPropertyChanged(nameof(ToolTip));
        */
    }

    protected override void BuildContextMenu(ContextMenu menu)
    {
        
    }

    private string GetToolTip()
    {
        return "elo";
        /*
        return _tournament.ControllerMode switch
        {
            ControllerMode.None => "No background service active",
            ControllerMode.Ranked => "Ranked mode active",
            ControllerMode.Paceman => "Paceman mode active",
            _ => "Unknown mode"
        };
    */
    }
}