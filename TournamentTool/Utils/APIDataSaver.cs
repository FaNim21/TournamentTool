using System.Diagnostics;
using System.IO;

namespace TournamentTool.Utils;

public class APIDataSaver
{
    public APIDataSaver()
    {
        Directory.CreateDirectory(Consts.AppAPIPath);
    }

    public string CheckFile(string fileName)
    {
        string path = Path.Combine(Consts.AppAPIPath, fileName + ".txt");
        string output = string.Empty;

        if (!File.Exists(path))
        {
            File.Create(path);
        }
        else
        {
            output = File.ReadAllText(path);
        }

        return output;
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
