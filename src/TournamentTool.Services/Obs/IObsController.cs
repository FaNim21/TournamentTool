using ObsWebSocket.Core.Protocol;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Events;
using ObsWebSocket.Core.Protocol.Responses;

namespace TournamentTool.Services.Obs;

public interface IObsController
{
    event EventHandler<SceneItemListReindexedPayload>? SceneItemUpdateRequested;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    event EventHandler<CurrentProgramSceneChangedPayload>? CurrentProgramSceneChanged;
    event EventHandler<CurrentPreviewSceneChangedPayload>? CurrentPreviewSceneChanged;
    event EventHandler? SceneTransitionStarted;
    event EventHandler? StudioModeChanged;
    event EventHandler<SceneCreatedPayload>? SceneCreated;
    event EventHandler<SceneRemovedPayload>? SceneRemoved;
    
    bool IsConnectedToWebSocket { get; }
    bool StudioMode { get; }

    Task ConnectAsync();
    Task DisconnectAsync();

    Task SwitchStudioModeAsync();
    Task TransitionStudioModeAsync();

    Task<GetInputSettingsResponseData?> GetInputSettingsAsync(string sourceUuid);
    Task<GetVideoSettingsResponseData?> GetVideoSettingsAsync();
    Task<GetCurrentProgramSceneResponseData?> GetCurrentProgramSceneAsync();
    Task<GetSceneListResponseData?> GetSceneListAsync();
    
    Task CallBatchAsync(IEnumerable<BatchRequestItem> requests);

    Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input);
    Task SetCurrentPreviewSceneAsync(string scene);

    Task<List<SceneItemStub>> GetSceneItemListAsync(string? sceneName = null, string? sceneUuid = null);
    Task<List<SceneItemStub>> GetGroupSceneItemListAsync(string group);

    void SetStartedTransition(bool option);
}