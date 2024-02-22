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
            if(value == null)
            {

            }
            _image = value;
            OnPropertyChanged(nameof(Image));
        }
    }
    public byte[]? ImageStream { get; set; }

    public string? Name { get; set; }
    public string? InGameName { get; set; }
    public string? TwitchName { get; set; } = "";
    public string PersonalBest { get; set; }


    public Player(string name = "")
    {
        Name = name;
    }

    public void Update()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(InGameName));
        OnPropertyChanged(nameof(TwitchName));
        OnPropertyChanged(nameof(PersonalBest));
        if (ImageStream != null)
        {
            Image = Helper.LoadImageFromStream(ImageStream);
            return;
        }

        Task.Run(LoadImage);
    }

    public async Task CompleteData()
    {
        try
        {
            string result = await Helper.MakeRequestAsString($"https://sessionserver.mojang.com/session/minecraft/profile/{UUID}");
            ResponseApiName name = JsonSerializer.Deserialize<ResponseApiName>(result);
            InGameName = name.Name;
            Update();
            TwitchName = Name;
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.Message + " - " + ex.StackTrace);
        }
    }

    private async Task LoadImage()
    {
        if (string.IsNullOrEmpty(InGameName) || Image != null) return;

        Image = await RequestHeadImage();
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

    internal void Clear()
    {
        Name = string.Empty;
        TwitchName = string.Empty;
        PersonalBest = string.Empty;
    }
}
