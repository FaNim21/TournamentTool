using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO.Compression;
using System.Diagnostics;
using TournamentTool.Utils;

namespace MultiOpener.Windows;

public partial class UpdateDownloadWindow : Window
{
    private struct Releases
    {
        [JsonPropertyName("tag_name")] public string Version { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("body")] public string Body { get; set; }
        [JsonPropertyName("published_at")] public DateTime PublishDate { get; set; }
        [JsonPropertyName("assets")] public Assets[] Assets { get; set; }
    }
    private struct Assets
    {
        [JsonPropertyName("browser_download_url")] public string DownloadURL { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
    }


    private readonly string downloadPath;
    private const string apiUrl = "https://api.github.com/repos/FaNim21/MultiOpener/releases";
    private const string REPO = "MultiOpener";

    private string? downloadUrl;
    private long size;

    private bool startedDownloading;
    private bool canDownloadUpdate;


    public UpdateDownloadWindow()
    {
        InitializeComponent();

        downloadPath = Path.Combine(Consts.AppdataPath, "Downloaded.zip");

        Task.Run(Setup);
    }

    private async Task Setup()
    {
        canDownloadUpdate = true;
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", REPO);
            var releasesResponse = await httpClient.GetAsync(apiUrl);

            if (!releasesResponse.IsSuccessStatusCode)
            {
                //StartViewModel.Log("Error while searching for update: " + releasesResponse.StatusCode);
                canDownloadUpdate = false;
                return;
            }

            string responseBody = await releasesResponse.Content.ReadAsStringAsync();
            Releases[] releases = JsonSerializer.Deserialize<Releases[]>(responseBody)!;

            if (releases.Length > 0)
            {
                downloadUrl = releases[0].Assets[0].DownloadURL;
                size = releases[0].Assets[0].Size;

                Application.Current?.Dispatcher.Invoke(delegate
                {
                    HeaderText.Text = releases[0].Name;
                    BodyText.Text = releases[0].Body;
                });
            }
            else
            {
                //StartViewModel.Log("No releases found in the repository.", Entities.ConsoleLineOption.Error);
                canDownloadUpdate = false;
            }
        }
        catch (Exception ex)
        {
            //StartViewModel.Log($"Error: {ex.Message} {ex.StackTrace}", Entities.ConsoleLineOption.Error);
            canDownloadUpdate = false;
        }
        finally
        {
            startedDownloading = false;
        }
    }

    public void StartDownload(object sender, RoutedEventArgs e)
    {
        if (!canDownloadUpdate) return;

        Task.Factory.StartNew(Download);
    }
    private async Task Download()
    {
        Application.Current?.Dispatcher.Invoke(delegate
        {
            startedDownloading = true;
            GridPatchNotes.Visibility = Visibility.Hidden;
            GridProgressBar.Visibility = Visibility.Visible;
        });

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", REPO);
            using (var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = File.Create(downloadPath))
            {
                var readBytes = 0L;
                var buffer = new byte[8192];
                var bytesRead = 0;

                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    readBytes += bytesRead;

                    UpdateProgress(readBytes, size);
                }
            }

            //StartViewModel.Log("Download completed successfully.");

            ReplaceExecutable();
        }
        catch (Exception ex)
        {
            //StartViewModel.Log($"Error: {ex.Message} {ex.StackTrace}", Entities.ConsoleLineOption.Error);
        }
        finally
        {
            startedDownloading = false;
        }
    }

    private void ReplaceExecutable()
    {
        try
        {
            string currentExecutablePath = Environment.ProcessPath!;

            var executableFileName = "MultiOpener.exe";
            var extractPath = Path.Combine(Consts.AppdataPath, "MultiOpener.exe");

            using (var archive = ZipFile.OpenRead(downloadPath))
            {
                var entry = archive.GetEntry(executableFileName);

                if (entry != null)
                {
                    entry.ExtractToFile(extractPath, true);
                }
                else
                {
                    //StartViewModel.Log($"{executableFileName} not found in the zip file.");
                    return;
                }
            }

            Application.Current?.Dispatcher.Invoke(delegate
            {
                Application.Current?.Shutdown();
            });
            File.Delete(downloadPath);

            var replaceCommand = $"cmd /c timeout /t 3 & copy /Y \"{extractPath}\" \"{currentExecutablePath}\" & del \"{extractPath}\" & start \"\" \"{currentExecutablePath}\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {replaceCommand}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            //StartViewModel.Log($"Error replacing executable: {ex.Message}\n{ex.StackTrace}", Entities.ConsoleLineOption.Error);
        }
    }

    public void UpdateProgress(long bytesDownloaded, long totalBytes)
    {
        if (totalBytes <= 0) return;
        Application.Current?.Dispatcher.Invoke(delegate
        {
            ProgressText.Text = $"{Math.Round((double)bytesDownloaded / totalBytes, 2) * 100}%";
            ProgressBar.Value = (double)bytesDownloaded / totalBytes * 100;
        });
    }

    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (startedDownloading) return;

        Close();
    }
    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
