using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace WMSApp
{
    /// <summary>
    /// HTTP client for REST API operations
    /// </summary>
    public class RestApiClient
    {
        private readonly HttpClient _httpClient;

        public RestApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> ExecuteGetAsync(string url)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] GET: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] Response received: {content.Length} chars");

                return content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] GET Error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ExecuteGetAsync(string url, string username, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] GET (auth): {url}");

                var request = new HttpRequestMessage(HttpMethod.Get, url);

                // Add Basic Auth header
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] GET (auth) Error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ExecutePostAsync(string url, string jsonBody)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] POST: {url}");

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] POST Error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ExecutePostAsync(string url, string jsonBody, string username, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] POST (auth): {url}");

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Add Basic Auth header
                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestApiClient] POST (auth) Error: {ex.Message}");
                throw;
            }
        }
    }
}
