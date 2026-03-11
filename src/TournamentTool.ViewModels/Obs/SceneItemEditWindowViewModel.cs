using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public class SceneItemEditWindowViewModel : BaseWindowViewModel
{
    private readonly IObsCommunicationProvider _obsCommunicationProvider;
    private readonly AppCache _appCache;

    public SceneItemViewModel SceneItem { get; }

    private InputKind _inputKind = InputKind.unsupported;
    public InputKind InputKind
    {
        get => _inputKind;
        set
        {
            if (value == _inputKind) return;
            _inputKind = value;
            OnPropertyChanged(nameof(InputKind));
        }
    }
    
    public SceneItemEditWindowViewModel(SceneItemViewModel sceneItem, IObsCommunicationProvider obsCommunicationProvider, AppCache appCache, 
        IDispatcherService dispatcher) : base(dispatcher)
    {
        _obsCommunicationProvider = obsCommunicationProvider;
        _appCache = appCache;
        SceneItem = sceneItem;

        InputKind = SceneItem.InputKind;
    }
    
    
}