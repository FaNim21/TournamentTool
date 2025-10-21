namespace TournamentTool.Core.Interfaces;

public interface ICloseRequest
{
    /// <summary>
    /// Needs to be cleared when disposing window view model
    /// </summary>
    Action? RequestClose { get; set; }
}