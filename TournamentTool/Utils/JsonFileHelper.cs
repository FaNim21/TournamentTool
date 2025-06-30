using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;

namespace TournamentTool.Utils;

public static class JsonFileHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions PerformanceOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultBufferSize = 4096
    };

    
    /// <summary>
    /// Loads JSON from file asynchronously
    /// </summary>
    public static async Task<T?> LoadAsync<T>(string filePath, JsonSerializerOptions? options = null)
    {
        if (!File.Exists(filePath))
            return default;

        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            return await JsonSerializer.DeserializeAsync<T>(stream, options ?? PerformanceOptions);
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to load JSON from {filePath}", ex);
        }
    }

    /// <summary>
    /// Saves object to JSON file asynchronously
    /// </summary>
    public static async Task SaveAsync<T>(string filePath, T data, JsonSerializerOptions? options = null)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Write to temp file first for safety
        var tempFile = $"{filePath}.tmp";
        
        try
        {
            await using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await JsonSerializer.SerializeAsync(stream, data, options ?? PerformanceOptions);
            }

            // Atomic replace
            File.Move(tempFile, filePath, true);
        }
        catch
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
    }

    /// <summary>
    /// Saves with automatic backup
    /// </summary>
    public static async Task SaveWithBackupAsync<T>(string filePath, T data, int maxBackups = 3)
    {
        if (File.Exists(filePath))
        {
            // Rotate backups
            for (int i = maxBackups - 1; i > 0; i--)
            {
                var oldBackup = $"{filePath}.bak{i}";
                var newBackup = $"{filePath}.bak{i + 1}";
                if (!File.Exists(oldBackup)) continue;
                if (File.Exists(newBackup)) File.Delete(newBackup);
                
                File.Move(oldBackup, newBackup);
            }

            // Create new backup
            File.Copy(filePath, $"{filePath}.bak1", true);
        }

        await SaveAsync(filePath, data);
    }

    /// <summary>
    /// Loads multiple JSON files in parallel
    /// </summary>
    public static async Task<Dictionary<string, T?>> LoadMultipleAsync<T>(params string[] filePaths)
    {
        var tasks = filePaths.Select(async path => new
        {
            Path = path,
            Data = await LoadAsync<T>(path)
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.Path, r => r.Data);
    }

    /// <summary>
    /// Saves multiple objects in parallel
    /// </summary>
    public static async Task SaveMultipleAsync<T>(Dictionary<string, T> dataToSave)
    {
        var tasks = dataToSave.Select(kvp => SaveAsync(kvp.Key, kvp.Value));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Loads large JSON arrays as async enumerable
    /// </summary>
    public static async IAsyncEnumerable<T> LoadArrayStreamAsync<T>(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
                
            yield return element.Deserialize<T>(DefaultOptions)!;
        }
    }

    /// <summary>
    /// Appends item to JSON array file
    /// </summary>
    public static async Task AppendToArrayAsync<T>(string filePath, T item)
    {
        var items = File.Exists(filePath) ? await LoadAsync<List<T>>(filePath) ?? [] : [];
        items.Add(item);
        await SaveAsync(filePath, items);
    }

    /// <summary>
    /// Validates JSON file structure
    /// </summary>
    public static async Task<bool> ValidateJsonAsync(string filePath)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            using var doc = await JsonDocument.ParseAsync(stream);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads with performance tracking
    /// </summary>
    public static async Task<(T? data, TimeSpan loadTime)> LoadWithTimingAsync<T>(string filePath)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var data = await LoadAsync<T>(filePath);
        sw.Stop();
        return (data, sw.Elapsed);
    }
}
