using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Obs;
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
        bool exists = _scene.ExistInItems<PointOfView>(p => p.StreamDisplayInfo.Name.Equals(PointOfView.CustomStreamName) 
                                                            && p.StreamDisplayInfo.Type.Equals(PointOfView.CustomStreamType));
        if (exists) return;
        
        Task.Run(async () => await PointOfView.SetCustomPOVAsync());
    }
}
