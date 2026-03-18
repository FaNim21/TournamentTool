using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public interface ISceneController
{
    
}

public class SceneController : ISceneController
{
    private readonly IObsController _obs;

    public SceneController(IObsController obs, ITournamentPlayerRepository playerRepository,
        ILoggingService logger, IBindingEngine bindingEngine, ISettingsProvider settingsProvider, IWindowService windowService)
    {
        _obs = obs;
    }
}