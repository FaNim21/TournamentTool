using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TournamentTool.Components.Controls;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public readonly struct ResponseMojangProfileAPI
{
    [JsonPropertyName("id")]
    public string UUID { get; init; }
    
    [JsonPropertyName("name")]
    public string InGameName { get; init; } 
}

public class StreamData : BaseViewModel
{
    [JsonIgnore] public TwitchStreamData LiveData { get; set; } = new();

    private string _main = string.Empty;
    public string Main
    {
        get => _main;
        set
        {
            _main = value;
            OnPropertyChanged(nameof(Main));
        }
    }

    private string _alt = string.Empty;
    public string Alt
    {
        get => _alt;
        set
        {
            _alt = value;
            OnPropertyChanged(nameof(Alt));
        }
    }

    private StringComparison _ordinalIgnoreCaseComparison = StringComparison.OrdinalIgnoreCase;


    public void SetName(string name)
    {
        if (string.IsNullOrEmpty(name) || ExistName(name)) return;

        if (IsMainEmpty())
        {
            Main = name;
        }
        else if (IsAltEmpty())
        {
            Alt = name;
        }
    }

    public bool ExistName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return Main.Equals(name, _ordinalIgnoreCaseComparison) || Alt.Equals(name, _ordinalIgnoreCaseComparison);
    }

    public string GetCorrectName()
    {
        if (string.IsNullOrEmpty(Main))
            return Alt;
        return Main;
    }

    public bool EqualsNoDialog(StreamData data)
    {
        if (ExistName(data.Main)) return true;
        if (ExistName(data.Alt)) return true;
        return false;
    }
    
    public bool Equals(StreamData data)
    {
        if (ExistName(data.Main))
        {
            DialogBox.Show($"Twitch name \"{data.Main}\" is already assigned to another player", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }
        if (ExistName(data.Alt))
        {
            DialogBox.Show($"Twitch name \"{data.Alt}\" is already assigned to another player", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }

        return false;
    }

    public bool IsMainEmpty()
    {
        return string.IsNullOrEmpty(Main);
    }
    public bool IsAltEmpty()
    {
        return string.IsNullOrEmpty(Alt);
    }
    public bool IsNullOrEmpty()
    {
        return IsMainEmpty() || IsAltEmpty();
    }
    public bool AreBothNullOrEmpty()
    {
        return IsMainEmpty() && IsAltEmpty();
    }

    public void Clear()
    {
        Main = string.Empty;
        Alt = string.Empty;

        LiveData.Clear();
    }
}

public class TwitchStreamData : BaseViewModel
{
    public string ID { get; set; } = string.Empty;
    public string BroadcasterID { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ViewerCount { get; set; }
    public DateTime StartedAt { get; set; }
    public string Language { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;

    public Brush? StatusLabelColor { get; set; }
    public string Status { get; set; } = "offline";
    public static Color liveColor = Color.FromRgb(0, 255, 127);
    public static Color offlineColor = Color.FromRgb(201, 61, 59);
    public static Color normalColor = Color.FromRgb(220, 220, 220);

    public string GameName { get; set; } = string.Empty;
    public bool WasUpdated { get; set; } = false;

    private bool _gameNameVisibility;
    public bool GameNameVisibility
    {
        get => _gameNameVisibility;
        set
        {
            _gameNameVisibility = value;
            OnPropertyChanged(nameof(GameNameVisibility));
        }
    }


    public void Update(TwitchStreamData data)
    {
        WasUpdated = true;

        ID = data.ID;
        BroadcasterID = data.BroadcasterID;
        UserName = data.UserName;
        UserLogin = data.UserLogin;
        GameName = data.GameName;
        Title = data.Title;
        ViewerCount = data.ViewerCount;
        StartedAt = data.StartedAt;
        Language = data.Language;
        ThumbnailUrl = data.ThumbnailUrl;
        Status = data.Status;

        Application.Current?.Dispatcher.Invoke(delegate
        {
            if (Status.Equals("live", StringComparison.OrdinalIgnoreCase))
                StatusLabelColor = new SolidColorBrush(liveColor);
            else
                StatusLabelColor = new SolidColorBrush(offlineColor);
        });

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

    public void Clear(bool isUsingTwitchApi = true)
    {
        BroadcasterID = string.Empty;
        UserName = string.Empty;
        GameName = string.Empty;
        Title = string.Empty;
        ViewerCount = 0;
        StartedAt = DateTime.MinValue;
        Language = string.Empty;
        ThumbnailUrl = string.Empty;
        Status = "offline";
        Application.Current?.Dispatcher.Invoke(delegate
        {
            if (!isUsingTwitchApi)
                StatusLabelColor = new SolidColorBrush(normalColor);
            else
                StatusLabelColor = new SolidColorBrush(offlineColor);
        });
        Update();
    }
}

public class Player : BaseViewModel, IPlayer
{
    public Guid Id { get; init; } = Guid.NewGuid();

    private string _UUID = string.Empty;
    public string UUID
    {
        get => _UUID;
        set
        {
            _UUID = value;
            OnPropertyChanged(nameof(UUID));
            IsUUIDEmpty = string.IsNullOrEmpty(UUID);
        }
    }

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

    public StreamData StreamData { get; set; } = new();

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

    private string? _personalBest = string.Empty;
    public string? PersonalBest
    {
        get => _personalBest;
        set
        {
            _personalBest = value;
            OnPropertyChanged(nameof(PersonalBest));
        }
    }
    
    private string? _teamName = string.Empty;
    public string? TeamName
    {
        get => _teamName;
        set
        {
            _teamName = value;
            OnPropertyChanged(nameof(TeamName));
        }
    }

    [JsonIgnore] private bool _isShowingTeamName;
    [JsonIgnore]
    public bool IsShowingTeamName
    {
        get => _isShowingTeamName;
        set
        {
            _isShowingTeamName = value;
            OnPropertyChanged(nameof(IsShowingTeamName));
        }
    }
    
    [JsonIgnore] private bool _isUsedInPov;
    [JsonIgnore]
    public bool IsUsedInPov
    {
        get => _isUsedInPov;
        set
        {
            _isUsedInPov = value;
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }

    [JsonIgnore] private bool _isUsedInPreview;
    [JsonIgnore]
    public bool IsUsedInPreview
    {
        get => _isUsedInPreview;
        set
        {
            _isUsedInPreview = value;
            OnPropertyChanged(nameof(IsUsedInPreview));
        }
    }
    
    [JsonIgnore] private bool _isUUIDEmpty;
    [JsonIgnore]
    public bool IsUUIDEmpty
    {
        get => _isUUIDEmpty;
        set
        {
            _isUUIDEmpty = value;
            OnPropertyChanged(nameof(IsUUIDEmpty));
        }
    }

    private const StringComparison _ordinalIgnoreCaseComparison = StringComparison.OrdinalIgnoreCase;


    [JsonConstructor]
    public Player(string name = "")
    {
        Name = name;
        StreamData.LiveData.Update(new TwitchStreamData());
    }

    public void Initialize()
    {
        LoadHead();
        CleanUpUUID();

        IsUUIDEmpty = string.IsNullOrEmpty(UUID);
    }

    public void ShowCategory(bool option)
    {
        StreamData.LiveData.GameNameVisibility = option;
    }
    public void ShowTeamName(bool option)
    {
        IsShowingTeamName = option;
    }

    public async Task CompleteData(bool completeUUID = true)
    {
        try
        {
            if (string.IsNullOrEmpty(UUID) && completeUUID)
            {
                var data = await GetDataFromInGameName();
                if (data != null)
                {
                    UUID = data.Value.UUID;
                }
            }
            if (string.IsNullOrEmpty(InGameName))
            {
                var data = await GetDataFromUUID();
                if (data != null)
                {
                    InGameName = data.Value.InGameName;
                }
            }
            
            await UpdateHeadImage();
        }
        catch (Exception ex)
        {
            DialogBox.Show("Error: " + ex.Message + " - " + ex.StackTrace, "ERROR completing data", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task<ResponseMojangProfileAPI?> GetDataFromUUID()
    {
        if (string.IsNullOrEmpty(UUID)) return null;
        
        string result = await Helper.MakeRequestAsString($"https://sessionserver.mojang.com/session/minecraft/profile/{UUID}");
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
    }
    public async Task<ResponseMojangProfileAPI?> GetDataFromInGameName()
    {
        if (string.IsNullOrEmpty(InGameName)) return null;
        
        string result = await Helper.MakeRequestAsString($"https://api.mojang.com/users/profiles/minecraft/{InGameName}");
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
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
    public async Task ForceUpdateHeadImage()
    {
        if (string.IsNullOrEmpty(InGameName)) return;
        Image = await RequestHeadImage();
    }
    private async Task<BitmapImage?> RequestHeadImage()
    {
        using HttpClient client = new();
        if (string.IsNullOrEmpty(InGameName)) return null;

        string path = $"https://minotar.net/helm/{InGameName}/180.png";
        HttpResponseMessage response = await client.GetAsync(path);
        if (!response.IsSuccessStatusCode) return null;

        byte[] stream = await response.Content.ReadAsByteArrayAsync();
        ImageStream = stream;
        return Helper.LoadImageFromStream(stream);
    }
    public void UpdateHeadBitmap()
    {
        if (ImageStream == null) return;
        Image = Helper.LoadImageFromStream(ImageStream);
    }

    public void Clear()
    {
        Name = string.Empty;
        PersonalBest = string.Empty;

        StreamData.Clear();
    }
    public void ClearFromController()
    {
        IsUsedInPov = false;

        StreamData.LiveData.Clear();
    }

    public void CleanUpUUID()
    {
        if (string.IsNullOrEmpty(UUID)) return;

        UUID = UUID.Replace("-", "");
    }

    public string GetDisplayName()
    {
        return Name!;
    }
    public string GetPersonalBest()
    {
        return PersonalBest ?? "Unk";
    }
    public string GetTwitchName()
    {
        if (string.IsNullOrEmpty(StreamData.LiveData.ID)) return StreamData.GetCorrectName();

        return StreamData.LiveData.UserLogin;
    }
    public string GetHeadViewParametr()
    {
        return InGameName!;
    }
    public bool IsFromWhiteList()
    {
        return true;
    }

    public bool EqualsNoDialog(Player player)
    {
        if (!string.IsNullOrEmpty(UUID) && !string.IsNullOrEmpty(player.UUID) && UUID.Equals(player.UUID)) return true;
        if (Name!.Equals(player.Name, _ordinalIgnoreCaseComparison)) return true;
        if (InGameName!.Equals(player.InGameName, _ordinalIgnoreCaseComparison)) return true;
        return StreamData.EqualsNoDialog(player.StreamData);
    }
    
    public bool Equals(Player player)
    {
        if (Name!.Equals(player.Name, _ordinalIgnoreCaseComparison))
        {
            DialogBox.Show($"Player with name \"{player.Name}\" already exists", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }

        if (InGameName!.Equals(player.InGameName, _ordinalIgnoreCaseComparison))
        {
            DialogBox.Show($"Player with in game name \"{player.InGameName}\" already exists", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }

        if (StreamData.Equals(player.StreamData)) return true;
        return false;
    }
}
