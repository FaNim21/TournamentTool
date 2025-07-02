using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules;
using TournamentTool.Utils;
using TournamentTool.Utils.Parsers;

namespace TournamentTool.ViewModels;

public class UpdatesViewModel : SelectableViewModel
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

    public bool Downloading { get; private set; }

    private bool _patchNotesGrid { get; set; }
    public bool PatchNotesGrid
    {
        get => _patchNotesGrid;
        set
        {
            _patchNotesGrid = value;
            OnPropertyChanged(nameof(PatchNotesGrid));
        }
    }

    private string _headerText { get; set; } = "Loading name...";
    public string HeaderText
    {
        get => _headerText;
        set
        {
            _headerText = value;
            OnPropertyChanged(nameof(HeaderText));
        }
    }

    private string _bodyText { get; set; } = "Loading body...";
    public string BodyText
    {
        get => _bodyText;
        set
        {
            _bodyText = value;
            OnPropertyChanged(nameof(BodyText));
            ParsedMarkdown = MarkdownParser.ParseMarkdown(_bodyText);
        }
    }

    private string _progressText { get; set; } = "0%";
    public string ProgressText
    {
        get => _progressText;
        set
        {
            _progressText = value;
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    private double _progressValue { get; set; }
    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            _progressValue = value;
            OnPropertyChanged(nameof(ProgressValue));
        }
    }

    private TextBlock? _parsedMarkdown;
    public TextBlock? ParsedMarkdown
    {
        get => _parsedMarkdown;
        private set
        {
            _parsedMarkdown = value;
            OnPropertyChanged(nameof(ParsedMarkdown));
        }
    }

    public ICommand DownloadCommand { get; set; }

    private readonly string downloadPath;
    private const string apiUrl = "https://api.github.com/repos/FaNim21/TournamentTool/releases";
    private const string REPO = "TournamentTool";

    private string? downloadUrl;
    private long size;

    public bool startedDownloading;
    private bool canDownloadUpdate;


    public UpdatesViewModel(ICoordinator coordinator) : base(coordinator)
    {
        downloadPath = Path.Combine(Consts.AppdataPath, "Downloaded.zip");

        DownloadCommand = new RelayCommand(() =>
        {
            if (!canDownloadUpdate) return;

            Task.Factory.StartNew(Download);
        });

        Task.Run(Setup);
    }

    public override bool CanEnable() => true;
    public override void OnEnable(object? parameter)
    {
        PatchNotesGrid = true;
    }
    public override bool OnDisable()
    {
        return true;
    }

    private async Task Setup()
    {
        if (!Coordinator.AvailableNewUpdate)
        {
            HeaderText = "NO NEW UPDATES";
            BodyText = "";
            return;
        }

        canDownloadUpdate = true;
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", REPO);
            var releasesResponse = await httpClient.GetAsync(apiUrl);

            if (!releasesResponse.IsSuccessStatusCode)
            {
                Trace.WriteLine("Error while searching for update: " + releasesResponse.StatusCode);
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
                    HeaderText = releases[0].Name;
                    BodyText = releases[0].Body;
                });
            }
            else
            {
                Trace.WriteLine("No releases found in the repository.");
                canDownloadUpdate = false;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error: {ex.Message} {ex.StackTrace}");
            canDownloadUpdate = false;
        }
        finally
        {
            startedDownloading = false;
        }
    }

    private async Task Download()
    {
        if (!canDownloadUpdate) return;

        Application.Current?.Dispatcher.Invoke(delegate
        {
            startedDownloading = true;
            PatchNotesGrid = false;
            Downloading = true;
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

            Trace.WriteLine("Download completed successfully.");

            ReplaceExecutable();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error: {ex.Message} {ex.StackTrace}");
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

            var executableFileName = "TournamentTool.exe";
            var extractPath = Path.Combine(Consts.AppdataPath, "TournamentTool.exe");

            using (var archive = ZipFile.OpenRead(downloadPath))
            {
                var entry = archive.GetEntry(executableFileName);

                if (entry != null)
                {
                    entry.ExtractToFile(extractPath, true);
                }
                else
                {
                    Trace.WriteLine($"{executableFileName} not found in the zip file.");
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
            Trace.WriteLine($"Error replacing executable: {ex.Message}\n{ex.StackTrace}");
        }
        Downloading = false;
    }

    public void UpdateProgress(long bytesDownloaded, long totalBytes)
    {
        if (totalBytes <= 0) return;
        Application.Current?.Dispatcher.Invoke(delegate
        {
            ProgressText = $"{Math.Round((double)bytesDownloaded / totalBytes, 2) * 100}%";
            ProgressValue = (double)bytesDownloaded / totalBytes * 100;
        });
    }
}
