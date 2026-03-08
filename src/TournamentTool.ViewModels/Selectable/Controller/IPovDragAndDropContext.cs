using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Obs;

namespace TournamentTool.ViewModels.Selectable.Controller;

public interface IPovDragAndDropContext
{
    public PointOfView? CurrentChosenPOV { get; set; }
}