using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels;

public class POVInformationViewModel : BaseWindowViewModel
{
    private readonly Scene _scene;
    
    public PointOfView PointOfView { get; private set; }


    public POVInformationViewModel(PointOfView pov, Scene scene, IDispatcherService dispatcher) : base(dispatcher)
    {
        _scene = scene;
        PointOfView = pov;
    }

    public override void Dispose()
    {
        if (_scene.IsPlayerInPov(PointOfView.CustomStreamName, PointOfView.CustomStreamType)) return;
        PointOfView.SetCustomPOV();
    }
}
