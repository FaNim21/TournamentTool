using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class POVInformationViewModel : BaseViewModel
{
    private PointOfView _pointOfView = null!;
    public PointOfView PointOfView
    {
        get => _pointOfView;
        set
        {
            _pointOfView = value;
            OnPropertyChanged(nameof(PointOfView));
        }
    }


    public POVInformationViewModel(PointOfView pov)
    {
        PointOfView = pov;
    }
}
