using TournamentTool.Models;
using TournamentTool.Modules.OBS;

namespace TournamentTool.ViewModels;

public class POVInformationViewModel : BaseViewModel
{
    private readonly Scene _scene;
    
    public PointOfView PointOfView { get; private set; }


    public POVInformationViewModel(PointOfView pov, Scene scene)
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
