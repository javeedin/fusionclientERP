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

    /// <summary>
    /// Message class for testing Claude API key
    /// </summary>
    public class ClaudeApiTestMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; }
    }

    /// <summary>
    /// Message class for Claude API requests
    /// </summary>
    public class ClaudeApiRequestMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; }

        [JsonPropertyName("userQuery")]
        public string UserQuery { get; set; }

        [JsonPropertyName("systemPrompt")]
        public string SystemPrompt { get; set; }

        [JsonPropertyName("dataJson")]
        public string DataJson { get; set; }
    }

    /// <summary>
    /// Message class for toggling auto-print
    /// </summary>
    public class ToggleAutoPrintMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("tripDate")]
        public string TripDate { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("orders")]
        public System.Collections.Generic.List<OrderInfo> Orders { get; set; }
    }

    /// <summary>
    /// Order info for auto-print
    /// </summary>
    public class OrderInfo
    {
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; }

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; }
    }

    /// <summary>
    /// Message class for downloading PDF
    /// </summary>
    public class DownloadPdfMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("tripDate")]
        public string TripDate { get; set; }
    }

    /// <summary>
    /// Message class for checking if PDF exists
    /// </summary>
    public class CheckPdfExistsMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("tripDate")]
        public string TripDate { get; set; }
    }

    /// <summary>
    /// Message class for printing PDF
    /// </summary>
    public class PrintPdfMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("tripDate")]
        public string TripDate { get; set; }

        [JsonPropertyName("printerName")]
        public string PrinterName { get; set; }

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; }
    }

    /// <summary>
    /// Message class for printing sales order
    /// </summary>
    public class PrintSalesOrderMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("instance")]
        public string Instance { get; set; }

        [JsonPropertyName("reportPath")]
        public string ReportPath { get; set; }

        [JsonPropertyName("parameterName")]
        public string ParameterName { get; set; }

        [JsonPropertyName("tripId")]
        public string TripId { get; set; }

        [JsonPropertyName("tripDate")]
        public string TripDate { get; set; }

        [JsonPropertyName("orderType")]
        public string OrderType { get; set; }
    }
}
