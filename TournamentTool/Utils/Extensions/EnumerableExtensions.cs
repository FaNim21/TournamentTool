namespace TournamentTool.Utils.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        var batch = new List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count != batchSize) continue;
            
            yield return batch;
            batch = new List<T>(batchSize);
        }
        if (batch.Count > 0)
            yield return batch;
    }
}