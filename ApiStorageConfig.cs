using System;
using System.IO;
using System.Xml;

namespace WMSApp
{
    /// <summary>
    /// Configuration for API Storage endpoint - used to store endpoints and instances remotely
    /// </summary>
    public class ApiStorageConfig
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool NoAuthentication { get; set; }

        private static readonly string ConfigFileName = "api-storage-config.xml";
        private static ApiStorageConfig _instance;

        /// <summary>
        /// Gets the singleton instance of the API storage configuration
        /// </summary>
        public static ApiStorageConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets the full path to the config file
        /// </summary>
        public static string GetConfigPath()
        {
            string settingsPath = @"C:\fusionclient\ERP\settings";
            return Path.Combine(settingsPath, ConfigFileName);
        }

        /// <summary>
        /// Loads the API storage configuration from file
        /// </summary>
        public static ApiStorageConfig Load()
        {
            var config = new ApiStorageConfig();
            string configPath = GetConfigPath();

            try
            {
                if (File.Exists(configPath))
                {
                    var doc = new XmlDocument();
                    doc.Load(configPath);

                    config.Url = doc.SelectSingleNode("/ApiStorageConfig/Url")?.InnerText ?? "";
                    config.Username = doc.SelectSingleNode("/ApiStorageConfig/Username")?.InnerText ?? "";
                    config.Password = doc.SelectSingleNode("/ApiStorageConfig/Password")?.InnerText ?? "";
                    config.NoAuthentication = bool.TryParse(
                        doc.SelectSingleNode("/ApiStorageConfig/NoAuthentication")?.InnerText,
                        out bool noAuth) && noAuth;

                    System.Diagnostics.Debug.WriteLine($"[ApiStorageConfig] Loaded from: {configPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ApiStorageConfig] Config file not found: {configPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiStorageConfig] Error loading config: {ex.Message}");
            }

            return config;
        }

        /// <summary>
        /// Saves the API storage configuration to file
        /// </summary>
        public void Save()
        {
            string configPath = GetConfigPath();

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    Encoding = System.Text.Encoding.UTF8
                };

                using (var writer = XmlWriter.Create(configPath, settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("ApiStorageConfig");

                    writer.WriteElementString("Url", Url ?? "");
                    writer.WriteElementString("Username", Username ?? "");
                    writer.WriteElementString("Password", Password ?? "");
                    writer.WriteElementString("NoAuthentication", NoAuthentication.ToString().ToLower());

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

                // Update singleton instance
                _instance = this;

                System.Diagnostics.Debug.WriteLine($"[ApiStorageConfig] Saved to: {configPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiStorageConfig] Error saving config: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clears the cached instance to force reload
        /// </summary>
        public static void ClearCache()
        {
            _instance = null;
        }

        /// <summary>
        /// Checks if the API storage is configured
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Url);

        /// <summary>
        /// Checks if credentials are required and provided
        /// </summary>
        public bool HasValidCredentials => NoAuthentication ||
            (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password));
    }
}
