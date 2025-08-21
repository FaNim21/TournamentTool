using System.IO;
using System.Net;
using TournamentTool.Modules.Logging;

namespace TournamentTool.Utils;

public class WebServer
{
    private readonly HttpListener listener;
    private readonly string expectedState;

    public WebServer(string uri, string state)
    {
        expectedState = state;
        
        listener = new HttpListener();
        listener.Prefixes.Add(uri);
    }

    public async Task<Models.Twitch.Authorization?> Listen()
    {
        try
        {
            listener.Start();
        }
        catch (Exception ex)
        {
            LogService.Error($"Error: {ex.Message} - {ex.StackTrace}");
            return null;
        }
        return await OnRequest();
    }

    private async Task<Models.Twitch.Authorization?> OnRequest()
    {
        Models.Twitch.Authorization? authorization = null;

        while (listener.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            var req = ctx.Request;
            var resp = ctx.Response;
            
            try
            {
                if (req.QueryString["code"] is { } code &&
                    req.QueryString["state"] == expectedState)
                {
                    authorization = new Models.Twitch.Authorization(code);

                    resp.StatusCode = 302;
                    resp.RedirectLocation = "http://localhost:8080/redirect/success";
                    resp.Close();
                }
                else if (req.Url?.AbsolutePath.EndsWith("/redirect/success") == true)
                {
                    resp.StatusCode = 200;
                    resp.ContentType = "text/html; charset=UTF-8";
    
                    const string html = "<!DOCTYPE html><html><head><meta charset='utf-8' />" +
                                        "<title>Twitch Auth</title></head>" +
                                        "<body style='font-family:sans-serif; text-align:center; margin-top:50px; color:green;'>" +
                                        "<h2>Authorization successful</h2>" +
                                        "<p>You can now close this window and return to the app.</p>" +
                                        "</body></html>";

                    await using var writer = new StreamWriter(resp.OutputStream);
                    await writer.WriteAsync(html);

                    _ = Task.Run(() =>
                    {
                        Thread.Sleep(500);
                        try
                        {
                            if (listener.IsListening)
                            {
                                listener.Stop();
                                listener.Close();
                            }
                        } catch { /**/ }
                    });
                }}
            catch (Exception ex)
            {
                LogService.Error($"Error in WebServer: {ex.Message}");
                resp.StatusCode = 500;
                await using var w = new StreamWriter(resp.OutputStream);
                await w.WriteAsync("<html><body><h2>Internal error.</h2></body></html>");
            }
            finally
            {
                resp.Close();
            }
        }

        return authorization;
    }
}
