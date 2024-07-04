namespace TournamentTool.ViewModels.Settings;

public class SettingsViewModel : SelectableViewModel
{


    public SettingsViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
    }

    public override void OnEnable(object? parameter)
    {

    }

    public override bool OnDisable()
    {
        return true;
    }
}
