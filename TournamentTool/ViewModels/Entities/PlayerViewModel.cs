using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class StreamDataViewModel : BaseViewModel
{
    
}

public class TwitchStreamDataViewModel : BaseViewModel
{
    
}

public class PlayerViewModel : BaseViewModel, IPlayer
{
    public Player Data { get; private set; }

    public Guid Id => Data.Id;
    public StreamData StreamData => Data.StreamData;
    
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
    
    public string DisplayName => Name!;
    public string GetPersonalBest => PersonalBest ?? "Unk";
    public string HeadViewParameter => InGameName!;
    public string TwitchName 
    {
        get
        {
            if (string.IsNullOrEmpty(StreamData.LiveData.ID)) return StreamData.GetCorrectName();
    
            return StreamData.LiveData.UserLogin;
        }
    }
    public bool IsFromWhitelist => true;
    
    private const StringComparison _ordinalIgnoreCaseComparison = StringComparison.OrdinalIgnoreCase;


    public PlayerViewModel(Player? data = null)
    {
        if (data == null)
        {
            Data = new Player();
            return;
        }
        Data = data;
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
        if (Data.ImageStream == null) return;
        Image = Helper.LoadImageFromStream(Data.ImageStream);
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
    
        StreamData.LiveData.Clear();
    }
    
    public void CleanUpUUID()
    {
        if (string.IsNullOrEmpty(UUID)) return;
    
        UUID = UUID.Replace("-", "");
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