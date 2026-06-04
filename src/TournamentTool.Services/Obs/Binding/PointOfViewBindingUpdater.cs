using TournamentTool.Core.Extensions;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Obs;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public interface IPointOfViewBindingUpdater
{
    void Publish(IPointOfView pointOfView, string sourceName);
}

public sealed class PointOfViewBindingUpdater : IPointOfViewBindingUpdater
{
    private readonly IBindingEngine _bindingEngine;
    private readonly Settings _settings;
    
    public PointOfViewBindingUpdater(IBindingEngine bindingEngine, ISettingsProvider settingsProvider)
    {
        _bindingEngine = bindingEngine;
        _settings = settingsProvider.Get<Settings>();
    }
    
    public void Publish(IPointOfView pointOfView, string sourceName)
    {
        string headUrl = string.Empty;
        if (pointOfView.Player != null)
        {
            headUrl = _settings.HeadAPIType.GetHeadURL(pointOfView.Player.HeadViewParameter, 180);
        }

        _bindingEngine.Publish(BindingKey.CreatePov("head", sourceName), headUrl);
        _bindingEngine.Publish(BindingKey.CreatePov("display_name", sourceName), pointOfView.DisplayedPlayer);
        _bindingEngine.Publish(BindingKey.CreatePov("ign", sourceName), pointOfView.Player?.InGameName ?? string.Empty);
        _bindingEngine.Publish(BindingKey.CreatePov("pb", sourceName), pointOfView.Player?.GetPersonalBest ?? string.Empty);
        _bindingEngine.Publish(BindingKey.CreatePov("team_name", sourceName), pointOfView.Player?.TeamName ?? string.Empty);
        _bindingEngine.Publish(BindingKey.CreatePov("stream_name", sourceName), pointOfView.Player?.StreamDisplayInfo.Name ?? string.Empty);
        _bindingEngine.Publish(BindingKey.CreatePov("stream_type", sourceName), pointOfView.Player?.StreamDisplayInfo.Type);
    }
}