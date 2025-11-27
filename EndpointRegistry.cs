using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WMSApp
{
    /// <summary>
    /// Registry for managing API endpoint URLs
    /// </summary>
    public static class EndpointRegistry
    {
        private static Dictionary<string, Dictionary<string, string>> _endpoints;
        private static readonly string _configPath;

        static EndpointRegistry()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FusionClientERP",
                "endpoints.json");

            LoadEndpoints();
        }

        private static void LoadEndpoints()
        {
            _endpoints = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    _endpoints = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                        ?? new Dictionary<string, Dictionary<string, string>>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointRegistry] Error loading endpoints: {ex.Message}");
            }

            // Add default endpoints if not loaded
            if (!_endpoints.ContainsKey("WMS"))
            {
                _endpoints["WMS"] = new Dictionary<string, string>
                {
                    { "GETPRINTERCONFIG", "/ords/wms/printerconfig/active" },
                    { "GETORDERS", "/ords/wms/orders" },
                    { "GETTRIPS", "/ords/wms/trips" }
                };
            }
        }

        public static string GetEndpointUrl(string module, string endpoint)
        {
            try
            {
                if (_endpoints.TryGetValue(module, out var moduleEndpoints))
                {
                    if (moduleEndpoints.TryGetValue(endpoint, out var url))
                    {
                        return url;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointRegistry] Error getting endpoint: {ex.Message}");
            }

            return null;
        }

        public static void SetEndpointUrl(string module, string endpoint, string url)
        {
            if (!_endpoints.ContainsKey(module))
            {
                _endpoints[module] = new Dictionary<string, string>();
            }

            _endpoints[module][endpoint] = url;
            SaveEndpoints();
        }

        public static Dictionary<string, string> GetModuleEndpoints(string module)
        {
            if (_endpoints.TryGetValue(module, out var moduleEndpoints))
            {
                return new Dictionary<string, string>(moduleEndpoints);
            }
            return new Dictionary<string, string>();
        }

        private static void SaveEndpoints()
        {
            try
            {
                string directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(_endpoints, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointRegistry] Error saving endpoints: {ex.Message}");
            }
        }
    }
}
