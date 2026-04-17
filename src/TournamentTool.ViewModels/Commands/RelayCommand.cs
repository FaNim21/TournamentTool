namespace TournamentTool.ViewModels.Commands;

public class RelayCommand : BaseCommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null) 
        : base(_ => canExecute?.Invoke() ?? true)
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

    public RelayCommand(Action<T> execute, Func<T?, bool>? canExecute = null) 
        : base(p => canExecute?.Invoke((T?)p) ?? true)
    {
        _execute = execute;
    }

    public override bool CanExecute(object? parameter)
    {
        if (!base.CanExecute(parameter)) return false;

        return parameter is T or null;
    }

    public override void Execute(object? parameter)
    {
        T value = parameter is T t ? t : default!;
        _execute(value);
    }
}