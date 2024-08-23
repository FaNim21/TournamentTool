using System.Diagnostics;
using System.IO;

namespace TournamentTool.Utils;

public class APIDataSaver
{
    public APIDataSaver()
    {
        if (!Directory.Exists(Consts.AppAPIPath))
            Directory.CreateDirectory(Consts.AppAPIPath);
    }

    public void CheckFile(string fileName)
    {
        string path = Path.Combine(Consts.AppAPIPath, fileName + ".txt");
        if (!File.Exists(path)) File.Create(path);
    }
    public void UpdateFileContent(string fileName, object content)
    {
        string path = Path.Combine(Consts.AppAPIPath, fileName + ".txt");

        try
        {
            using StreamWriter writer = new(path);
            writer.Write(content.ToString());
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error updating api file {fileName}: {ex.Message}");
        }
    }
}
