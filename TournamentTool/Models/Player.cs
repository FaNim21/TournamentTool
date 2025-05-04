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

    private const StringComparison _ordinalIgnoreCaseComparison = StringComparison.OrdinalIgnoreCase;


    public StreamData()
    {
        LiveData.Update(new TwitchStreamData());
    }
    
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

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UUID { get; set; } = string.Empty;
    public byte[]? ImageStream { get; set; }
    public string? Name { get; set; } = string.Empty;
    public StreamData StreamData { get; set; } = new();
    public string? InGameName { get; set; } = string.Empty;
    public string PersonalBest { get; set; } = string.Empty;
    public string? TeamName { get; set; } = string.Empty;


    /*
    [JsonConstructor]
    public Player()
    {
        Name = name;
        StreamData.LiveData.Update(new TwitchStreamData());
    }
*/
}
