namespace TournamentTool.Domain.Interfaces;

public interface IRenameItem
{
    public string Name { get; set; }

    public void ChangeName(string name);
}
