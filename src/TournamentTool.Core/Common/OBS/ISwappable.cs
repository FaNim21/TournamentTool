namespace TournamentTool.Core.Common.OBS;

public interface ISwappable<in T>
{
    bool Swap(T item);
}