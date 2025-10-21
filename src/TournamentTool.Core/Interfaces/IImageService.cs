namespace TournamentTool.Core.Interfaces;

public interface IImageService
{
    public object LoadImageFromStream(byte[] imageData);
    public Task<object?> LoadImageFromUrlAsync(string url);
    public object LoadImageFromResources(string url);
}