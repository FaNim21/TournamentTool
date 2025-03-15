using System.Windows;

namespace TournamentTool.Interfaces;

public interface ILoadingDialog
{
    void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading);
    
    void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading, bool keepBackground);
}