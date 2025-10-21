namespace TournamentTool.ViewModels.Selectable.Controller.SidePanel;

public interface IGroupableItem
{
    string GroupKey { get; }
    int GroupSortOrder { get; }
    long SortValue { get; }
    string SecondarySortValue { get; }
    string Identifier { get; }

    void Update();
}