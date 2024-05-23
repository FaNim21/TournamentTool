using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Net.Http;

namespace TournamentTool.Utils;

public static class Helper
{
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

    public static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        if (parent is T _child) return _child;

        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                return typedChild;
            }

            T? foundChild = FindChild<T>(child);
            if (foundChild != null) return foundChild;
        }

        return null;
    }

    public static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T ancestor) return ancestor;
            current = VisualTreeHelper.GetParent(current);
        } while (current != null);
        return null;
    }

    public static T? GetFocusedUIElement<T>() where T : DependencyObject
    {
        IInputElement focusedControl = Keyboard.FocusedElement;
        T? result = FindChild<T>((DependencyObject)focusedControl);
        return result;
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

    public static BitmapImage LoadImageFromStream(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static async Task<string> MakeRequestAsString(string ApiUrl)
    {
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(ApiUrl);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");

        return await response.Content.ReadAsStringAsync();
    }
}
