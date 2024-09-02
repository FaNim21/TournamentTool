using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class SelectableViewModel : BaseViewModel
{
    protected MainViewModel MainViewModel { get; set; }

    public object? parameterForNextSelectable;


    public SelectableViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public virtual bool CanEnable(Tournament tournament)
    {
        return true;
    }

    public void SetParameter(object? parameter)
    {
        parameterForNextSelectable = parameter;
    }
}
