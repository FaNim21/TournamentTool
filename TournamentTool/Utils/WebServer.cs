using System.IO;
using System.Net;
using TournamentTool.Modules.Logging;

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
            LogService.Error($"Error: {ex.Message} - {ex.StackTrace}");
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

            await using var writer = new StreamWriter(resp.OutputStream);
            if (req.QueryString.AllKeys.Any("code".Contains!))
            {
                await writer.WriteLineAsync("Successfully authorized!");
                await writer.FlushAsync();
                var authorization = new Models.Twitch.Authorization(req.QueryString["code"]!);
                return authorization;
            }
            else
            {
                await writer.WriteLineAsync("No code found in query string!");
                await writer.FlushAsync();
            }
        }
        return null!;
    }
}
