using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Parsers;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities.Markdown;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.External;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Modals;

namespace TournamentTool.ViewModels.Selectable;

public class UpdatesViewModel : SelectableViewModel
{
    private readonly IWindowService _windowService;
    private readonly IUpdateCheckerService _updateChecker;
    public ILoggingService Logger { get; }

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
            
            Elements = MarkdownParser.Parse(_bodyText);
            OnPropertyChanged(nameof(Elements));
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
    
    private bool _canDownloadUpdate;
    public bool CanDownloadUpdate
    {
        get => _canDownloadUpdate;
        set
        {
            _canDownloadUpdate = value;
            OnPropertyChanged(nameof(CanDownloadUpdate));
        }
    }

    public List<MarkdownElement> Elements { get; private set; } = [];

    public ICommand DownloadCommand { get; set; }
    public ICommand OpenVersionSite { get; set; }

    private readonly string downloadPath;
    private const string apiUrl = "https://api.github.com/repos/FaNim21/TournamentTool/releases";
    private const string REPO = "TournamentTool";

    private string? downloadUrl;
    private long size;

    public bool startedDownloading;


    public UpdatesViewModel(ILoggingService logger, IDispatcherService dispatcher, IWindowService windowService, 
        IUpdateCheckerService updateChecker, IDialogService dialogService) : base(dispatcher)
    {
        _windowService = windowService;
        _updateChecker = updateChecker;
        Logger = logger;
        
        downloadPath = Path.Combine(Consts.AppdataPath, "Downloaded.zip");

        DownloadCommand = new RelayCommand(() => { Task.Factory.StartNew(Download); });
        OpenVersionSite = new RelayCommand(() =>
        {
            if (dialogService.Show($"Do you want to open TournamentTool site to check for new updates or patch notes?",
                    $"Opening Github Release site For TournamentTool", MessageBoxButton.YesNo,
                    MessageBoxImage.Information) != MessageBoxResult.Yes) return;
        
            Helper.StartProcess("https://github.com/FaNim21/TournamentTool/releases");
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
        if (!_updateChecker.AvailableUpdate)
        {
            HeaderText = "NO NEW UPDATES";
            BodyText = "";
            return;
        }

        CanDownloadUpdate = true;
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", REPO);
            var releasesResponse = await httpClient.GetAsync(apiUrl);

            if (!releasesResponse.IsSuccessStatusCode)
            {
                Logger.Error("Error while searching for update: " + releasesResponse.StatusCode);
                CanDownloadUpdate = false;
                return;
            }

            string responseBody = await releasesResponse.Content.ReadAsStringAsync();
            Releases[] releases = JsonSerializer.Deserialize<Releases[]>(responseBody)!;

            if (releases.Length > 0)
            {
                downloadUrl = releases[0].Assets[0].DownloadURL;
                size = releases[0].Assets[0].Size;

                await Dispatcher.InvokeAsync(delegate
                {
                    HeaderText = releases[0].Name;
                    BodyText = releases[0].Body;
                });
            }
            else
            {
                Logger.Log("No releases found in the repository.");
                CanDownloadUpdate = false;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            CanDownloadUpdate = false;
        }
        finally
        {
            startedDownloading = false;
        }
    }

    private async Task Download()
    {
        if (!CanDownloadUpdate) return;

        await Dispatcher.InvokeAsync(delegate
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

            Logger.Log("Download completed successfully.");

            ReplaceExecutable();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
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
                    Logger.Log($"{executableFileName} not found in the zip file.");
                    return;
                }
            }

            _windowService.CloseApplication();
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
            Logger.Error($"Error replacing executable: {ex}");
        }
        Downloading = false;
    }

    public void UpdateProgress(long bytesDownloaded, long totalBytes)
    {
        if (totalBytes <= 0) return;
        Dispatcher.Invoke(delegate
        {
            ProgressText = $"{Math.Round((double)bytesDownloaded / totalBytes, 2) * 100}%";
            ProgressValue = (double)bytesDownloaded / totalBytes * 100;
        });
    }
}
