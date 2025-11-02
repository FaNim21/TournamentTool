using System.Net;
using Authorization = TournamentTool.Domain.Entities.Twitch.Authorization;

namespace TournamentTool.Core.Utils;

public class WebServer
{
    private readonly HttpListener listener;
    private readonly string expectedState;

    public WebServer(string uri, string state)
    {
        expectedState = state;
        
        listener = new HttpListener();
        listener.Prefixes.Add(uri.EndsWith('/') ? uri : uri + "/");
    }

    public async Task<Authorization?> Listen()
    {
        listener.Start();
        
        Authorization? authorization = null;
        
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

            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            
            try
            {
                if (request.Url?.AbsolutePath == "/")
                {
                    await StartupPage(response);
                }
                else if (request.Url?.AbsolutePath == "/auth-callback")
                {
                    authorization = GetAuthorization(response, request);
                }
                else if (request.Url?.AbsolutePath.EndsWith("success") == true)
                {
                    await SuccessConnection(response);
                }
            }
            catch (Exception)
            {
                response.StatusCode = 500;
                await using var w = new StreamWriter(response.OutputStream);
                await w.WriteAsync("<html><body><h2>Internal error.</h2></body></html>");
                throw;
            }
            finally
            {
                response.Close();
            }
        }

        return authorization;
    }

    private async Task StartupPage(HttpListenerResponse response)
    {
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=UTF-8";
                    
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

        await using var writer = new StreamWriter(response.OutputStream);
        await writer.WriteAsync(html);
    }
    private Authorization GetAuthorization(HttpListenerResponse response, HttpListenerRequest request)
    {
        string? accessToken = request.QueryString["access_token"];
        string? state = request.QueryString["state"];
        Authorization authorization = new Authorization(string.Empty);
                    
        if (!string.IsNullOrEmpty(accessToken) && state == expectedState)
        {
            authorization = new Authorization(accessToken);
            response.StatusCode = 302;
            response.RedirectLocation = "http://localhost:8080/success";
            response.Close();
        }

        return authorization;
    }
    private async Task SuccessConnection(HttpListenerResponse response)
    {
        response.StatusCode = 200;
        response.ContentType = "text/html; charset=UTF-8";
    
        const string html = "<!DOCTYPE html><html><head><meta charset='utf-8' />" +
                            "<title>Twitch Auth</title></head>" +
                            "<body style='font-family:sans-serif; text-align:center; margin-top:50px; color:green;'>" +
                            "<h2>Authorization successful</h2>" +
                            "<p>You can now close this window and return to the app.</p>" +
                            "</body></html>";

        await using var writer = new StreamWriter(response.OutputStream);
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