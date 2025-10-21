using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services.Logging;

namespace TournamentTool.App.Services;

public class ImageService : IImageService
{
    private ILoggingService Logger { get; }
    private readonly HttpClient _client;

    public ImageService(HttpClient client, ILoggingService logger)
    {
        Logger = logger;
        _client = client;
    }
    
    public async Task<object?> LoadImageFromUrlAsync(string url)
    {
        try
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var imageStream = await response.Content.ReadAsStreamAsync();

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = imageStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            return null;
        }
    }
    public object LoadImageFromStream(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
    public object LoadImageFromResources(string url)
    {
        var uri = new Uri($"pack://application:,,,/Resources/{url}", UriKind.Absolute);
        
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = uri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        
        return bitmap;
    }
}