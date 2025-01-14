using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Modules.SidePanels;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.ManagementPanels;

public class RankedManagementPanel : ManagementPanel
{
    private ControllerViewModel Controller { get; set; }
    private RankedPacePanel RankedPacePanel { get; set; }


    private string _customText = string.Empty;
    public string CustomText
    {
        get => _customText; 
        set
        {
            _customText = value;
            OnPropertyChanged(nameof(CustomText));
        }
    }

    private int _rounds;
    public int Rounds
    {
        get => _rounds; 
        set
        {
            _rounds = value;
            OnPropertyChanged(nameof(Rounds));
        }
    }

    public ICommand AddRoundCommand { get; set; }
    public ICommand SubstractRoundCommand { get; set; }


    public RankedManagementPanel(ControllerViewModel controller, RankedPacePanel rankedPacePanel)
    {
        Controller = controller;
        RankedPacePanel = rankedPacePanel;

        AddRoundCommand = new RelayCommand(() => { Rounds++; });
        SubstractRoundCommand = new RelayCommand(() => { Rounds--; });
    }

}
