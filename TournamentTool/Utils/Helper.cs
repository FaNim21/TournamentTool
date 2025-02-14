using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Net.Http;
using TournamentTool.Components.Controls;
using System.Windows.Controls;

namespace TournamentTool.Utils;

public static class Helper
{
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

    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) yield break;
        
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child is T typedChild)
                yield return typedChild;

            foreach (var childOfChild in FindVisualChildren<T>(child))
                yield return childOfChild;
        }
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

    public static T? GetViewItemFromMousePosition<T, U>(U? view, Point mousePosition) where T : Control where U : ItemsControl
    {
        HitTestResult hitTestResult = VisualTreeHelper.HitTest(view, mousePosition);
        DependencyObject? target = hitTestResult?.VisualHit;

        while (target != null && target is not T)
            target = VisualTreeHelper.GetParent(target);

        return target as T;
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
    public static async Task<BitmapImage?> LoadImageFromUrlAsync(string url)
    {
        try
        {
            using HttpClient client = new();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var imageStream = await response.Content.ReadAsStreamAsync();

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = imageStream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Rrror: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            return null;
        }
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
