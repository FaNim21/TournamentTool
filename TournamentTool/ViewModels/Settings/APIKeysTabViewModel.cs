using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;
using TournamentTool.Models;

namespace TournamentTool.ViewModels.Settings;

public class APIKeysTabViewModel : BaseViewModel, ISettingsTab
{
    private readonly APIKeys _apiKeys;

    public bool IsChosen { get; private set; }
    public string Name { get; }

    public string MaskedTwitchAccessToken { get; set; } = string.Empty;

    public string CustomTwitchClientID
    {
        get => _apiKeys.CustomTwitchClientID;
        set
        {
            _apiKeys.CustomTwitchClientID = value;
            OnPropertyChanged(nameof(CustomTwitchClientID));
        }
    }
    public string MaskedCustomTwitchClientID { get; private set; } = string.Empty;
    private bool _showCustomTwitchClientID;
    public bool ShowCustomTwitchClientID
    {
        get => _showCustomTwitchClientID;
        set
        {
            if (!value)
            {
                MaskedCustomTwitchClientID = new string('*', CustomTwitchClientID.Length);
                OnPropertyChanged(nameof(MaskedCustomTwitchClientID));
            }
            _showCustomTwitchClientID = value;
            OnPropertyChanged(nameof(ShowCustomTwitchClientID));
        }
    }

    public string MCSRRankedAPI
    {
        get => _apiKeys.MCSRRankedAPI;
        set
        {
            _apiKeys.MCSRRankedAPI = value;
            OnPropertyChanged(nameof(MCSRRankedAPI));
        }
    }
    public string MaskedMCSRRankedAPI { get; private set; } = string.Empty;
    
    private bool _showMCSRRankedAPI;
    public bool ShowMCSRRankedAPI
    {
        get => _showMCSRRankedAPI;
        set
        {
            if (!value)
            {
                MaskedMCSRRankedAPI = new string('*', MCSRRankedAPI.Length);
                OnPropertyChanged(nameof(MaskedMCSRRankedAPI));
            }
            _showMCSRRankedAPI = value;
            OnPropertyChanged(nameof(ShowMCSRRankedAPI));
        }
    }

    public string PacemanAPI
    {
        get => _apiKeys.PacemanAPI;
        set
        {
            _apiKeys.PacemanAPI = value;
            OnPropertyChanged(nameof(PacemanAPI));
        }
    }
    public string MaskedPacemanAPI { get; private set; } = string.Empty;

    private bool _showPacemanAPI;
    public bool ShowPacemanAPI
    {
        get => _showPacemanAPI;
        set
        {
            if (!value)
            {
                MaskedPacemanAPI = new string('*', PacemanAPI.Length);
                OnPropertyChanged(nameof(MaskedPacemanAPI));
            }
            _showPacemanAPI = value;
            OnPropertyChanged(nameof(ShowPacemanAPI));
        }
    }

    public ICommand ClearAccessTokenCommand { get; private set; }


    public APIKeysTabViewModel(APIKeys apiKeys)
    {
        Name = "apikeys";
        _apiKeys = apiKeys;

        ClearAccessTokenCommand = new RelayCommand(ClearAccessToken);
    }
    public void OnOpen()
    {
        MaskedTwitchAccessToken = string.IsNullOrEmpty(_apiKeys.TwitchAccessToken) ? string.Empty : new string('*', _apiKeys.TwitchAccessToken.Length);
        OnPropertyChanged(nameof(MaskedTwitchAccessToken));
        
        ShowPacemanAPI = false;
        ShowMCSRRankedAPI = false;
        ShowCustomTwitchClientID = false;
        
        IsChosen = true;
    }
    public void OnClose()
    {
        IsChosen = true;
    }

    private void ClearAccessToken()
    {
        var result = DialogBox.Show("Are you sure you want to clear your twitch access token from tournament tool?", "Clearing access token", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        
        _apiKeys.TwitchAccessToken = string.Empty;
        MaskedTwitchAccessToken = string.Empty;
        OnPropertyChanged(nameof(MaskedTwitchAccessToken));
    }
}