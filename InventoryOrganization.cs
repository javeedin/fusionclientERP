using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WMSApp
{
    /// <summary>
    /// Represents an Inventory Organization from Oracle Fusion
    /// </summary>
    public class InventoryOrganization
    {
        [JsonPropertyName("organization_id")]
        public long OrganizationId { get; set; }

        [JsonPropertyName("organization_name")]
        public string OrganizationName { get; set; }

        [JsonPropertyName("organization_code")]
        public string OrganizationCode { get; set; }

        [JsonPropertyName("classification_code")]
        public string ClassificationCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("master_organization_id")]
        public long MasterOrganizationId { get; set; }

        public override string ToString()
        {
            return $"{OrganizationName} ({OrganizationCode})";
        }
    }

    /// <summary>
    /// Response wrapper for inventory organizations API
    /// </summary>
    public class InventoryOrganizationsResponse
    {
        [JsonPropertyName("items")]
        public List<InventoryOrganization> Items { get; set; } = new List<InventoryOrganization>();
    }

    /// <summary>
    /// Global session manager for storing organization and user context
    /// </summary>
    public static class SessionManager
    {
        // User Session
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string InstanceName { get; set; }
        public static string LoginDateTime { get; set; }
        public static bool IsLoggedIn { get; set; }

        // Selected Organization
        public static InventoryOrganization SelectedOrganization { get; set; }

        // Available Organizations
        public static List<InventoryOrganization> AvailableOrganizations { get; set; } = new List<InventoryOrganization>();

        /// <summary>
        /// Check if an organization is selected
        /// </summary>
        public static bool HasOrganization => SelectedOrganization != null;

        /// <summary>
        /// Get selected organization display name
        /// </summary>
        public static string OrganizationDisplayName => SelectedOrganization?.OrganizationName ?? "No Organization";

        /// <summary>
        /// Get selected organization code
        /// </summary>
        public static string OrganizationCode => SelectedOrganization?.OrganizationCode ?? "";

        /// <summary>
        /// Get selected organization ID
        /// </summary>
        public static long OrganizationId => SelectedOrganization?.OrganizationId ?? 0;

        /// <summary>
        /// Clear the session (logout)
        /// </summary>
        public static void Clear()
        {
            Username = null;
            Password = null;
            InstanceName = null;
            LoginDateTime = null;
            IsLoggedIn = false;
            SelectedOrganization = null;
            AvailableOrganizations.Clear();
        }

        /// <summary>
        /// Set the selected organization
        /// </summary>
        public static void SetOrganization(InventoryOrganization org)
        {
            SelectedOrganization = org;
            System.Diagnostics.Debug.WriteLine($"[SessionManager] Organization set: {org?.OrganizationName} ({org?.OrganizationCode})");
        }
    }
}
