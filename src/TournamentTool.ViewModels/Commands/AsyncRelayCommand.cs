namespace TournamentTool.ViewModels.Commands;

public enum AsyncCommandConcurrencyMode
{
    Allow,
    Ignore,
    CancelPrevious
}

public class AsyncRelayCommand<T> : BaseCommand
{
    private CancellationTokenSource? _cts;
    
    private readonly Func<T?, CancellationToken, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Func<Exception, Task>? _errorHandler;

    public AsyncCommandConcurrencyMode ConcurrencyMode { get; }

    public Exception? ThrownException { get; private set; }
    public Task? ExecutionTask { get; private set; }
    
    public bool IsRunning { get; private set; }

    public AsyncRelayCommand(
        Func<T?, CancellationToken, Task> execute,
        Func<T?, bool>? canExecute = null,
        Func<Exception, Task>? errorHandler = null,
        AsyncCommandConcurrencyMode concurrencyMode = AsyncCommandConcurrencyMode.Ignore)
        : base(_ => true)
    {
        _execute = execute;
        _canExecute = canExecute;
        _errorHandler = errorHandler;
        ConcurrencyMode = concurrencyMode;
    }

    public override bool CanExecute(object? parameter)
    {
        if (_canExecute != null)
        {
            if (!_canExecute((T?)parameter))
            {
                return false;
            }
        }

        if (IsRunning && ConcurrencyMode == AsyncCommandConcurrencyMode.Ignore) return false;
        return true;
    }

    public override async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        T? value = parameter is T t ? t : default;

        try
        {
            switch (ConcurrencyMode)
            {
                case AsyncCommandConcurrencyMode.CancelPrevious:
                    await _cts?.CancelAsync()!;
                    break;

                case AsyncCommandConcurrencyMode.Ignore:
                    if (IsRunning)
                        return;
                    break;
            }

            _cts = new CancellationTokenSource();

            IsRunning = true;
            ThrownException = null;

            RaiseCanExecuteChanged();

            ExecutionTask = ExecuteInternal(value, _cts.Token);

            await ExecutionTask;
        }
        finally
        {
            IsRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    private async Task ExecuteInternal(T? parameter, CancellationToken token)
    {
        try
        {
            await _execute(parameter, token);
        }
        catch (OperationCanceledException) { /**/ }
        catch (Exception ex)
        {
            ThrownException = ex;
            
            if (_errorHandler == null) throw;
            await _errorHandler(ex);
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }
}

public class AsyncRelayCommand : AsyncRelayCommand<object?>
{
    public AsyncRelayCommand(
        Func<CancellationToken, Task> execute,
        Func<bool>? canExecute = null,
        Func<Exception, Task>? errorHandler = null,
        AsyncCommandConcurrencyMode concurrencyMode = AsyncCommandConcurrencyMode.Ignore)
        : base(
            (_, ct) => execute(ct),
            canExecute != null ? _ => canExecute() : null,
            errorHandler,
            concurrencyMode) { }
}