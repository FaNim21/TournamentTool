using System.Windows.Input;

namespace TournamentTool.ViewModels.Commands;

public abstract class BaseCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;
    private readonly Func<object?, bool>? _canExecute;

    protected BaseCommand(Func<object?, bool>? canExecute = null)
    {
        _canExecute = canExecute;
    }

    public virtual bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public abstract void Execute(object? parameter);

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
