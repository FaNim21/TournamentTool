using System.IO;
using System.Text;

namespace TournamentTool.Utils;

public class APIDataSaver
{
    public APIDataSaver()
    {
        Directory.CreateDirectory(Consts.AppAPIPath);
    }

    public async Task UpdateFileContent(string fileName, object content)
    {
        string path = Path.Combine(Consts.AppAPIPath, fileName + ".txt");
        await WriteFileAsync(path, content.ToString() ?? string.Empty);
    }
    
    public static async Task WriteFileAsync(string path, string content, int retries = 3, int delayMs = 100)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
                await using var writer = new StreamWriter(fs, Encoding.UTF8);
                await writer.WriteLineAsync(content);
                return;
            }
            catch (IOException)
            {
                if (i == retries - 1) return;
                await Task.Delay(delayMs);
            }
        }
    }}
