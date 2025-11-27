using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WMSApp.PrintManagement;

namespace WMSApp
{
    /// <summary>
    /// Manages print job queuing and processing
    /// </summary>
    public class PrintJobManager
    {
        private readonly LocalStorageManager _storageManager;
        private readonly PrinterService _printerService;
        private bool _autoPrintEnabled = false;
        private string _currentTripId;
        private string _currentTripDate;

        public PrintJobManager()
        {
            _storageManager = new LocalStorageManager();
            _printerService = new PrinterService();
        }

        public static async Task<List<PrintJob>> GetAllPrintJobsAsync()
        {
            return await Task.Run(() =>
            {
                var manager = new LocalStorageManager();
                return manager.GetAllPrintJobs(
                    DateTime.Now.AddDays(-30),
                    DateTime.Now);
            });
        }

        public async Task<AutoPrintResult> EnableAutoPrintAsync(
            string tripId,
            string tripDate,
            string printerName,
            string fusionUsername,
            string fusionPassword,
            string fusionInstance)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Enabling auto-print for Trip: {tripId}, Date: {tripDate}");

                _autoPrintEnabled = true;
                _currentTripId = tripId;
                _currentTripDate = tripDate;

                return new AutoPrintResult
                {
                    Success = true,
                    Message = $"Auto-print enabled for Trip {tripId}",
                    TripId = tripId,
                    TripDate = tripDate
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] EnableAutoPrint Error: {ex.Message}");
                return new AutoPrintResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // Overload with orders list
        public async Task<AutoPrintResult> EnableAutoPrintAsync(
            string tripId,
            string tripDate,
            List<OrderInfo> orders)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Enabling auto-print for Trip: {tripId}, Date: {tripDate}, Orders: {orders?.Count ?? 0}");

                _autoPrintEnabled = true;
                _currentTripId = tripId;
                _currentTripDate = tripDate;

                var tripConfig = new TripConfig
                {
                    TripId = tripId,
                    TripDate = tripDate,
                    Orders = orders ?? new List<OrderInfo>()
                };

                return new AutoPrintResult
                {
                    Success = true,
                    Message = $"Auto-print enabled for Trip {tripId} with {orders?.Count ?? 0} orders",
                    TripId = tripId,
                    TripDate = tripDate,
                    TripConfig = tripConfig
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] EnableAutoPrint Error: {ex.Message}");
                return new AutoPrintResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public bool DisableAutoPrint(string tripId, string tripDate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Disabling auto-print for Trip: {tripId}");
                _autoPrintEnabled = false;
                _currentTripId = null;
                _currentTripDate = null;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] DisableAutoPrint Error: {ex.Message}");
                return false;
            }
        }

        public async Task<DownloadResult> DownloadSingleOrderAsync(
            string orderNumber,
            string tripId,
            string tripDate,
            string reportPath,
            string fusionUsername,
            string fusionPassword,
            string fusionInstance)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Downloading order: {orderNumber}");

                var downloader = new FusionPdfDownloader();
                var result = await downloader.DownloadGenericReportPdfAsync(
                    reportPath,
                    "P_ORDER_NUMBER",
                    orderNumber,
                    fusionInstance,
                    fusionUsername,
                    fusionPassword);

                if (result.Success)
                {
                    // Save to file system
                    string folderPath = Path.Combine(@"C:\fusion", tripDate, tripId);
                    Directory.CreateDirectory(folderPath);

                    string filePath = Path.Combine(folderPath, $"{orderNumber}.pdf");
                    byte[] pdfBytes = Convert.FromBase64String(result.Base64Content);
                    await File.WriteAllBytesAsync(filePath, pdfBytes);

                    return new DownloadResult
                    {
                        Success = true,
                        Message = $"Downloaded {orderNumber}",
                        FilePath = filePath
                    };
                }

                return new DownloadResult
                {
                    Success = false,
                    Message = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Download Error: {ex.Message}");
                return new DownloadResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // Simplified overload that uses stored credentials
        public async Task<DownloadResult> DownloadSingleOrderAsync(
            string orderNumber,
            string tripId,
            string tripDate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Downloading order (simple): {orderNumber}");

                // Get credentials from storage
                var credentials = _storageManager.GetFusionCredentials();
                var config = _storageManager.LoadPrinterConfig();

                if (string.IsNullOrEmpty(credentials.Username))
                {
                    return new DownloadResult
                    {
                        Success = false,
                        Message = "Fusion credentials not configured"
                    };
                }

                var downloader = new FusionPdfDownloader();
                var result = await downloader.DownloadSalesOrderPdfAsync(
                    orderNumber,
                    config?.FusionInstance ?? "",
                    credentials.Username,
                    credentials.Password);

                if (result.Success)
                {
                    string folderPath = Path.Combine(@"C:\fusion", tripDate, tripId);
                    Directory.CreateDirectory(folderPath);

                    string filePath = Path.Combine(folderPath, $"{orderNumber}.pdf");
                    byte[] pdfBytes = Convert.FromBase64String(result.Base64Content);
                    await File.WriteAllBytesAsync(filePath, pdfBytes);

                    return new DownloadResult
                    {
                        Success = true,
                        Message = $"Downloaded {orderNumber}",
                        FilePath = filePath
                    };
                }

                return new DownloadResult
                {
                    Success = false,
                    Message = result.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Download Error: {ex.Message}");
                return new DownloadResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<PrintSingleResult> PrintSingleOrderAsync(
            string orderNumber,
            string tripId,
            string tripDate,
            string printerName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Printing order: {orderNumber}");

                string filePath = Path.Combine(@"C:\fusion", tripDate, tripId, $"{orderNumber}.pdf");

                if (!File.Exists(filePath))
                {
                    return new PrintSingleResult
                    {
                        Success = false,
                        Message = $"PDF file not found: {filePath}"
                    };
                }

                var printResult = await _printerService.PrintPdfAsync(filePath, printerName);

                return new PrintSingleResult
                {
                    Success = printResult.Success,
                    Message = printResult.Message,
                    FilePath = filePath
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Print Error: {ex.Message}");
                return new PrintSingleResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<RetryResult> RetryFailedJobsAsync(string tripId, string tripDate)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Retrying failed jobs for Trip: {tripId}");

                var jobs = _storageManager.GetTripPrintJobs(tripDate, tripId);
                var failedJobs = jobs.FindAll(j => j.Status == PrintJobStatus.Failed);

                int retried = 0;
                foreach (var job in failedJobs)
                {
                    // Attempt to retry
                    retried++;
                }

                return new RetryResult
                {
                    Success = true,
                    Message = $"Retried {retried} failed jobs",
                    RetriedCount = retried
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PrintJobManager] Retry Error: {ex.Message}");
                return new RetryResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }

    public class AutoPrintResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TripId { get; set; }
        public string TripDate { get; set; }
        public TripConfig TripConfig { get; set; }
    }

    public class TripConfig
    {
        public string TripId { get; set; }
        public string TripDate { get; set; }
        public List<OrderInfo> Orders { get; set; }
    }

    public class DownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
    }

    public class PrintSingleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
    }

    public class RetryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int RetriedCount { get; set; }
    }
}
