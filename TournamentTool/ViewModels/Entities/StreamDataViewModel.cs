using System.Windows;
using System.Windows.Media;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class TwitchStreamDataViewModel : BaseViewModel
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


    public void Update(TwitchStreamDataViewModel data)
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
                StatusLabelColor = new SolidColorBrush(Consts.LiveColor);
            else
                StatusLabelColor = new SolidColorBrush(Consts.OfflineColor);
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
        ID = string.Empty;
        BroadcasterID = string.Empty;
        UserName = string.Empty;
        UserLogin = string.Empty;
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
                StatusLabelColor = new SolidColorBrush(Consts.DefaultColor);
            else
                StatusLabelColor = new SolidColorBrush(Consts.OfflineColor);
        });
        Update();
    }
}

/// <summary>
/// To moze kiedys jeszcze raz przerobic na 1.0 pod listy, a nie takie dodawanie
/// - wtedy natomiast uwzglednic szukanie duplikatow po typie stream'a
/// - czyli dodawanie streamow do listy z opcja wybrania kolejnosci (glownie ze wzgledu na twitch api)
/// </summary>
public class StreamDataViewModel : BaseViewModel
{
    private StreamData _streamData;
    
    public TwitchStreamDataViewModel LiveData { get; set; } = new();

    public string Main
    {
        get => _streamData.Main;
        set
        {
            _streamData.Main = value;
            OnPropertyChanged(nameof(Main));
        }
    }
    public string Alt
    {
        get => _streamData.Alt;
        set
        {
            _streamData.Alt = value;
            OnPropertyChanged(nameof(Alt));
        }
    }

    public string Other
    {
        get => _streamData.Other;
        set
        {
            _streamData.Other = value;
            OnPropertyChanged(nameof(Other));
        }
    }
    public StreamType OtherType
    {
        get => _streamData.OtherType;
        set
        {
            _streamData.OtherType = value;
            OnPropertyChanged(nameof(OtherType));
        }
    }
    
    private bool _isLive;
    public bool IsLive
    {
        get => _isLive;
        set
        {
            _isLive = value;
            OnPropertyChanged(nameof(IsLive));
        }
    }
    
    private const StringComparison _ordinalIgnoreCaseComparison = StringComparison.OrdinalIgnoreCase;


    public StreamDataViewModel(StreamData data)
    {
        _streamData = data;
        LiveData.Update(new TwitchStreamDataViewModel());
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

    public StreamDisplayInfo GetCorrectStream()
    {
        if (!string.IsNullOrEmpty(Other))
            return new StreamDisplayInfo(Other, OtherType);
        if (string.IsNullOrEmpty(Main))
            return new StreamDisplayInfo(Alt, StreamType.twitch);
        return new StreamDisplayInfo(Main, StreamType.twitch);
    }

    public bool EqualsNoDialog(StreamData data)
    {
        if (ExistName(data.Main)) return true;
        if (ExistName(data.Alt)) return true;
        if (!string.IsNullOrEmpty(data.Other) && Other.Equals(data.Other, _ordinalIgnoreCaseComparison)) return true;
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
        if (!string.IsNullOrEmpty(data.Other) && Other.Equals(data.Other, _ordinalIgnoreCaseComparison))
        {
            DialogBox.Show($"Other name \"{data.Other}\" is already assigned to another player", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    public bool IsOtherEmpty()
    {
        return string.IsNullOrEmpty(Other);
    }
    public bool IsNullOrEmpty()
    {
        return IsMainEmpty() && IsAltEmpty() && IsOtherEmpty();
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
