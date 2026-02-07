using TournamentTool.Core.Interfaces;

namespace TournamentTool.Core.Factories;

public interface IPopupViewModelFactory
{
    public IPopupViewModel Create(string text, TimeSpan duration);
}