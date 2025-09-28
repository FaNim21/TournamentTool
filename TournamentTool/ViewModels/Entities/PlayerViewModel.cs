using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Modules.Logging;
using TournamentTool.Services;
using TournamentTool.Services.External;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class PlayerViewModel : BaseViewModel, IPlayer
{
    private readonly IMinecraftDataService _minecraftDataService;
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

    public bool IsStreamLive => StreamData.IsLive;
    
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


    public PlayerViewModel(IMinecraftDataService minecraftDataService, Player? data = null)
    {
        _minecraftDataService = minecraftDataService;

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
        return await _minecraftDataService.GetDataFromUUID(UUID);
    }
    public async Task<ResponseMojangProfileAPI?> GetDataFromInGameName()
    {
        if (string.IsNullOrEmpty(InGameName)) return null;
        return await _minecraftDataService.GetDataFromIGN(InGameName);
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
        if (string.IsNullOrEmpty(InGameName)) return null;
    
        string id = string.IsNullOrEmpty(UUID) ? InGameName : UUID;
        byte[] stream = await _minecraftDataService.GetPlayerHeadAsync(id, 32);
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