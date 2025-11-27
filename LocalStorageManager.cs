using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WMSApp.PrintManagement
{
    /// <summary>
    /// Manages local storage for printer configuration and print jobs
    /// </summary>
    public class LocalStorageManager
    {
        private readonly string _configPath;
        private readonly string _printJobsPath;
        private readonly string _instanceSettingPath;

        public LocalStorageManager()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FusionClientERP");

            Directory.CreateDirectory(appDataPath);

            _configPath = Path.Combine(appDataPath, "printer-config.json");
            _printJobsPath = Path.Combine(appDataPath, "print-jobs.json");
            _instanceSettingPath = Path.Combine(appDataPath, "instance-setting.json");
        }

        public PrinterConfig LoadPrinterConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<PrinterConfig>(json);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalStorageManager] Error loading config: {ex.Message}");
            }
            return null;
        }

        public bool SavePrinterConfig(PrinterConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalStorageManager] Error saving config: {ex.Message}");
                return false;
            }
        }

        public bool SaveInstanceSetting(string instance)
        {
            try
            {
                var setting = new { Instance = instance, UpdatedAt = DateTime.UtcNow };
                string json = JsonSerializer.Serialize(setting, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_instanceSettingPath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalStorageManager] Error saving instance: {ex.Message}");
                return false;
            }
        }

        public string GetInstanceSetting()
        {
            try
            {
                if (File.Exists(_instanceSettingPath))
                {
                    string json = File.ReadAllText(_instanceSettingPath);
                    using var doc = JsonDocument.Parse(json);
                    return doc.RootElement.GetProperty("Instance").GetString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalStorageManager] Error loading instance: {ex.Message}");
            }
            return null;
        }

        public (string Username, string Password) GetFusionCredentials()
        {
            var config = LoadPrinterConfig();
            if (config != null)
            {
                return (config.FusionUsername, config.FusionPassword);
            }
            return (null, null);
        }

        public List<PrintJob> GetTripPrintJobs(string startDate, string tripId)
        {
            var allJobs = LoadPrintJobsList();
            return allJobs.FindAll(j => j.TripDate == startDate && j.TripId == tripId);
        }

        public List<PrintJob> GetAllPrintJobs(DateTime? startDate, DateTime? endDate)
        {
            var allJobs = LoadPrintJobsList();
            if (startDate == null && endDate == null)
                return allJobs;

            return allJobs.FindAll(j =>
            {
                if (string.IsNullOrEmpty(j.TripDate)) return false;
                var jobDate = DateTime.Parse(j.TripDate);
                bool afterStart = startDate == null || jobDate >= startDate.Value;
                bool beforeEnd = endDate == null || jobDate <= endDate.Value;
                return afterStart && beforeEnd;
            });
        }

        public PrintJobStats GetPrintJobStats()
        {
            var jobs = LoadPrintJobsList();
            return new PrintJobStats
            {
                TotalJobs = jobs.Count,
                CompletedJobs = jobs.FindAll(j => j.Status == PrintJobStatus.Completed || j.Status == PrintJobStatus.Printed).Count,
                PendingJobs = jobs.FindAll(j => j.Status == PrintJobStatus.Pending).Count,
                FailedJobs = jobs.FindAll(j => j.Status == PrintJobStatus.Failed).Count,
                PendingDownload = jobs.FindAll(j => j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Downloading).Count,
                DownloadCompleted = jobs.FindAll(j => j.Status == PrintJobStatus.Completed || j.Status == PrintJobStatus.Printed).Count
            };
        }

        private List<PrintJob> LoadPrintJobsList()
        {
            try
            {
                if (File.Exists(_printJobsPath))
                {
                    string json = File.ReadAllText(_printJobsPath);
                    return JsonSerializer.Deserialize<List<PrintJob>>(json) ?? new List<PrintJob>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalStorageManager] Error loading print jobs: {ex.Message}");
            }
            return new List<PrintJob>();
        }

        private List<PrintJobInfo> LoadPrintJobs()
        {
            try
            {
                if (File.Exists(_printJobsPath))
                {
                    string json = File.ReadAllText(_printJobsPath);
                    return JsonSerializer.Deserialize<List<PrintJobInfo>>(json) ?? new List<PrintJobInfo>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalStorageManager] Error loading jobs: {ex.Message}");
            }
            return new List<PrintJobInfo>();
        }

        public void SavePrintJobs(List<PrintJobInfo> jobs)
        {
            try
            {
                string json = JsonSerializer.Serialize(jobs, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_printJobsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LocalStorageManager] Error saving jobs: {ex.Message}");
            }
        }
    }

    public enum PrintJobStatus
    {
        Pending,
        Downloading,
        Completed,
        Printing,
        Printed,
        Failed
    }

    public class PrintJobInfo
    {
        public string JobId { get; set; }
        public string OrderNumber { get; set; }
        public string TripId { get; set; }
        public string TripDate { get; set; }
        public PrintJobStatus Status { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PrintedAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PrintJob
    {
        public string OrderNumber { get; set; }
        public string TripId { get; set; }
        public string TripDate { get; set; }
        public PrintJobStatus Status { get; set; }
        public string DownloadStatus { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ErrorMessage { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }
    }

    public class PrintJobStats
    {
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int PendingJobs { get; set; }
        public int FailedJobs { get; set; }
        public int PendingDownload { get; set; }
        public int DownloadCompleted { get; set; }
    }
}
