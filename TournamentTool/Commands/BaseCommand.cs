using System.Windows.Input;

namespace TournamentTool.Commands;

public abstract class BaseCommand : ICommand
{
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }

    public bool CanExecute(object? parameter) => true;
    public abstract void Execute(object? parameter);
}
