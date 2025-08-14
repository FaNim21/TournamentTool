using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Modules.Logging;
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

public class PlayerViewModel : BaseViewModel, IPlayer
{
    public Player Data { get; private set; }

    public Guid Id => Data.Id;
    public StreamDataViewModel StreamData { get; set; }

    private BitmapImage? _image;
    public BitmapImage? Image
    {
        get => _image;
        set
        {
            _image = value;
            OnPropertyChanged(nameof(Image));
        }
    }
    
    public string UUID
    {
        get => Data.UUID;
        set
        {
            Data.UUID = value;
            OnPropertyChanged(nameof(UUID));
            IsUUIDEmpty = string.IsNullOrEmpty(UUID);
        }
    }
    public string? Name
    {
        get => Data.Name;
        set
        {
            Data.Name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
    public string? InGameName
    {
        get => Data.InGameName;
        set
        {
            Data.InGameName = value;
            OnPropertyChanged(nameof(InGameName));
        }
    }
    public string PersonalBest
    {
        get => Data.PersonalBest;
        set
        {
            Data.PersonalBest = value;
            OnPropertyChanged(nameof(PersonalBest));
        }
    }
    public string? TeamName
    {
        get => Data.TeamName;
        set
        {
            Data.TeamName = value;
            OnPropertyChanged(nameof(TeamName));
        }
    }
    
    private bool _isShowingTeamName;
    public bool IsShowingTeamName
    {
        get => _isShowingTeamName;
        set
        {
            _isShowingTeamName = value;
            OnPropertyChanged(nameof(IsShowingTeamName));
        }
    }
        
    private bool _isUsedInPov;
    public bool IsUsedInPov
    {
        get => _isUsedInPov;
        set
        {
            _isUsedInPov = value;
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }
    
    private bool _isUsedInPreview;
    public bool IsUsedInPreview
    {
        get => _isUsedInPreview;
        set
        {
            _isUsedInPreview = value;
            OnPropertyChanged(nameof(IsUsedInPreview));
        }
    }
        
    private bool _isUUIDEmpty;
    public bool IsUUIDEmpty
    {
        get => _isUUIDEmpty;
        set
        {
            _isUUIDEmpty = value;
            OnPropertyChanged(nameof(IsUUIDEmpty));
        }
    }
    
    private bool _isLive = true;
    public bool IsLive
    {
        get => _isLive;
        set
        {
            _isLive = value;
            OnPropertyChanged(nameof(IsLive));
        }
    }

    public bool isStreamLive => StreamData.IsLive;
    
    public string DisplayName => Name!;
    public string GetPersonalBest => PersonalBest ?? "Unk";
    public string HeadViewParameter => InGameName!;
    public StreamDisplayInfo StreamDisplayInfo 
    {
        get
        {
            if (string.IsNullOrEmpty(StreamData.LiveData.ID))
                return StreamData.GetCorrectStream();
            return new StreamDisplayInfo(StreamData.LiveData.UserLogin, StreamType.twitch);
        }
    }
    public bool IsFromWhitelist => true;
    
    private const StringComparison _ordinalIgnoreCaseComparison = StringComparison.OrdinalIgnoreCase;


    public PlayerViewModel(Player? data = null)
    {
        if (data == null)
        {
            Data = new Player();
            StreamData = new StreamDataViewModel(Data.StreamData);
            return;
        }
        Data = data;
        StreamData = new StreamDataViewModel(Data.StreamData);
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
                
            await UpdateHeadImageAsync();
        }
        catch (Exception ex)
        {
            LogService.Error("ERROR completing data: " + ex.Message + " - " + ex.StackTrace);
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
        if (Data.ImageStream == null || Image != null) return;
        Image = Helper.LoadImageFromStream(Data.ImageStream);
    }

    public void UpdateHeadImage()
    {
        Task.Run(async () => await UpdateHeadImageAsync());
    }
    public async Task UpdateHeadImageAsync()
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
    
        string path = $"https://minotar.net/helm/{InGameName}/8.png";
        HttpResponseMessage response = await client.GetAsync(path);
        if (!response.IsSuccessStatusCode) return null;
    
        byte[] stream = await response.Content.ReadAsByteArrayAsync();
        Data.ImageStream = stream;
        return Helper.LoadImageFromStream(stream);
    }
    public void UpdateHeadBitmap()
    {
        if (Data.ImageStream == null) return;
        Image = Helper.LoadImageFromStream(Data.ImageStream);
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
    }
    
    public void CleanUpUUID()
    {
        if (string.IsNullOrEmpty(UUID)) return;
    
        UUID = UUID.Replace("-", "");
    }
    
    public bool EqualsNoDialog(Player player)
    {
        if (!string.IsNullOrEmpty(UUID) && !string.IsNullOrEmpty(player.UUID) && UUID.Equals(player.UUID)) return true;
        if (Name!.Trim().Equals(player.Name!.Trim(), _ordinalIgnoreCaseComparison)) return true;
        if (InGameName!.Equals(player.InGameName, _ordinalIgnoreCaseComparison)) return true;
        return StreamData.EqualsNoDialog(player.StreamData);
    }
        
    public bool Equals(Player player)
    {
        if (Name!.Trim().Equals(player.Name!.Trim(), _ordinalIgnoreCaseComparison))
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