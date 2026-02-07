using TournamentTool.Core.Interfaces;

namespace TournamentTool.Core.Common;

public class BaseWindowViewModel : BaseViewModel, ICloseRequest
{
    public Guid Guid { get; }
    public Action? RequestClose { get; set; }

    protected BaseWindowViewModel(IDispatcherService dispatcher) : base(dispatcher)
    {
        Guid = Guid.NewGuid();
    }
    public override void Dispose()
    {
        RequestClose = null;
        base.Dispose();
    }

    public void Close()
    {
        Dispatcher.Invoke(() =>
        {
            RequestClose?.Invoke();
        });
    }
}
