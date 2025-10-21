namespace TournamentTool.Core.Interfaces;

public interface IClipboardService
{
    string GetText();
    void SetText(string text);

    void Clear();
}