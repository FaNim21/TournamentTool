using TournamentTool.Models;

namespace TournamentTool.Interfaces;

public interface IPovDragAndDropContext
{
    public PointOfView? CurrentChosenPOV { get; set; }
}