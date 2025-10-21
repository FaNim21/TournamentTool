using System.Diagnostics;

namespace TournamentTool.Core.Utils;

public static class Helper
{
    public static void StartProcess(string path)
    {
        var processStart = new ProcessStartInfo(path)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(processStart);
    }
    
    public static void SaveLog(string output, string logName = "log")
    {
        string date = DateTimeOffset.Now.ToString("yyyy-MM-dd_HH.mm");
        string fileName = $"{logName} {date}.txt";

        int count = 1;
        while (File.Exists(Consts.LogsPath + "\\" + fileName))
        {
            fileName = $"{logName} {date} [{count}].txt";
            count++;
        }

        File.WriteAllText(Consts.LogsPath + "\\" + fileName, output);
    }

    public static string CaptalizeAll(this string text)
    {
        string[] words = text.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            words[i] = char.ToUpper(words[i][0]) + words[i][1..];
        }

        return string.Join(' ', words);
    }

    /// <summary>
    /// Removing Json as extension from end of string
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Name of file from path as string</returns>
    public static string GetFileNameWithoutExtension(string? path)
    {
        string output = Path.GetFileName(path) ?? "";
        if (output.EndsWith(".json"))
            output = output.Remove(output.Length - 5);

        return output;
    }

    public static string GetUniqueName(string oldName, string newName, Func<string, bool> CheckIfUnique)
    {
        string baseName = oldName;
        int suffix = 1;

        while (!CheckIfUnique(newName))
        {
            if (baseName.EndsWith(")"))
            {
                int openParenthesisIndex = baseName.LastIndexOf('(');
                if (openParenthesisIndex > 0)
                {
                    int closeParenthesisIndex = baseName.LastIndexOf(')');
                    if (closeParenthesisIndex == baseName.Length - 1)
                    {
                        string suffixStr = baseName.Substring(openParenthesisIndex + 1, closeParenthesisIndex - openParenthesisIndex - 1);
                        if (int.TryParse(suffixStr, out int existingSuffix))
                        {
                            suffix = existingSuffix + 1;
                            baseName = baseName[..openParenthesisIndex].TrimEnd();
                        }
                    }
                }
            }

            newName = $"{baseName} ({suffix++})";
        }

        return newName;
    }

    public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue) where TAttribute : Attribute
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString());
        var attributes = memberInfo[0].GetCustomAttributes(typeof(TAttribute), false);

        return attributes.Length > 0 ? (TAttribute)attributes[0] : null;
    }
}
