using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Controls;
using TournamentTool.Modules.Logging;

namespace TournamentTool.Utils;

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

    public static object? GetItemUnderMouse<TItem>(UIElement item, Func<IInputElement, Point> getPosition) where TItem : FrameworkElement
    {
        return GetUIItemUnderMouse<TItem>(item, getPosition)?.DataContext;
    }
    public static TItem? GetUIItemUnderMouse<TItem>(UIElement item, Func<IInputElement, Point> getPosition) where TItem : FrameworkElement
    {
        var pos = getPosition(item);
        var element = item.InputHitTest(pos) as DependencyObject;
    
        while (element != null && element is not ListBoxItem)
            element = VisualTreeHelper.GetParent(element);

        return element as TItem;
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
            LogService.Error($"Error: {ex.Message} - {ex.StackTrace}");
            return null;
        }
    }
    public static BitmapImage LoadImageFromResources(string url)
    {
        var uri = new Uri($"pack://application:,,,/Resources/{url}", UriKind.Absolute);
        
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = uri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        
        return bitmap;
    }
    
    public static async Task<string> MakeRequestAsString(string ApiUrl)
    {
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(ApiUrl);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");

        return await response.Content.ReadAsStringAsync();
    }
    public static async Task<Stream> MakeRequestAsStream(string ApiUrl, string? key = null)
    {
        using HttpClient client = new();
        if (!string.IsNullOrEmpty(key))
        {
            client.DefaultRequestHeaders.Add("Private-Key", key);
        }
        
        HttpResponseMessage response = await client.GetAsync(ApiUrl);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");

        return await response.Content.ReadAsStreamAsync();
    }
    
    public static string ToPlainText(SecureString secureString)
    {
        if (secureString == null) return string.Empty;

        IntPtr unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToBSTR(secureString);
            return Marshal.PtrToStringBSTR(unmanagedString);
        }
        finally
        {
            Marshal.ZeroFreeBSTR(unmanagedString);
        }
    }
}
