using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.Logging;

public class ConsoleWindowViewModel : BaseWindowViewModel
{
    public ConsoleViewModel Console { get; init; }

    public ConsoleWindowViewModel(ConsoleViewModel console, IDispatcherService dispatcher) : base(dispatcher)
    {
        Console = console;
    }
}