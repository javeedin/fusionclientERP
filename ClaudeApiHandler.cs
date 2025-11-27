using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WMSApp
{
    /// <summary>
    /// Handler for Claude AI API interactions
    /// </summary>
    public class ClaudeApiHandler
    {
        private readonly HttpClient _httpClient;
        private const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";
        private string _apiKey;

        public ClaudeApiHandler()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<ApiKeyTestResult> TestApiKeyAsync(string apiKey)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ClaudeApiHandler] Testing API key...");

                var request = new HttpRequestMessage(HttpMethod.Post, CLAUDE_API_URL);
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                var testPayload = new
                {
                    model = "claude-3-haiku-20240307",
                    max_tokens = 10,
                    messages = new[]
                    {
                        new { role = "user", content = "Hi" }
                    }
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(testPayload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _apiKey = apiKey;
                    return new ApiKeyTestResult
                    {
                        Success = true,
                        Message = "API key is valid"
                    };
                }

                return new ApiKeyTestResult
                {
                    Success = false,
                    Message = $"API key validation failed: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClaudeApiHandler] Test Error: {ex.Message}");
                return new ApiKeyTestResult
                {
                    Success = false,
                    Message = $"Error testing API key: {ex.Message}"
                };
            }
        }

        public async Task<ClaudeQueryResult> QueryClaudeAsync(
            string prompt,
            string systemPrompt = null,
            string model = "claude-3-haiku-20240307",
            int maxTokens = 4096)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                {
                    return new ClaudeQueryResult
                    {
                        Success = false,
                        Message = "API key not set"
                    };
                }

                System.Diagnostics.Debug.WriteLine($"[ClaudeApiHandler] Querying Claude...");

                var request = new HttpRequestMessage(HttpMethod.Post, CLAUDE_API_URL);
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                var payload = new
                {
                    model = model,
                    max_tokens = maxTokens,
                    system = systemPrompt ?? "You are a helpful assistant.",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var content = doc.RootElement
                        .GetProperty("content")[0]
                        .GetProperty("text")
                        .GetString();

                    return new ClaudeQueryResult
                    {
                        Success = true,
                        Response = content,
                        ResponseJson = content,
                        Model = model
                    };
                }

                return new ClaudeQueryResult
                {
                    Success = false,
                    Message = $"Query failed: {response.StatusCode}",
                    Error = $"Query failed: {response.StatusCode}",
                    Response = responseContent
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClaudeApiHandler] Query Error: {ex.Message}");
                return new ClaudeQueryResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Error = ex.Message
                };
            }
        }

        // Overload with apiKey, userQuery, systemPrompt, dataJson parameters
        public async Task<ClaudeQueryResult> QueryClaudeAsync(
            string apiKey,
            string userQuery,
            string systemPrompt,
            string dataJson)
        {
            _apiKey = apiKey;

            string fullPrompt = userQuery;
            if (!string.IsNullOrEmpty(dataJson))
            {
                fullPrompt = $"{userQuery}\n\nData:\n{dataJson}";
            }

            return await QueryClaudeAsync(fullPrompt, systemPrompt);
        }
    }

    public class ApiKeyTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ClaudeQueryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Response { get; set; }
        public string ResponseJson { get; set; }
        public string Error { get; set; }
        public string Model { get; set; }
    }
}
