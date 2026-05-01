using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels;

public class POVInformationViewModel : BaseWindowViewModel
{
    private readonly SceneViewModel _sceneViewModel;
    
    public PointOfViewViewModel PointOfViewViewModel { get; }


    public POVInformationViewModel(PointOfViewViewModel pov, SceneViewModel sceneViewModel, IDispatcherService dispatcher) : base(dispatcher)
    {
        _sceneViewModel = sceneViewModel;
        PointOfViewViewModel = pov;
    }

    public override void Dispose()
    {
        bool exists = _sceneViewModel.ExistInItems<PointOfViewViewModel>(p =>
            p.StreamDisplayInfo.Name.Equals(PointOfViewViewModel.CustomStreamName) &&
            p.StreamDisplayInfo.Type.Equals(PointOfViewViewModel.CustomStreamType));
        
        if (exists) return;

        PointOfViewViewModel.SetCustomPOV();
    }
}
