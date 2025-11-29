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
        private static List<EndpointConfig> _cachedEndpoints = null;
        private static string _resolvedSettingsPath = null;

        /// <summary>
        /// Gets the list of possible settings paths to search
        /// </summary>
        private static List<string> GetPossibleSettingsPaths()
        {
            var paths = new List<string>();

            // 1. Standard deployment path
            paths.Add(@"C:\fusionclient\ERP\settings");

            // 2. Relative to application directory
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            paths.Add(Path.Combine(appDir, "ERP", "settings"));
            paths.Add(Path.Combine(appDir, "settings"));

            // 3. Parent directory (for development)
            string parentDir = Directory.GetParent(appDir)?.FullName;
            if (!string.IsNullOrEmpty(parentDir))
            {
                paths.Add(Path.Combine(parentDir, "ERP", "settings"));
            }

            // 4. Two levels up (for bin/Debug/net scenarios)
            string grandParentDir = Directory.GetParent(parentDir ?? appDir)?.FullName;
            if (!string.IsNullOrEmpty(grandParentDir))
            {
                paths.Add(Path.Combine(grandParentDir, "ERP", "settings"));

                // Go even further up for bin/Debug/net8.0-windows scenarios
                string greatGrandParentDir = Directory.GetParent(grandParentDir)?.FullName;
                if (!string.IsNullOrEmpty(greatGrandParentDir))
                {
                    paths.Add(Path.Combine(greatGrandParentDir, "ERP", "settings"));

                    string ggGrandParentDir = Directory.GetParent(greatGrandParentDir)?.FullName;
                    if (!string.IsNullOrEmpty(ggGrandParentDir))
                    {
                        paths.Add(Path.Combine(ggGrandParentDir, "ERP", "settings"));
                    }
                }
            }

            return paths;
        }

        /// <summary>
        /// Finds the settings path that contains the endpoints file
        /// </summary>
        private static string FindSettingsPath()
        {
            if (_resolvedSettingsPath != null)
                return _resolvedSettingsPath;

            foreach (var path in GetPossibleSettingsPaths())
            {
                string xmlPath = Path.Combine(path, "endpoints.xml");
                string csvPath = Path.Combine(path, "endpoints.csv");

                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Checking path: {path}");

                if (File.Exists(xmlPath) || File.Exists(csvPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Found settings at: {path}");
                    _resolvedSettingsPath = path;
                    return path;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] No settings file found in any location");
            return null;
        }

        /// <summary>
        /// Loads all endpoints from the settings file
        /// </summary>
        public static List<EndpointConfig> LoadEndpoints(string settingsPath = null)
        {
            if (_cachedEndpoints != null)
                return _cachedEndpoints;

            // Find the settings path if not provided
            settingsPath = settingsPath ?? FindSettingsPath();

            if (string.IsNullOrEmpty(settingsPath))
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] No settings path found");
                _cachedEndpoints = new List<EndpointConfig>();
                return _cachedEndpoints;
            }

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
            _resolvedSettingsPath = null;
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

        /// <summary>
        /// Saves endpoints to the XML file
        /// </summary>
        public static void SaveEndpoints(List<EndpointConfig> endpoints, string settingsPath = null)
        {
            settingsPath = settingsPath ?? GetSettingsPath();
            string xmlPath = Path.Combine(settingsPath, "endpoints.xml");

            try
            {
                // Ensure directory exists
                if (!Directory.Exists(settingsPath))
                {
                    Directory.CreateDirectory(settingsPath);
                }

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = System.Text.Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(xmlPath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Endpoints");

                    foreach (var ep in endpoints)
                    {
                        writer.WriteStartElement("Endpoint");
                        writer.WriteElementString("Sno", ep.Sno.ToString());
                        writer.WriteElementString("Source", ep.Source ?? "");
                        writer.WriteElementString("IntegrationCode", ep.IntegrationCode ?? "");
                        writer.WriteElementString("InstanceName", ep.InstanceName ?? "");
                        writer.WriteElementString("URL", ep.BaseUrl ?? "");
                        writer.WriteElementString("Path", ep.Endpoint ?? "");
                        writer.WriteElementString("Comments", ep.Comments ?? "");
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

                // Also save CSV version
                SaveEndpointsToCsv(endpoints, settingsPath);

                // Clear cache so next load gets fresh data
                ClearCache();

                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Saved {endpoints.Count} endpoints to {xmlPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Error saving endpoints: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves endpoints to CSV file
        /// </summary>
        private static void SaveEndpointsToCsv(List<EndpointConfig> endpoints, string settingsPath)
        {
            string csvPath = Path.Combine(settingsPath, "endpoints.csv");

            var lines = new List<string>
            {
                "Sno,Source,IntegrationCode,InstanceName,URL,Endpoint,Comments"
            };

            foreach (var ep in endpoints)
            {
                string line = string.Join(",",
                    ep.Sno,
                    EscapeCsvField(ep.Source),
                    EscapeCsvField(ep.IntegrationCode),
                    EscapeCsvField(ep.InstanceName),
                    EscapeCsvField(ep.BaseUrl),
                    EscapeCsvField(ep.Endpoint),
                    EscapeCsvField(ep.Comments)
                );
                lines.Add(line);
            }

            File.WriteAllLines(csvPath, lines);
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        /// <summary>
        /// Gets the settings path
        /// </summary>
        public static string GetSettingsPath()
        {
            return FindSettingsPath() ?? @"C:\fusionclient\ERP\settings";
        }

        private static List<EndpointConfig> LoadFromXml(string xmlPath)
        {
            var endpoints = new List<EndpointConfig>();

            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlPath);

                // Use specific XPath to only select direct Endpoint children of Endpoints root
                // This avoids selecting the inner <Endpoint> (path) child elements
                var nodes = doc.SelectNodes("/Endpoints/Endpoint");
                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        // Read the endpoint path from either <Path> or <Endpoint> child element
                        string endpointPath = node.SelectSingleNode("Path")?.InnerText
                            ?? node.SelectSingleNode("Endpoint")?.InnerText
                            ?? "";

                        var endpoint = new EndpointConfig
                        {
                            Sno = int.TryParse(node.SelectSingleNode("Sno")?.InnerText, out int sno) ? sno : 0,
                            Source = node.SelectSingleNode("Source")?.InnerText ?? "",
                            IntegrationCode = node.SelectSingleNode("IntegrationCode")?.InnerText ?? "",
                            InstanceName = node.SelectSingleNode("InstanceName")?.InnerText ?? "",
                            BaseUrl = node.SelectSingleNode("URL")?.InnerText ?? "",
                            Endpoint = endpointPath,
                            Comments = node.SelectSingleNode("Comments")?.InnerText ?? ""
                        };

                        // Only add valid endpoints (must have at least IntegrationCode or URL)
                        if (!string.IsNullOrWhiteSpace(endpoint.IntegrationCode) ||
                            !string.IsNullOrWhiteSpace(endpoint.BaseUrl))
                        {
                            endpoints.Add(endpoint);
                            System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Loaded: {endpoint.IntegrationCode} - {endpoint.InstanceName}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Skipped invalid/empty endpoint node");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[EndpointConfigReader] Loaded {endpoints.Count} valid endpoints from XML");
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
