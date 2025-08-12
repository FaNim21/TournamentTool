using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class POVInformationViewModel : BaseViewModel
{
    public PointOfView PointOfView { get; private set; }

    //TODO: 9 jakies rzeczy typu rozdzielczosc pova, skala, itp itd


    public POVInformationViewModel(PointOfView pov)
    {
        PointOfView = pov;
    }

    public override void Dispose()
    {
        PointOfView.SetCustomPOV();
        // zatwierdzanie zmian odnosnie custom'owego pov'a
    }
}
