using System.IO;
using System.Net;
using System.Windows;
using TournamentTool.Components.Controls;

namespace TournamentTool.Utils;

public class WebServer
{
    private HttpListener listener;

    public WebServer(string uri)
    {
        listener = new HttpListener();
        listener.Prefixes.Add(uri);
    }

    public async Task<Models.Twitch.Authorization> Listen()
    {
        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return await OnRequest();
    }

    private async Task<Models.Twitch.Authorization> OnRequest()
    {
        while (listener.IsListening)
        {
            var ctx = await listener.GetContextAsync();
            var req = ctx.Request;
            var resp = ctx.Response;

            using var writer = new StreamWriter(resp.OutputStream);
            if (req.QueryString.AllKeys.Any("code".Contains))
            {
                writer.WriteLine("Successfully authorized!");
                writer.Flush();
                var authorization = new Models.Twitch.Authorization(req.QueryString["code"]!);
                return authorization;
            }
            else
            {
                writer.WriteLine("No code found in query string!");
                writer.Flush();
            }
        }
        return null!;
    }
}
