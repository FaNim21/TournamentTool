/*using MultiOpener.ViewModels;
using NuGet.Versioning;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MultiOpener.Utils;

public class Release
{
    public string tag_name { get; set; }
}

public class UpdateChecker
{
    private const string OWNER = "FaNim21";
    private const string REPO = "MultiOpener";

    public async Task<bool> CheckForUpdates(string? version = null)
    {
        if (string.IsNullOrEmpty(version))
            version = Consts.Version[1..];

        const string apiUrl = "https://api.github.com/repos/FaNim21/MultiOpener/releases";
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", REPO);
            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var releases = JsonSerializer.Deserialize<Release[]>(responseBody);
                var latestRelease = releases![0];

                if (latestRelease != null && !IsUpToDate(latestRelease.tag_name, version))
                {
                    StartViewModel.Log($"Found new update - {latestRelease.tag_name}", Entities.ConsoleLineOption.Warning);
                    return true;
                }
            }
            else
            {
                throw new System.Exception($"{response.StatusCode}");
            }
        }

        StartViewModel.Log($"You are up to date - {version}");
        return false;
    }

    public static bool IsUpToDate(string latestTag, string currentTag)
    {
        NuGetVersion latest = new(latestTag);
        NuGetVersion current = new(currentTag);

        return latest <= current;
    }
}*/