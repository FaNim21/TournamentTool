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
        listener.Prefixes.Add(uri.EndsWith("/") ? uri : uri + "/");
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
                if (req.Url?.AbsolutePath == "/")
                {
                    resp.StatusCode = 200;
                    resp.ContentType = "text/html; charset=UTF-8";
                    
                    string html = $$"""
                                    <!DOCTYPE html>
                                    <html>
                                    <head>
                                        <meta charset='utf-8' />
                                        <title>Twitch Authentication</title>
                                    </head>
                                    <body style='font-family:sans-serif; text-align:center; margin-top:50px;'>
                                        <h2>Authenticating...</h2>
                                        <script>
                                            // Extract token from URL fragment
                                            var hash = window.location.hash.substring(1);
                                            var params = new URLSearchParams(hash);
                                            
                                            var accessToken = params.get('access_token');
                                            var state = params.get('state');
                                            
                                            if (accessToken && state === '{{expectedState}}') {
                                                // Redirect to success page with token in query string
                                                window.location.href = '/auth-callback?access_token=' + encodeURIComponent(accessToken) + '&state=' + encodeURIComponent(state);
                                            } else {
                                                document.body.innerHTML = '<h2 style="color:red;">Authentication failed!</h2><p>Invalid or missing token.</p>';
                                            }
                                        </script>
                                    </body>
                                    </html>
                                    """;

                    await using var writer = new StreamWriter(resp.OutputStream);
                    await writer.WriteAsync(html);
                }
                else if (req.Url?.AbsolutePath == "/auth-callback")
                {
                    var accessToken = req.QueryString["access_token"];
                    var state = req.QueryString["state"];
                    
                    if (!string.IsNullOrEmpty(accessToken) && state == expectedState)
                    {
                        authorization = new Models.Twitch.Authorization(accessToken);
                        resp.StatusCode = 302;
                        resp.RedirectLocation = "http://localhost:8080/success";
                        resp.Close();
                    }
                }
                else if (req.Url?.AbsolutePath.EndsWith("success") == true)
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
                            if (!listener.IsListening) return;
                            
                            listener.Stop();
                            listener.Close();
                        } catch { /**/ }
                    });
                }
            }
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