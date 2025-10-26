using System.Text.Json;
using System.Text.Json.Serialization;
using NuGet.Versioning;
using TournamentTool.Core.Utils;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.External;

public struct Release
{
    [JsonPropertyName("tag_name")] public string Version { get; set; }
}

public interface IUpdateCheckerService
{
    Task<bool> CheckForUpdates(string? version = null);
    bool AvailableUpdate { get; }
}

public class UpdateCheckerService : IUpdateCheckerService
{
    private readonly IHttpClientFactory _clientFactory;
    private ILoggingService Logger { get; }
    
    private const string OWNER = "FaNim21";
    private const string REPO = "TournamentTool";
    private const string URL = "https://api.github.com/repos/FaNim21/TournamentTool/releases";

    public bool AvailableUpdate { get; private set; }


    public UpdateCheckerService(IHttpClientFactory clientFactory, ILoggingService logger)
    {
        _clientFactory = clientFactory;
        Logger = logger;
    }
    
    public async Task<bool> CheckForUpdates(string? version = null)
    {
        if (string.IsNullOrEmpty(version))
        {
            version = Consts.Version[1..];
        }

        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(URL);
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<Release[]>(responseBody);
            var latestRelease = releases![0];

            if (!string.IsNullOrEmpty(latestRelease.Version) && !IsUpToDate(latestRelease.Version, version))
            {
                Logger.Warning($"Found new update - {latestRelease.Version}");
                AvailableUpdate = true;
                return true;
            }
        }
        else
        {
            throw new Exception($"{response.StatusCode}");
        }

        Logger.Log($"You are up to date - {version}");
        AvailableUpdate = false;
        return false;
    }
    private bool IsUpToDate(string latestTag, string currentTag)
    {
        NuGetVersion latest = new(latestTag);
        NuGetVersion current = new(currentTag);

        return latest <= current;
    }
}