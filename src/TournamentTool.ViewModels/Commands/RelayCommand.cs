namespace TournamentTool.ViewModels.Commands;

public class RelayCommand : BaseCommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute;
    }
    public RelayCommand(Action execute, Func<bool> canExecute) : base(_ => canExecute())
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
    public RelayCommand(Action<T> execute, Func<T?, bool> canExecute) : base(p => canExecute((T?)p))
    {
        _execute = execute;
    }
    
    public override bool CanExecute(object? parameter)
    {
        if (!base.CanExecute(parameter)) return false;

        return parameter is T || (parameter == null && default(T) == null);
    }

    public override void Execute(object? parameter)
    {
        if (parameter is T value)
        {
            _execute(value);
        }
        else if (parameter is null && default(T) is null)
        {
            _execute(default!);
        }
    }
}
