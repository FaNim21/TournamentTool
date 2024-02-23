using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public struct ResponseApiID
{
    [JsonPropertyName("id")]
    public string ID { get; set; }
}
public struct ResponseApiName
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class Player : BaseViewModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? UUID { get; set; }

    [JsonIgnore]
    private BitmapImage? _image;
    [JsonIgnore]
    public BitmapImage? Image
    {
        get => _image;
        set
        {
            if (value == null)
            {

            }
            _image = value;
            OnPropertyChanged(nameof(Image));
        }
    }
    public byte[]? ImageStream { get; set; }

    private string? _name;
    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    private string? _inGameName;
    public string? InGameName
    {
        get => _inGameName;
        set
        {
            _inGameName = value;
            OnPropertyChanged(nameof(InGameName));
        }
    }

    private string? _twitchName = "";
    public string? TwitchName
    {
        get => _twitchName;
        set
        {
            _twitchName = value;
            OnPropertyChanged(nameof(TwitchName));
        }
    }

    private string? _personalBest;
    public string? PersonalBest
    {
        get => _personalBest;
        set
        {
            _personalBest = value;
            OnPropertyChanged(nameof(PersonalBest));
        }
    }


    public Player(string name = "")
    {
        Name = name;
    }

    public void LoadHead()
    {
        if (ImageStream == null) return;
        Image = Helper.LoadImageFromStream(ImageStream);
    }
    public async Task UpdateHeadImage()
    {
        if (string.IsNullOrEmpty(InGameName) || Image != null) return;
        Image = await RequestHeadImage();
    }
    public async Task CompleteData()
    {
        try
        {
            string result = await Helper.MakeRequestAsString($"https://sessionserver.mojang.com/session/minecraft/profile/{UUID}");
            ResponseApiName name = JsonSerializer.Deserialize<ResponseApiName>(result);
            InGameName = name.Name;
            await UpdateHeadImage();
            TwitchName = Name;
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message + " - " + ex.StackTrace);
        }
    }

    private async Task<BitmapImage?> RequestHeadImage()
    {
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{InGameName}");
        if (!response.IsSuccessStatusCode) return null;
        string output = await response.Content.ReadAsStringAsync();
        ResponseApiID apiID = JsonSerializer.Deserialize<ResponseApiID>(output);

        response = await client.GetAsync($"https://api.mineatar.io/face/{apiID.ID}");
        if (!response.IsSuccessStatusCode) return null;
        byte[] stream = await response.Content.ReadAsByteArrayAsync();
        ImageStream = stream;
        return Helper.LoadImageFromStream(stream);
    }

    public void Clear()
    {
        Name = string.Empty;
        TwitchName = string.Empty;
        PersonalBest = string.Empty;
    }
}
