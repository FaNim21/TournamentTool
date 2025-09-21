using TournamentTool.Interfaces;

namespace TournamentTool.ViewModels.Settings;

public class GeneralTabViewModel : BaseViewModel, ISettingsTab
{
    private readonly Models.Settings _settings;

    public bool IsChosen { get; private set; }
    public string Name { get; }

    
    public GeneralTabViewModel(Models.Settings settings)
    {
        Name = "general";
        _settings = settings;
    }
    public void OnOpen()
    {
        IsChosen = true;
    }
    public void OnClose()
    {
        IsChosen = true;
    }
}