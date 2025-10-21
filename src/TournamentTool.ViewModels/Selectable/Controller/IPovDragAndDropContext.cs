using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable.Controller;

public interface IPovDragAndDropContext
{
    public PointOfView? CurrentChosenPOV { get; set; }
}