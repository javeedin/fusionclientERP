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

        public List<PrintJobInfo> GetTripPrintJobs(string startDate, string tripId)
        {
            var allJobs = LoadPrintJobs();
            return allJobs.FindAll(j => j.TripDate == startDate && j.TripId == tripId);
        }

        public List<PrintJobInfo> GetAllPrintJobs(string startDate, string endDate)
        {
            var allJobs = LoadPrintJobs();
            return allJobs.FindAll(j =>
                string.Compare(j.TripDate, startDate) >= 0 &&
                string.Compare(j.TripDate, endDate) <= 0);
        }

        public PrintJobStats GetPrintJobStats()
        {
            var jobs = LoadPrintJobs();
            return new PrintJobStats
            {
                TotalJobs = jobs.Count,
                CompletedJobs = jobs.FindAll(j => j.Status == "Completed").Count,
                PendingJobs = jobs.FindAll(j => j.Status == "Pending").Count,
                FailedJobs = jobs.FindAll(j => j.Status == "Failed").Count
            };
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

    public class PrintJobInfo
    {
        public string JobId { get; set; }
        public string OrderNumber { get; set; }
        public string TripId { get; set; }
        public string TripDate { get; set; }
        public string Status { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PrintedAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PrintJobStats
    {
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int PendingJobs { get; set; }
        public int FailedJobs { get; set; }
    }
}
