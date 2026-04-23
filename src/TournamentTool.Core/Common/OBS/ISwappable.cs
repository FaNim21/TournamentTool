namespace TournamentTool.Core.Common.OBS;

public interface ISwappable<in T> : ISwappable
{
    Task<bool> SwapAsync(T item);
    
    async Task<bool> ISwappable.SwapAsync(object item)
    {
        if (item is T t) return await SwapAsync(t);
        return false;
    }
}

public interface ISwappable
{
    Task<bool> SwapAsync(object item);
}