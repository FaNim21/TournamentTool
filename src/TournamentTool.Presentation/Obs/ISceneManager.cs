using System.Collections.ObjectModel;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs.Entities;

namespace TournamentTool.Presentation.Obs;

public interface ISceneManager
{
    event EventHandler? ObsConnected;
    event EventHandler? ObsDisconnected;
    event EventHandler<string>? SelectedSceneUpdated; 
    
    Scene MainScene { get; }
    Scene PreviewScene { get; }
    
    ReadOnlyObservableCollection<SceneDto> Scenes { get; }
    
    Task RefreshScenesPOVSAsync();
    
    void QueueUpdate(string sourceUuid, Dictionary<string, object> input);
    Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input);
    
    Task<GetInputSettingsResponseData?> GetItemInputSettingsAsync(string sourceUuid);
    Task<List<(SceneItemStub, SceneItemStub?)>> GetSceneItemsAsync(string sceneName, string sceneUuid);

    IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type);
}