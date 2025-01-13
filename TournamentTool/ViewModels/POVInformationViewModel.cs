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

    //TODO: 9 jakies rzeczy typu rozdzielczosc pova, skala, itp itd


    public POVInformationViewModel(PointOfView pov)
    {
        PointOfView = pov;
    }
}
