using NuGet.Versioning;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TournamentTool.Utils;

public struct Release
{
    [JsonPropertyName("tag_name")] public string Version { get; set; }
}

public class UpdateChecker
{
    private const string OWNER = "FaNim21";
    private const string REPO = "TournamentTool";

    public async Task<bool> CheckForUpdates(string? version = null)
    {
        if (string.IsNullOrEmpty(version))
            version = Consts.Version[1..];

        const string apiUrl = "https://api.github.com/repos/FaNim21/TournamentTool/releases";
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", REPO);
            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var releases = JsonSerializer.Deserialize<Release[]>(responseBody);
                var latestRelease = releases![0];

                if (!string.IsNullOrEmpty(latestRelease.Version) && !IsUpToDate(latestRelease.Version, version))
                {
                    Trace.WriteLine($"Found new update - {latestRelease.Version}");
                    return true;
                }
            }
            else
            {
                throw new Exception($"{response.StatusCode}");
            }
        }

        Trace.WriteLine($"You are up to date - {version}");
        return false;
    }

    public static bool IsUpToDate(string latestTag, string currentTag)
    {
        NuGetVersion latest = new(latestTag);
        NuGetVersion current = new(currentTag);

        return latest <= current;
    }
}