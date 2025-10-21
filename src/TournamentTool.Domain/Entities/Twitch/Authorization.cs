namespace TournamentTool.Domain.Entities.Twitch;

public class Authorization
{
    public string Code { get; }

    public Authorization(string code)
    {
        Code = code;
    }
}
