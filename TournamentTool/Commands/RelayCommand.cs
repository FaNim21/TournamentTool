namespace TournamentTool.Commands;

public class RelayCommand : BaseCommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute;
    }

    public override void Execute(object? parameter)
    {
        _execute();
    }
}

public class RelayCommand<T> : BaseCommand
{
    private readonly Action<T> _execute;

    public RelayCommand(Action<T> execute)
    {
        _execute = execute;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;

        _execute((T)parameter);
    }
}
