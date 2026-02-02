namespace TournamentTool.Core.Exceptions;

public class BackgroundServiceException : Exception
{
    public BackgroundServiceException(string message) : base(message) { }
}