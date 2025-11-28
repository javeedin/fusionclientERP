using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace WMSApp
{
    /// <summary>
    /// Service for fetching Inventory Organizations from Oracle Fusion
    /// </summary>
    public class InventoryOrganizationService
    {
        private const string SETTINGS_PATH = @"C:\fusionclient\ERP\settings\endpoints.xml";
        private const string INTEGRATION_CODE = "INVENTORY_ORGS";

        /// <summary>
        /// Fetch inventory organizations from the webservice
        /// </summary>
        public async Task<List<InventoryOrganization>> GetOrganizationsAsync(string username, string instanceName)
        {
            try
            {
                string endpointUrl = GetEndpointUrl(instanceName);

                if (string.IsNullOrEmpty(endpointUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] No endpoint found for {INTEGRATION_CODE} - {instanceName}");
                    return new List<InventoryOrganization>();
                }

                // Build the request URL with username parameter
                string requestUrl = $"{endpointUrl}?username={Uri.EscapeDataString(username)}";

                System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] Fetching organizations from: {requestUrl}");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var response = await client.GetAsync(requestUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] Response: {jsonResponse}");

                        var result = JsonSerializer.Deserialize<InventoryOrganizationsResponse>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (result?.Items != null && result.Items.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] Loaded {result.Items.Count} organizations");
                            return result.Items;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] HTTP Error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] Error: {ex.Message}");
            }

            return new List<InventoryOrganization>();
        }

        /// <summary>
        /// Get endpoint URL from settings for INVENTORY_ORGS integration code
        /// </summary>
        private string GetEndpointUrl(string instanceName)
        {
            try
            {
                if (!File.Exists(SETTINGS_PATH))
                {
                    System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] Settings file not found: {SETTINGS_PATH}");
                    return null;
                }

                var doc = new XmlDocument();
                doc.Load(SETTINGS_PATH);

                // Look for APEX endpoint with INVENTORY_ORGS integration code
                var endpoints = doc.SelectNodes("/Endpoints/Endpoint");
                if (endpoints != null)
                {
                    foreach (XmlNode endpoint in endpoints)
                    {
                        string source = endpoint.SelectSingleNode("Source")?.InnerText ?? "";
                        string integrationCode = endpoint.SelectSingleNode("IntegrationCode")?.InnerText ?? "";
                        string instance = endpoint.SelectSingleNode("InstanceName")?.InnerText ?? "";
                        string url = endpoint.SelectSingleNode("URL")?.InnerText ?? "";
                        string path = endpoint.SelectSingleNode("Path")?.InnerText
                            ?? endpoint.SelectSingleNode("Endpoint")?.InnerText
                            ?? "";

                        if (source.Equals("APEX", StringComparison.OrdinalIgnoreCase) &&
                            integrationCode.Equals(INTEGRATION_CODE, StringComparison.OrdinalIgnoreCase) &&
                            instance.Equals(instanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            string fullUrl = url.TrimEnd('/') + path;
                            System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] Found endpoint: {fullUrl}");
                            return fullUrl;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] No endpoint found for {INTEGRATION_CODE} - {instanceName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InventoryOrgService] Error loading endpoints: {ex.Message}");
            }

            return null;
        }
    }
}
