using System.Windows.Input;
using TournamentTool.Commands;

namespace TournamentTool.ViewModels;

public class LoadingWindowViewModel : BaseViewModel
{
    private readonly CancellationTokenSource _cts = new();

    private float _progress;
    public float Progress
    {   
        get => _progress;
        set
        {
            _progress = value;
            ProgressPercentage = (int)(_progress * 100f);
            OnPropertyChanged(nameof(Progress));
        }
    }

    private int _progressPercentage;
    public int ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            _progressPercentage = value;
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    private string _textLog = string.Empty;
    public string TextLog
    {
        get => _textLog;
        set
        {
            _textLog = value;
            OnPropertyChanged(nameof(TextLog));
        }
    }

    public ICommand CancelCommand { get; }

    public event Action? closeWindow;
    
    private readonly IProgress<float> _progressValue;
    private readonly IProgress<string> _progressLog;

    
    public LoadingWindowViewModel(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading)
    {
        _progressValue = new Progress<float>(value => Progress = value);
        _progressLog = new Progress<string>(message => TextLog = message);
        
        CancelCommand = new RelayCommand(Cancel);

        _ = StartLoading(loading);
    }

    private async Task StartLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading)
    {
        try
        {
            await loading(_progressValue, _progressLog, _cts.Token);
        }
        finally
        {
            closeWindow?.Invoke();
        }
    }

    private void Cancel()
    {
        _cts.Cancel();
    }
}