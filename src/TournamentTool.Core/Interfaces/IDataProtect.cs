namespace TournamentTool.Core.Interfaces;

public interface IDataProtect
{
    byte[] Protect(string json);
    string UnProtect(byte[] data);
}