using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Selectable.Controller;

public interface IPovDragAndDropContext
{
    public PointOfView? CurrentChosenPOV { get; set; }
}