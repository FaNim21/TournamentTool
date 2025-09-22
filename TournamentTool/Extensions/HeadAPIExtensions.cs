using TournamentTool.Models;

namespace TournamentTool.Extensions;

public static class HeadAPIExtensions
{
    public static string GetHeadURL(this HeadAPIType type, string id, int size) =>
        type switch
        {
            HeadAPIType.minotar => $"https://minotar.net/helm/{id}/{size}.png",
            HeadAPIType.mcheads => $"https://mc-heads.net/avatar/{id}/{size}",
            _ => string.Empty
        };
}