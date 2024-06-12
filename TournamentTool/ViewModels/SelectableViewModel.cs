namespace TournamentTool.ViewModels;

public class SelectableViewModel : BaseViewModel
{
    protected MainViewModel MainViewModel { get; set; }
    public bool CanBeDestroyed { get; set; } = false;

    public object? parameterForNextSelectable;


    public SelectableViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public void SetParameter(object? parameter)
    {
        parameterForNextSelectable = parameter;
    }
}
