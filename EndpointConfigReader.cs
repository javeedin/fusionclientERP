using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace WMSApp
{
    /// <summary>
    /// Configuration for a single endpoint
    /// </summary>
    public class EndpointConfig
    {
        public int Sno { get; set; }
        public string Source { get; set; }  // APEX or FUSION
        public string IntegrationCode { get; set; }  // LOGIN, WMS, GL, AR, AP, INV, OM, etc.
        public string InstanceName { get; set; }  // PROD, TEST, DEV
        public string BaseUrl { get; set; }
        public string Endpoint { get; set; }
        public string Comments { get; set; }

        public string FullUrl => $"{BaseUrl?.TrimEnd('/')}{Endpoint}";
    }

    /// <summary>
    /// Reads endpoint configurations from XML or CSV files
    /// </summary>
    public static class EndpointConfigReader
    {
        private static readonly string DefaultSettingsPath = @"C:\fusionclient\ERP\settings";
        private static List<EndpointConfig> _cachedEndpoints = null;

        /// <summary>
        /// Loads all endpoints from the settings file
        /// </summary>
        public static List<EndpointConfig> LoadEndpoints(string settingsPath = null)
        {
            if (_cachedEndpoints != null)
                return _cachedEndpoints;

            settingsPath = settingsPath ?? DefaultSettingsPath;

            // Try XML first, then CSV
            string xmlPath = Path.Combine(settingsPath, "endpoints.xml");
            string csvPath = Path.Combine(settingsPath, "endpoints.csv");

            if (File.Exists(xmlPath))
            {
                _cachedEndpoints = LoadFromXml(xmlPath);
            }
            else if (File.Exists(csvPath))
            {
                _cachedEndpoints = LoadFromCsv(csvPath);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] No settings file found at {settingsPath}");
                _cachedEndpoints = new List<EndpointConfig>();
            }

            return _cachedEndpoints;
        }

        /// <summary>
        /// Clears the cached endpoints to force reload
        /// </summary>
        public static void ClearCache()
        {
            _cachedEndpoints = null;
        }

        /// <summary>
        /// Gets endpoints by source (APEX or FUSION)
        /// </summary>
        public static List<EndpointConfig> GetBySource(string source)
        {
            var endpoints = LoadEndpoints();
            return endpoints.Where(e => e.Source.Equals(source, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Gets endpoints by integration code (LOGIN, WMS, GL, etc.)
        /// </summary>
        public static List<EndpointConfig> GetByIntegrationCode(string integrationCode)
        {
            var endpoints = LoadEndpoints();
            return endpoints.Where(e => e.IntegrationCode.Equals(integrationCode, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Gets a specific endpoint by source, integration code, and instance
        /// </summary>
        public static EndpointConfig GetEndpoint(string source, string integrationCode, string instanceName)
        {
            var endpoints = LoadEndpoints();
            return endpoints.FirstOrDefault(e =>
                e.Source.Equals(source, StringComparison.OrdinalIgnoreCase) &&
                e.IntegrationCode.Equals(integrationCode, StringComparison.OrdinalIgnoreCase) &&
                e.InstanceName.Equals(instanceName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the full URL for an endpoint
        /// </summary>
        public static string GetEndpointUrl(string source, string integrationCode, string instanceName)
        {
            var endpoint = GetEndpoint(source, integrationCode, instanceName);
            return endpoint?.FullUrl;
        }

        /// <summary>
        /// Gets all available instance names
        /// </summary>
        public static List<string> GetInstanceNames()
        {
            var endpoints = LoadEndpoints();
            return endpoints.Select(e => e.InstanceName).Distinct().ToList();
        }

        /// <summary>
        /// Gets all available module codes (integration codes)
        /// </summary>
        public static List<string> GetModuleCodes()
        {
            var endpoints = LoadEndpoints();
            return endpoints.Select(e => e.IntegrationCode).Distinct().ToList();
        }

        private static List<EndpointConfig> LoadFromXml(string xmlPath)
        {
            var endpoints = new List<EndpointConfig>();

            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlPath);

                var nodes = doc.SelectNodes("//Endpoint");
                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        var endpoint = new EndpointConfig
                        {
                            Sno = int.TryParse(node.SelectSingleNode("Sno")?.InnerText, out int sno) ? sno : 0,
                            Source = node.SelectSingleNode("Source")?.InnerText ?? "",
                            IntegrationCode = node.SelectSingleNode("IntegrationCode")?.InnerText ?? "",
                            InstanceName = node.SelectSingleNode("InstanceName")?.InnerText ?? "",
                            BaseUrl = node.SelectSingleNode("URL")?.InnerText ?? "",
                            Endpoint = node.SelectSingleNode("Endpoint")?.InnerText ?? "",
                            Comments = node.SelectSingleNode("Comments")?.InnerText ?? ""
                        };
                        endpoints.Add(endpoint);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Loaded {endpoints.Count} endpoints from XML");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Error loading XML: {ex.Message}");
            }

            return endpoints;
        }

        private static List<EndpointConfig> LoadFromCsv(string csvPath)
        {
            var endpoints = new List<EndpointConfig>();

            try
            {
                var lines = File.ReadAllLines(csvPath);
                bool isHeader = true;

                foreach (var line in lines)
                {
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    var parts = ParseCsvLine(line);
                    if (parts.Length >= 6)
                    {
                        var endpoint = new EndpointConfig
                        {
                            Sno = int.TryParse(parts[0], out int sno) ? sno : 0,
                            Source = parts[1],
                            IntegrationCode = parts[2],
                            InstanceName = parts[3],
                            BaseUrl = parts[4],
                            Endpoint = parts[5],
                            Comments = parts.Length > 6 ? parts[6] : ""
                        };
                        endpoints.Add(endpoint);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Loaded {endpoints.Count} endpoints from CSV");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Error loading CSV: {ex.Message}");
            }

            return endpoints;
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = "";

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current.Trim());

            return result.ToArray();
        }
    }
}
