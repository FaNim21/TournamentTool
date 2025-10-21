using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.ViewModels.Modals;

namespace TournamentTool.ViewModels.Entities;

public class TwitchStreamDataViewModel : BaseViewModel
{
    public TwitchStreamData Data { get; }

    public string ID
    {
        get => Data.ID;
        set => Data.ID = value;
    }
    public StreamType ServiceType => Data.ServiceType;

    public string BroadcasterID
    {
        get => Data.BroadcasterID;
        set => Data.BroadcasterID = value;
    }
    public string UserLogin
    {
        get => Data.UserLogin;
        set => Data.UserLogin = value;
    }
    public string UserName
    {
        get => Data.UserName;
        set => Data.UserName = value;
    }
    public string Title 
    {
        get => Data.Title;
        set => Data.Title = value;
    }
    public int ViewerCount 
    {
        get => Data.ViewerCount;
        set => Data.ViewerCount = value;
    }
    public DateTime StartedAt 
    {
        get => Data.StartedAt;
        set => Data.StartedAt = value;
    }
    public string Language 
    {
        get => Data.Language;
        set => Data.Language = value;
    }
    public string ThumbnailUrl 
    {
        get => Data.ThumbnailUrl;
        set => Data.ThumbnailUrl = value;
    }

    public string Status
    {
        get => Data.Status;
        set => Data.Status = value;
    }
    public string GameName 
    {
        get => Data.GameName;
        set => Data.GameName = value;
    }

    public string? StatusLabelColor { get; set; }
    
    private bool _wasUpdated;
    public bool WasUpdated
    {
        get => _wasUpdated;
        set
        {
            _wasUpdated = value;
            OnPropertyChanged(nameof(WasUpdated));
        }
    }

    public bool GameNameVisibility
    {
        get => Data.GameNameVisibility;
        set
        {
            Data.GameNameVisibility = value;
            OnPropertyChanged(nameof(GameNameVisibility));
        }
    }


    public TwitchStreamDataViewModel(TwitchStreamData data, IDispatcherService dispatcher) : base(dispatcher)
    {
        Data = data;
        Update(data);
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
        StatusLabelColor = Status.Equals("live", StringComparison.OrdinalIgnoreCase) ? Consts.LiveColor : Consts.OfflineColor;

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
        StatusLabelColor = !isUsingTwitchApi ? Consts.DefaultColor : Consts.OfflineColor;
        
        Update();
    }
}

/// <summary>
/// TODO: 5 To moze kiedys jeszcze raz przerobic na 1.0 pod listy, a nie takie dodawanie
/// - wtedy natomiast uwzglednic szukanie duplikatow po typie stream'a
/// - czyli dodawanie streamow do listy z opcja wybrania kolejnosci (glownie ze wzgledu na twitch api)
/// </summary>
public class StreamDataViewModel : BaseViewModel
{
    private readonly IDialogService _dialogService;
    
    public StreamData Data { get; }
    public TwitchStreamDataViewModel Live { get; }

    public string Main
    {
        get => Data.Main;
        set
        {
            Data.Main = value;
            OnPropertyChanged(nameof(Main));
        }
    }
    public string Alt
    {
        get => Data.Alt;
        set
        {
            Data.Alt = value;
            OnPropertyChanged(nameof(Alt));
        }
    }

    public string Other
    {
        get => Data.Other;
        set
        {
            Data.Other = value;
            OnPropertyChanged(nameof(Other));
        }
    }
    public StreamType OtherType
    {
        get => Data.OtherType;
        set
        {
            Data.OtherType = value;
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


    public StreamDataViewModel(StreamData data, IDispatcherService dispatcher, IDialogService dialogService) : base(dispatcher)
    {
        _dialogService = dialogService;
        Data = data;
        Data.StreamService = new TwitchStreamData();
        // to wiadomo do przerobienia, bo trzeba to porzadnie zrobic po tym jak skoncze folder refactor
        Live = new TwitchStreamDataViewModel((TwitchStreamData)Data.StreamService, dispatcher);
    }
    
    public void SetName(string name)
    {
        if (string.IsNullOrEmpty(name) || ExistName(name)) return;

        if (Data.IsMainEmpty())
        {
            Main = name;
        }
        else if (Data.IsAltEmpty())
        {
            Alt = name;
        }
    }
    public bool ExistName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return Main.Equals(name, _ordinalIgnoreCaseComparison) || Alt.Equals(name, _ordinalIgnoreCaseComparison);
    }

    public StreamDisplayInfo GetStreamDisplayInfo() => Data.GetStreamDisplayInfo();

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
            _dialogService.Show($"Twitch name \"{data.Main}\" is already assigned to another player", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }
        if (ExistName(data.Alt))
        {
            _dialogService.Show($"Twitch name \"{data.Alt}\" is already assigned to another player", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }
        if (!string.IsNullOrEmpty(data.Other) && Other.Equals(data.Other, _ordinalIgnoreCaseComparison))
        {
            _dialogService.Show($"Other name \"{data.Other}\" is already assigned to another player", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }

        return false;
    }

    public bool IsNullOrEmpty()
    {
        return Data.IsMainEmpty() && Data.IsAltEmpty() && Data.IsOtherEmpty();
    }

    public void Clear()
    {
        Main = string.Empty;
        Alt = string.Empty;

        Live.Clear();
    }
}
