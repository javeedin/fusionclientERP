using System.Text.Json.Serialization;
using WMSApp.PrintManagement;

namespace WMSApp
{
    /// <summary>
    /// Message class for printer configuration
    /// </summary>
    public class ConfigurePrinterMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("config")]
        public PrinterConfig Config { get; set; }
    }

    /// <summary>
    /// Message class for getting print jobs
    /// </summary>
    public class GetPrintJobsMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; }

        [JsonPropertyName("filter")]
        public string Filter { get; set; }
    }

    /// <summary>
    /// Message class for enabling auto-print
    /// </summary>
    public class EnableAutoPrintMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("tripDate")]
        public string TripDate { get; set; }

        [JsonPropertyName("printerName")]
        public string PrinterName { get; set; }

        [JsonPropertyName("fusionUsername")]
        public string FusionUsername { get; set; }

        [JsonPropertyName("fusionPassword")]
        public string FusionPassword { get; set; }

        [JsonPropertyName("fusionInstance")]
        public string FusionInstance { get; set; }
    }

    /// <summary>
    /// Message class for disabling auto-print
    /// </summary>
    public class DisableAutoPrintMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("tripDate")]
        public string TripDate { get; set; }
    }

    /// <summary>
    /// Message class for testing API key
    /// </summary>
    public class TestApiKeyMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; }
    }

    /// <summary>
    /// Message class for Claude AI queries
    /// </summary>
    public class ClaudeQueryMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; }

        [JsonPropertyName("systemPrompt")]
        public string SystemPrompt { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; }
    }
}
