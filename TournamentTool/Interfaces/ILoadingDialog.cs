using System.Windows;

namespace TournamentTool.Interfaces;

public interface ILoadingDialog
{
    void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading);
}