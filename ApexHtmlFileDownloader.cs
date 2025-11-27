using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WMSApp
{
    /// <summary>
    /// Downloads and caches HTML files from Oracle APEX
    /// </summary>
    public class ApexHtmlFileDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly string _cacheDirectory;

        public ApexHtmlFileDownloader()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);

            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FusionClientERP",
                "HtmlCache");

            Directory.CreateDirectory(_cacheDirectory);
        }

        public async Task<string> DownloadApexHtmlFileAsync(string apexUrl, string localFileName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ApexHtmlFileDownloader] Downloading: {apexUrl}");

                // Check cache first
                string cachedPath = Path.Combine(_cacheDirectory, localFileName);
                if (File.Exists(cachedPath))
                {
                    var fileInfo = new FileInfo(cachedPath);
                    // Use cache if less than 1 hour old
                    if (DateTime.Now - fileInfo.LastWriteTime < TimeSpan.FromHours(1))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ApexHtmlFileDownloader] Using cached file: {cachedPath}");
                        return $"file:///{cachedPath.Replace("\\", "/")}";
                    }
                }

                // Download fresh copy
                string htmlContent = await _httpClient.GetStringAsync(apexUrl);

                // Save to cache
                await File.WriteAllTextAsync(cachedPath, htmlContent);
                System.Diagnostics.Debug.WriteLine($"[ApexHtmlFileDownloader] Cached to: {cachedPath}");

                return $"file:///{cachedPath.Replace("\\", "/")}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApexHtmlFileDownloader] Error: {ex.Message}");
                throw;
            }
        }

        public void ClearCache()
        {
            try
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    foreach (var file in Directory.GetFiles(_cacheDirectory))
                    {
                        File.Delete(file);
                    }
                    System.Diagnostics.Debug.WriteLine($"[ApexHtmlFileDownloader] Cache cleared");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApexHtmlFileDownloader] Clear cache error: {ex.Message}");
            }
        }

        public long GetCacheSize()
        {
            long totalSize = 0;
            try
            {
                if (Directory.Exists(_cacheDirectory))
                {
                    foreach (var file in Directory.GetFiles(_cacheDirectory))
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                }
            }
            catch { }
            return totalSize;
        }
    }
}
