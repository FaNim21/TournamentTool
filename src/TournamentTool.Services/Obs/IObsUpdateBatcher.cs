using ObsWebSocket.Core.Protocol.Requests;

namespace TournamentTool.Services.Obs;

public interface IObsUpdateBatcher
{
    void Queue(SetInputSettingsRequestData requestData);
}