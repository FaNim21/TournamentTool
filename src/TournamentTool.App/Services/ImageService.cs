using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services.Logging;

namespace TournamentTool.App.Services;

public class ImageService : IImageService
{
    private readonly IHttpClientFactory _clientFactory;
    private ILoggingService Logger { get; }

    
    public ImageService(IHttpClientFactory clientFactory, ILoggingService logger)
    {
        _clientFactory = clientFactory;
        Logger = logger;
    }
    
    public async Task<object?> LoadImageFromUrlAsync(string url)
    {
        try
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(url);
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
        if (imageData == null || imageData.Length == 0) return null!;
        
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