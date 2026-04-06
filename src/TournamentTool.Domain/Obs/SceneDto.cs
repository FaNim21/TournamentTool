namespace TournamentTool.Domain.Obs;

public record SceneDto(string Name, string Uuid)
{
    public static SceneDto Empty() => new(string.Empty, string.Empty);
    public static SceneDto Create(string? name, string? uuid) => new(name ?? string.Empty, uuid ?? string.Empty);
}