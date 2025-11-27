using System;
using System.Collections.Generic;
using Microsoft.Web.WebView2.WinForms;

namespace WMSApp
{
    /// <summary>
    /// Routes messages between WebView2 and C# handlers
    /// </summary>
    public class WebViewMessageRouter
    {
        private readonly WebView2 _webView;
        private readonly Dictionary<string, Action<string, string>> _handlers;

        public WebViewMessageRouter(WebView2 webView)
        {
            _webView = webView;
            _handlers = new Dictionary<string, Action<string, string>>();
        }

        public void RegisterHandler(string action, Action<string, string> handler)
        {
            _handlers[action] = handler;
        }

        public void UnregisterHandler(string action)
        {
            _handlers.Remove(action);
        }

        public bool RouteMessage(string action, string messageJson, string requestId)
        {
            if (_handlers.TryGetValue(action, out var handler))
            {
                try
                {
                    handler(messageJson, requestId);
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[WebViewMessageRouter] Handler error for {action}: {ex.Message}");
                    return false;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[WebViewMessageRouter] No handler registered for action: {action}");
            return false;
        }

        public void SendMessage(string messageJson)
        {
            try
            {
                _webView?.CoreWebView2?.PostWebMessageAsJson(messageJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WebViewMessageRouter] Send error: {ex.Message}");
            }
        }

        public void SendResponse(string requestId, bool success, object data = null, string errorMessage = null)
        {
            var response = new
            {
                requestId = requestId,
                success = success,
                data = data,
                error = errorMessage
            };

            string json = System.Text.Json.JsonSerializer.Serialize(response);
            SendMessage(json);
        }
    }
}
