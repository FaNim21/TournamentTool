using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
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

public class TwitchStreamData : BaseViewModel
{
    public string ID { get; set; } = string.Empty;
    public string BroadcasterID { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ViewerCount { get; set; }
    public DateTime StartedAt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;

    public Brush? StatusLabelColor { get; set; }
    public string Status { get; set; } = "offline";


    public void Update(TwitchStreamData data)
    {
        ID = data.ID;
        BroadcasterID = data.BroadcasterID;
        UserName = data.UserName;
        GameName = data.GameName;
        Title = data.Title;
        ViewerCount = data.ViewerCount;
        StartedAt = data.StartedAt;
        Language = data.Language;
        ThumbnailUrl = data.ThumbnailUrl;

        Status = data.Status;
        if (Status.Equals("live", StringComparison.OrdinalIgnoreCase))
            Application.Current?.Dispatcher.Invoke(delegate { StatusLabelColor = new SolidColorBrush(Color.FromRgb(51, 204, 51)); });
        else
            Application.Current?.Dispatcher.Invoke(delegate { StatusLabelColor = new SolidColorBrush(Color.FromRgb(125, 38, 37)); });

        Update();
    }
    private void Update()
    {
        OnPropertyChanged(nameof(ID));
        OnPropertyChanged(nameof(BroadcasterID));
        OnPropertyChanged(nameof(UserLogin));
        OnPropertyChanged(nameof(UserName));
        OnPropertyChanged(nameof(GameName));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ViewerCount));
        OnPropertyChanged(nameof(StartedAt));
        OnPropertyChanged(nameof(Language));
        OnPropertyChanged(nameof(ThumbnailUrl));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusLabelColor));
    }

    public void Clear()
    {
        ID = string.Empty;
        BroadcasterID = string.Empty;
        UserName = string.Empty;
        GameName = string.Empty;
        Title = string.Empty;
        ViewerCount = 0;
        StartedAt = DateTime.MinValue;
        Language = string.Empty;
        ThumbnailUrl = string.Empty;
        Status = "offline";
        Application.Current?.Dispatcher.Invoke(delegate { StatusLabelColor = new SolidColorBrush(Color.FromRgb(125, 38, 37)); });
        Update();
    }
}

public class Player : BaseViewModel, ITwitchPovInformation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? UUID { get; set; }

    [JsonIgnore]
    public TwitchStreamData TwitchStreamData { get; set; } = new();

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

    //TODO: 0 Zamiast jednego TwitchName to zrobic TwitchStreamData w tablicy i uwzgledniac alty
    //i ewentualnie mozna zostawic twitch name albo dla podobienstwa zrobic wyciaganie aktywnego twitch streamdata
    //dla nazwy streama
    private string? _twitchName = "";
    public string? TwitchName
    {
        get => _twitchName;
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            _twitchName = value;
            TwitchStreamData.UserLogin = value;
            OnPropertyChanged(nameof(TwitchName));
        }
    }

    private string? _twitchNameAlt = "";
    public string? TwitchNameAlt
    {
        get => _twitchNameAlt;
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            _twitchNameAlt = value;
            //TwitchStreamData.UserLogin = value;
            OnPropertyChanged(nameof(TwitchNameAlt));
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

    public string GetDisplayName()
    {
        return Name!;
    }
    public string GetTwitchName()
    {
        return TwitchName!;
    }
}
