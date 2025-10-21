using TournamentTool.Core.Interfaces;

namespace TournamentTool.Core.Common;

public class BaseWindowViewModel : BaseViewModel, ICloseRequest
{
    public Action? RequestClose { get; set; }
    
    protected BaseWindowViewModel(IDispatcherService dispatcher) : base(dispatcher) { }
    public override void Dispose()
    {
        RequestClose = null;
        base.Dispose();
    }
}
