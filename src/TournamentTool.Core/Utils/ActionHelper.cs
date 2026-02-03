namespace TournamentTool.Core.Utils;

public static class ActionHelper
{
    public static Action Debounce(this Action action, int milliseconds = 300)
    {
        CancellationTokenSource? lastCToken = null;

        return () =>
        {
            lastCToken?.Cancel();
            try { 
                lastCToken?.Dispose(); 
            }
            catch { /* ignored */ }

            var tokenSrc = lastCToken = new CancellationTokenSource();

            Task.Delay(milliseconds).ContinueWith(_ => { action(); }, tokenSrc.Token);
        };
    }   
}