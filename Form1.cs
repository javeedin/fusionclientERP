using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMSApp.PrintManagement;

// ADD THIS LINE to resolve ambiguity:
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WMSApp
{
    public partial class Form1 : Form
    {
        private WebView2 webView;
        private Panel urlPanel;
        private TextBox urlTextBox;
        private Button favoriteButton;
        private Button profileButton;
        private Button copilotButton;
        private Button backButton;
        private Button forwardButton;
        private Button refreshButton;
        private Button homeButton;
        private Button openFileButton;
        private Button clearCacheButton;
        private Button modulesButton;
        private ContextMenuStrip modulesContextMenu;
        private Label securityIcon;
        private FlowLayoutPanel tabBar;
        private ToolTip moduleToolTip;
        private ApexHtmlFileDownloader _apexDownloader;
        private Dictionary<WebView2, WebViewMessageRouter> _messageRouters = new Dictionary<WebView2, WebViewMessageRouter>();
        private Dictionary<string, Action<string, string>> _pendingRequests = new Dictionary<string, Action<string, string>>();

        private ClaudeApiHandler _claudeApiHandler;
        private PromptHistoryManager _promptHistoryManager;
        private Button historyButton;

        // Print Management Fields
        private PrintJobManager _printJobManager;
        private LocalStorageManager _storageManager;
        private PrinterService _printerService;
        private RestApiClient _restApiClient;

        // User Session Fields
        private string _loggedInUsername;
        private string _loggedInPassword;
        private string _loggedInInstance;
        private string _loggedInDateTime;
        private bool _isLoggedIn = false;
        private bool _isDevelopmentMode = true; // Toggle between Development and Distribution mode

        // Organization UI Fields
        private Panel _orgPanel;
        private Label _orgLabel;
        private Button _orgChangeButton;
        private InventoryOrganizationService _orgService;

        // Path constants
        private const string DEV_BASE_PATH = @"C:\Users\Javeed Shaik\source\repos\javeedin\fusionclientERP";
        private const string DIST_BASE_PATH = @"C:\fusionclient\ERP";

        public Form1()
        {
            InitializeComponent();
            InitializeComponent1();

            // ⭐ ADD THIS TEST
            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine("🚀 APPLICATION STARTED - TESTING DEBUG OUTPUT");
            System.Diagnostics.Debug.WriteLine("========================================");

            SetupUI();

            // Initialize the APEX downloader
            _apexDownloader = new ApexHtmlFileDownloader();
            _messageRouters = new Dictionary<WebView2, WebViewMessageRouter>();
            var restClient = new RestApiClient();
            _claudeApiHandler = new ClaudeApiHandler();
            _promptHistoryManager = new PromptHistoryManager();

            // Initialize Print Management Services
            _printJobManager = new PrintJobManager();
            _storageManager = new LocalStorageManager();
            _printerService = new PrinterService();
            _restApiClient = new RestApiClient();

            // Initialize Organization Service
            _orgService = new InventoryOrganizationService();
        }

        private void InitializeComponent1()
        {
            this.Text = "FusionClientERP V1.0";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
        }

        // Fixed installation path for web files
        private const string INSTALL_PATH = @"C:\fusion\fusionclientweb";

        /// <summary>
        /// Gets the base path for web files (wms, gl, sync, etc.)
        /// Supports development mode and installed mode at C:\fusion\fusionclientweb
        /// </summary>
        private string GetWebFilesBasePath()
        {
            // PRIORITY 1: Check for development mode first (running from source)
            // Go up from bin/Debug/net8.0-windows to repo root
            string devPath = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", ".."));
            if (Directory.Exists(Path.Combine(devPath, "wms")))
            {
                System.Diagnostics.Debug.WriteLine($"[Path] Using development path: {devPath}");
                return devPath;
            }

            // PRIORITY 2: Check if files are installed at fixed location
            if (Directory.Exists(Path.Combine(INSTALL_PATH, "wms")))
            {
                System.Diagnostics.Debug.WriteLine($"[Path] Using installed path: {INSTALL_PATH}");
                return INSTALL_PATH;
            }

            // Check if files are in AppContext.BaseDirectory (single-file extracted to temp)
            string basePath = AppContext.BaseDirectory;
            if (Directory.Exists(Path.Combine(basePath, "wms")))
            {
                // Files found in temp extraction - install them to fixed location
                System.Diagnostics.Debug.WriteLine($"[Path] Found files in temp, installing to: {INSTALL_PATH}");
                InstallWebFiles(basePath, INSTALL_PATH);
                return INSTALL_PATH;
            }

            // Fallback to StartupPath
            System.Diagnostics.Debug.WriteLine($"[Path] Using fallback path: {Application.StartupPath}");
            return Application.StartupPath;
        }

        /// <summary>
        /// Installs web files from source (temp extraction) to C:\fusion\fusionclientweb
        /// </summary>
        private void InstallWebFiles(string sourcePath, string destPath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Install] Installing web files from {sourcePath} to {destPath}");

                // Create destination directory
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }

                // Folders to copy
                string[] folders = { "wms", "gl", "sync", "ar", "ap", "om", "fa", "ca", "pos" };

                foreach (string folder in folders)
                {
                    string sourceFolder = Path.Combine(sourcePath, folder);
                    string destFolder = Path.Combine(destPath, folder);

                    if (Directory.Exists(sourceFolder))
                    {
                        CopyDirectory(sourceFolder, destFolder);
                        System.Diagnostics.Debug.WriteLine($"[Install] Copied {folder}");
                    }
                }

                // Copy root files (index.html, app.js, etc.)
                string[] rootFiles = { "index.html", "app.js", "config.js", "styles.css", "printer-management-new.js", "monitor-printing.js" };
                foreach (string file in rootFiles)
                {
                    string sourceFile = Path.Combine(sourcePath, file);
                    string destFile = Path.Combine(destPath, file);

                    if (File.Exists(sourceFile))
                    {
                        File.Copy(sourceFile, destFile, true);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Install] Installation complete to {destPath}");
                MessageBox.Show(
                    $"Web files installed successfully to:\n{destPath}",
                    "Installation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine($"[Install ERROR] Access denied to {destPath}");
                MessageBox.Show(
                    $"Cannot install to {destPath}\n\nAccess denied. Please run as Administrator.",
                    "Installation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Install ERROR] {ex.Message}");
                MessageBox.Show(
                    $"Failed to install web files to {destPath}\n\nError: {ex.Message}",
                    "Installation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private bool ShowLoginForm()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ========================================");
            System.Diagnostics.Debug.WriteLine("[DEBUG] ShowLoginForm() CALLED");
            System.Diagnostics.Debug.WriteLine("[DEBUG] Creating LoginForm instance...");

            using (LoginForm loginForm = new LoginForm())
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] LoginForm instance created, calling ShowDialog()...");
                var dialogResult = loginForm.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ShowDialog returned: {dialogResult}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoginSuccessful: {loginForm.LoginSuccessful}");

                if (dialogResult == DialogResult.OK && loginForm.LoginSuccessful)
                {
                    _loggedInUsername = loginForm.Username;
                    _loggedInPassword = loginForm.Password;
                    _loggedInInstance = loginForm.InstanceName;
                    _loggedInDateTime = DateTime.Now.ToString("MMM dd, yyyy hh:mm:ss tt");
                    _isLoggedIn = true;

                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Success - User: {_loggedInUsername}, Instance: {_loggedInInstance}, DateTime: {_loggedInDateTime}");
                    return true;
                }
                return false;
            }
        }

        private void HandleLogout()
        {
            System.Diagnostics.Debug.WriteLine("[LOGOUT] Clearing user session...");

            // Clear session variables
            _isLoggedIn = false;
            _loggedInUsername = null;
            _loggedInPassword = null;
            _loggedInInstance = null;
            _loggedInDateTime = null;

            // Clear organization and session manager
            SessionManager.Clear();

            // Hide organization panel
            if (_orgPanel != null)
            {
                _orgPanel.Visible = false;
            }

            System.Diagnostics.Debug.WriteLine("[LOGOUT] User session cleared successfully");
        }

        private async Task ClearWebViewLoginStateAsync()
        {
            // Clear localStorage login state in WebView2 before navigating to WMS
            // This ensures fresh authentication is required each session
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[LOGIN] Clearing localStorage login state before WMS navigation...");
                    await wv.CoreWebView2.ExecuteScriptAsync(@"
                        localStorage.removeItem('loggedIn');
                        localStorage.removeItem('username');
                        localStorage.removeItem('password');
                        localStorage.removeItem('instanceName');
                        localStorage.removeItem('loginTime');
                        localStorage.removeItem('fusionCloudUsername');
                        localStorage.removeItem('fusionCloudPassword');
                        localStorage.removeItem('fusionInstance');
                        console.log('[C# CLEAR] localStorage login state cleared');
                    ");
                    System.Diagnostics.Debug.WriteLine("[LOGIN] ✅ localStorage login state cleared");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Warning: Could not clear localStorage: {ex.Message}");
                }
            }
        }

        private void NavigateToWmsIndex(string repoRoot)
        {
            string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "wms", "index.html"));

            if (File.Exists(indexPath))
            {
                System.Diagnostics.Debug.WriteLine($"[WMS Dev] Launching from local: {indexPath}");
                // Clear any stale localStorage login state before navigation
                _ = ClearWebViewLoginStateAsync();
                string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                Navigate(fileUrl);
            }
            else
            {
                MessageBox.Show(
                    "Local WMS development files not found at:\n" + indexPath,
                    "WMS Dev Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void HandleLoginSuccess(JsonElement root)
        {
            try
            {
                _loggedInUsername = root.TryGetProperty("username", out var userProp) ? userProp.GetString() : "Unknown";
                _loggedInInstance = root.TryGetProperty("instanceName", out var instProp) ? instProp.GetString() : "PROD";
                _loggedInDateTime = root.TryGetProperty("loginTime", out var timeProp) ? timeProp.GetString() : DateTime.Now.ToString("MMM dd, yyyy hh:mm:ss tt");
                _isLoggedIn = true;

                System.Diagnostics.Debug.WriteLine($"[LOGIN SUCCESS] User: {_loggedInUsername}, Instance: {_loggedInInstance}, DateTime: {_loggedInDateTime}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN ERROR] Failed to process login: {ex.Message}");
            }
        }

        private async Task HandleValidateLogin(WebView2 wv, JsonElement root, string requestId)
        {
            string username = "";
            string instanceName = "PROD";

            try
            {
                username = root.TryGetProperty("username", out var userProp) ? userProp.GetString() : "";
                string password = root.TryGetProperty("password", out var passProp) ? passProp.GetString() : "";
                instanceName = root.TryGetProperty("instanceName", out var instProp) ? instProp.GetString() : "PROD";

                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] ========================================");
                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] Username: {username}");
                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] Instance: {instanceName}");

                // Get LOGIN endpoint URL from EndpointConfigReader using IntegrationCode
                string baseUrl = EndpointConfigReader.GetEndpointUrl("APEX", "LOGIN", instanceName);
                if (string.IsNullOrEmpty(baseUrl))
                {
                    throw new Exception($"LOGIN endpoint not configured for instance '{instanceName}'. Please add LOGIN endpoint in Settings.");
                }
                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] BaseUrl from IntegrationCode: {baseUrl}");

                // Call the login endpoint with credentials
                string encodedUsername = Uri.EscapeDataString(username);
                string encodedPassword = Uri.EscapeDataString(password);
                string apiUrl = $"{baseUrl}/login?username={encodedUsername}&password={encodedPassword}";
                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] Calling login API: {baseUrl}/login?username={encodedUsername}&password=***");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var response = await client.GetAsync(apiUrl);

                    System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] Response status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] Response length: {jsonResponse.Length}");

                        // Parse JSON response
                        using (var doc = JsonDocument.Parse(jsonResponse))
                        {
                            var items = doc.RootElement.GetProperty("items");
                            int itemCount = 0;
                            foreach (var _ in items.EnumerateArray()) itemCount++;

                            if (itemCount > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] SUCCESS - User validated! Items count: {itemCount}");

                                // Set session
                                _loggedInUsername = username;
                                _loggedInInstance = instanceName;
                                _loggedInDateTime = DateTime.Now.ToString("MMM dd, yyyy hh:mm:ss tt");
                                _isLoggedIn = true;

                                // Send success response to JavaScript by calling the global function directly
                                string escapedUsername = username.Replace("\\", "\\\\").Replace("'", "\\'");
                                string escapedInstance = instanceName.Replace("\\", "\\\\").Replace("'", "\\'");

                                await wv.CoreWebView2.ExecuteScriptAsync($@"
                                    console.log('[C# -> JS] Calling handleLoginResponse with success...');
                                    if (typeof window.handleLoginResponse === 'function') {{
                                        window.handleLoginResponse(true, '{escapedUsername}', '{escapedInstance}', '');
                                    }} else {{
                                        console.error('[C# -> JS] handleLoginResponse function not found!');
                                    }}
                                ");

                                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] Sent success response to JS");
                            }
                            else
                            {
                                throw new Exception("Invalid username or password");
                            }
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                             response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new Exception("Invalid username or password");
                    }
                    else
                    {
                        throw new Exception($"Server error: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] FAILED - {ex.Message}");

                // Send error response to JavaScript by calling the global function directly
                string escapedError = ex.Message.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", " ").Replace("\r", "");
                string escapedUsername = username.Replace("\\", "\\\\").Replace("'", "\\'");
                string escapedInstance = instanceName.Replace("\\", "\\\\").Replace("'", "\\'");

                await wv.CoreWebView2.ExecuteScriptAsync($@"
                    console.log('[C# -> JS] Calling handleLoginResponse with error...');
                    if (typeof window.handleLoginResponse === 'function') {{
                        window.handleLoginResponse(false, '{escapedUsername}', '{escapedInstance}', '{escapedError}');
                    }} else {{
                        console.error('[C# -> JS] handleLoginResponse function not found!');
                        alert('Login failed: {escapedError}');
                    }}
                ");

                System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] Sent error response to JS");
            }

            System.Diagnostics.Debug.WriteLine($"[VALIDATE LOGIN] ========================================");
        }

        private void SetupUI()
        {
            // Initialize ToolTip
            moduleToolTip = new ToolTip
            {
                AutoPopDelay = 3000,
                InitialDelay = 500,
                ReshowDelay = 200,
                ShowAlways = true
            };

            // Title bar panel with tabs
            Panel titleBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(32, 32, 32)
            };

            // Tab bar (for custom tabs)
            tabBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(32, 32, 32),
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(5, 2, 5, 0)
            };

            // New Tab button in title bar
            Button newTabButton = new Button
            {
                Text = "+",
                Width = 35,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            newTabButton.FlatAppearance.BorderSize = 0;
            newTabButton.Click += (s, e) => AddNewTab("https://www.google.com");

            tabBar.Controls.Add(newTabButton);
            titleBarPanel.Controls.Add(tabBar);

            // Navigation panel
            Panel navPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 10, 10, 10)
            };

            int leftPosition = 10;

            // Back button - HIDDEN
            backButton = new Button
            {
                Text = "←",
                Width = 35,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand,
                Visible = false
            };
            backButton.FlatAppearance.BorderColor = Color.LightGray;
            backButton.Click += BackButton_Click;

            // Forward button - HIDDEN
            forwardButton = new Button
            {
                Text = "→",
                Width = 35,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand,
                Visible = false
            };
            forwardButton.FlatAppearance.BorderColor = Color.LightGray;
            forwardButton.Click += ForwardButton_Click;

            // Refresh button - VISIBLE
            refreshButton = new Button
            {
                Text = "⟳",
                Width = 35,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            refreshButton.FlatAppearance.BorderColor = Color.LightGray;
            refreshButton.Click += RefreshButton_Click;
            leftPosition += 40;

            // Home button - HIDDEN
            homeButton = new Button
            {
                Text = "⌂",
                Width = 35,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand,
                Visible = false
            };
            homeButton.FlatAppearance.BorderColor = Color.LightGray;
            homeButton.Click += HomeButton_Click;

            // Open File button - VISIBLE
            openFileButton = new Button
            {
                Text = "📁",
                Width = 35,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand
            };
            openFileButton.FlatAppearance.BorderColor = Color.LightGray;
            openFileButton.Click += OpenFileButton_Click;
            leftPosition += 45;

            // Clear Cache button - VISIBLE
            clearCacheButton = new Button
            {
                Text = "🗑️",
                Width = 35,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(255, 240, 240)
            };
            clearCacheButton.FlatAppearance.BorderColor = Color.LightCoral;
            clearCacheButton.Click += ClearCacheButton_Click;
            moduleToolTip.SetToolTip(clearCacheButton, "Clear Browser Cache");
            leftPosition += 45;

            // Settings Button - Opens endpoint settings
            Button navSettingsButton = new Button
            {
                Text = "Settings",
                Width = 80,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Tag = "SETTINGS"
            };
            navSettingsButton.FlatAppearance.BorderSize = 0;
            navSettingsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 98, 104);
            navSettingsButton.Click += (s, e) =>
            {
                var settingsForm = new SettingsForm();
                settingsForm.ShowDialog();
            };
            moduleToolTip.SetToolTip(navSettingsButton, "Settings");
            leftPosition += 85;

            // Inventory Button - Opens inventory module (requires login)
            Button inventoryButton = new Button
            {
                Text = "Inventory",
                Width = 85,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Tag = "INVENTORY"
            };
            inventoryButton.FlatAppearance.BorderSize = 0;
            inventoryButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 105, 217);
            inventoryButton.Click += InventoryButton_Click;
            moduleToolTip.SetToolTip(inventoryButton, "Inventory Management");
            leftPosition += 90;

            // Mode Toggle Panel (Dev/Dist)
            Panel modePanel = new Panel
            {
                Width = 100,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            Label lblMode = new Label
            {
                Text = "DEV",
                Width = 40,
                Height = 20,
                Top = 5,
                Left = 5,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 167, 69),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button btnModeToggle = new Button
            {
                Text = _isDevelopmentMode ? "●" : "○",
                Width = 50,
                Height = 24,
                Left = 45,
                Top = 3,
                FlatStyle = FlatStyle.Flat,
                BackColor = _isDevelopmentMode ? Color.FromArgb(40, 167, 69) : Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                Tag = "MODE_TOGGLE"
            };
            btnModeToggle.FlatAppearance.BorderSize = 0;
            btnModeToggle.Click += (s, e) =>
            {
                _isDevelopmentMode = !_isDevelopmentMode;
                btnModeToggle.Text = _isDevelopmentMode ? "●" : "○";
                btnModeToggle.BackColor = _isDevelopmentMode ? Color.FromArgb(40, 167, 69) : Color.FromArgb(108, 117, 125);
                lblMode.Text = _isDevelopmentMode ? "DEV" : "DIST";
                lblMode.ForeColor = _isDevelopmentMode ? Color.FromArgb(40, 167, 69) : Color.FromArgb(108, 117, 125);
                System.Diagnostics.Debug.WriteLine($"[MODE] Switched to: {(_isDevelopmentMode ? "Development" : "Distribution")}");

                // When switching to distribution mode, ensure folders exist
                if (!_isDevelopmentMode)
                {
                    EnsureDistributionFoldersExist();
                }
            };
            moduleToolTip.SetToolTip(btnModeToggle, "Toggle Development/Distribution Mode");

            modePanel.Controls.Add(lblMode);
            modePanel.Controls.Add(btnModeToggle);
            leftPosition += 105;

            // Organization Display Panel
            _orgPanel = new Panel
            {
                Width = 200,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                BackColor = Color.FromArgb(245, 245, 245),
                Visible = false // Hidden until organization is selected
            };

            _orgLabel = new Label
            {
                Text = "No Organization",
                AutoSize = false,
                Width = 160,
                Height = 24,
                Top = 3,
                Left = 5,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(60, 60, 60),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            _orgLabel.Click += OrgLabel_Click;
            moduleToolTip.SetToolTip(_orgLabel, "Click to change organization");

            _orgChangeButton = new Button
            {
                Text = "⟳",
                Width = 28,
                Height = 24,
                Left = 168,
                Top = 3,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(102, 126, 234),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            _orgChangeButton.FlatAppearance.BorderSize = 0;
            _orgChangeButton.Click += OrgChangeButton_Click;
            moduleToolTip.SetToolTip(_orgChangeButton, "Change Organization");

            _orgPanel.Controls.Add(_orgLabel);
            _orgPanel.Controls.Add(_orgChangeButton);
            leftPosition += 205;

            // Modules Dropdown Button
            modulesButton = new Button
            {
                Text = "Modules ▼",
                Width = 90,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(240, 240, 240),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Tag = "MODULES"
            };
            modulesButton.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            modulesButton.FlatAppearance.BorderSize = 1;
            modulesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);

            // Create context menu for modules dropdown
            modulesContextMenu = new ContextMenuStrip();
            modulesContextMenu.Items.Add("WMS - Warehouse Management").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "wms", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("GL - General Ledger").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "gl", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("SYNC - Oracle Fusion Sync").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "sync", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("AR - Accounts Receivable").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "ar", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("AP - Accounts Payable").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "ap", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("OM - Order Management").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "om", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("FA - Fixed Assets").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "fa", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("CA - Cash Management").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "ca", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesContextMenu.Items.Add("POS - Point of Sale").Click += (s, e) =>
            {
                string repoRoot = GetWebFilesBasePath();
                string indexPath = Path.GetFullPath(Path.Combine(repoRoot, "pos", "index.html"));
                if (File.Exists(indexPath))
                {
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    Navigate(fileUrl);
                }
            };

            modulesButton.Click += (s, e) =>
            {
                modulesContextMenu.Show(modulesButton, new Point(0, modulesButton.Height));
            };

            moduleToolTip.SetToolTip(modulesButton, "Select a module to launch");
            leftPosition += 95;

            // Compact Oval URL panel container
            urlPanel = new Panel
            {
                Left = leftPosition,
                Top = 10,
                Width = this.ClientSize.Width - leftPosition - 250,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            urlPanel.Paint += UrlPanel_Paint;

            // Security/Lock icon (inside oval panel on the left)
            securityIcon = new Label
            {
                Text = "🔒",
                Left = 8,
                Top = 7,
                Width = 20,
                Height = 18,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            securityIcon.Click += SecurityIcon_Click;

            // URL textbox (inside oval panel, after security icon)
            urlTextBox = new TextBox
            {
                Left = 32,
                Top = 7,
                Height = 18,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            urlTextBox.Width = urlPanel.Width - 150;
            urlTextBox.KeyDown += UrlTextBox_KeyDown;

            // Favorite button (star icon) - compact version
            favoriteButton = new Button
            {
                Text = "☆",
                Width = 28,
                Height = 22,
                Top = 5,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                BackColor = Color.White,
                ForeColor = Color.Gray,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false
            };
            favoriteButton.FlatAppearance.BorderSize = 0;
            favoriteButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
            favoriteButton.Click += FavoriteButton_Click;

            // Copilot button (icon version) - made more compact
            copilotButton = new Button
            {
                Text = "🤖",
                Width = 28,
                Height = 22,
                Top = 5,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0, 100, 200),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false
            };
            copilotButton.FlatAppearance.BorderSize = 0;
            copilotButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 240, 255);
            copilotButton.Click += CopilotButton_Click;

            // Add controls to URL panel
            urlPanel.Controls.Add(securityIcon);
            urlPanel.Controls.Add(urlTextBox);
            urlPanel.Controls.Add(favoriteButton);
            urlPanel.Controls.Add(copilotButton);

            // Position buttons from right to left inside the URL panel
            copilotButton.Left = urlPanel.Width - copilotButton.Width - 8;
            favoriteButton.Left = copilotButton.Left - favoriteButton.Width - 2;
            urlTextBox.Width = favoriteButton.Left - urlTextBox.Left - 5;

            // Profile button (outside URL panel, to the right)
            profileButton = new Button
            {
                Text = "👤",
                Width = 35,
                Height = 30,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false
            };
            profileButton.Left = urlPanel.Right + 10;
            profileButton.FlatAppearance.BorderColor = Color.LightGray;
            profileButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            profileButton.Click += ProfileButton_Click;

            // History button (after profile button)
            historyButton = new Button
            {
                Text = "",
                Width = 60,
                Height = 30,
                Left = leftPosition,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(245, 235, 220),
                Tag = "His"
            };
            historyButton.Left = profileButton.Right + 5;
            historyButton.FlatAppearance.BorderColor = Color.LightGray;
            historyButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            historyButton.Click += HistoryButton_Click;

            // Add to navigation panel
            navPanel.Controls.Add(historyButton);

            // Settings button (gear icon)
            Button settingsButton = new Button
            {
                Text = "⚙",
                Width = 35,
                Height = 30,
                Top = 10,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            settingsButton.Left = this.ClientSize.Width - 45;
            settingsButton.FlatAppearance.BorderColor = Color.LightGray;
            settingsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 220);
            settingsButton.Click += SettingsButton_Click;
            moduleToolTip.SetToolTip(settingsButton, "Settings");

            // Add controls to navigation panel
            navPanel.Controls.Add(backButton);
            navPanel.Controls.Add(forwardButton);
            navPanel.Controls.Add(refreshButton);
            navPanel.Controls.Add(homeButton);
            navPanel.Controls.Add(openFileButton);
            navPanel.Controls.Add(clearCacheButton);
            navPanel.Controls.Add(navSettingsButton);
            navPanel.Controls.Add(inventoryButton);
            navPanel.Controls.Add(modePanel);
            navPanel.Controls.Add(_orgPanel);
            navPanel.Controls.Add(modulesButton);
            // navPanel.Controls.Add(urlPanel); // HIDDEN: Address bar removed per user request
            navPanel.Controls.Add(profileButton);
            navPanel.Controls.Add(settingsButton);

            // Web content panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // Add controls to form
            this.Controls.Add(contentPanel);
            this.Controls.Add(navPanel);
            this.Controls.Add(titleBarPanel);

            // Create initial tab
            AddNewTab("https://www.google.com");
        }

        private void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} - {message}");
        }

        private void HistoryButton_Click(object sender, EventArgs e)
        {
            var viewer = new PromptHistoryViewer(_promptHistoryManager);
            viewer.ShowDialog();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
        }

        private void EnsureDistributionFoldersExist()
        {
            try
            {
                // Create main distribution folder
                if (!Directory.Exists(DIST_BASE_PATH))
                {
                    Directory.CreateDirectory(DIST_BASE_PATH);
                    System.Diagnostics.Debug.WriteLine($"[DIST] Created: {DIST_BASE_PATH}");
                }

                // Create INV folder
                string invPath = Path.Combine(DIST_BASE_PATH, "inv");
                if (!Directory.Exists(invPath))
                {
                    Directory.CreateDirectory(invPath);
                    System.Diagnostics.Debug.WriteLine($"[DIST] Created: {invPath}");
                }

                // Copy inv/index.html from dev to dist if not exists
                string devInvIndex = Path.Combine(DEV_BASE_PATH, "inv", "index.html");
                string distInvIndex = Path.Combine(invPath, "index.html");

                if (File.Exists(devInvIndex) && !File.Exists(distInvIndex))
                {
                    File.Copy(devInvIndex, distInvIndex);
                    System.Diagnostics.Debug.WriteLine($"[DIST] Copied inv/index.html to distribution folder");
                    MessageBox.Show(
                        $"Distribution folder created:\n{DIST_BASE_PATH}\n\n" +
                        "INV module files have been copied.",
                        "Distribution Setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                // Create settings folder if not exists
                string settingsPath = Path.Combine(DIST_BASE_PATH, "settings");
                if (!Directory.Exists(settingsPath))
                {
                    Directory.CreateDirectory(settingsPath);
                    System.Diagnostics.Debug.WriteLine($"[DIST] Created: {settingsPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DIST] Error creating folders: {ex.Message}");
                MessageBox.Show(
                    $"Error creating distribution folders:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void InventoryButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Inventory button clicked");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Current _isLoggedIn: {_isLoggedIn}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Mode: {(_isDevelopmentMode ? "Development" : "Distribution")}");

            // Check if user is logged in
            if (!_isLoggedIn)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] User NOT logged in, showing login form...");

                // Show login form
                if (!ShowLoginForm())
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Login cancelled or failed");
                    return;
                }

                // After successful login, fetch organizations
                await FetchAndSelectOrganizationAsync();
            }

            // Check if organization is selected
            if (!SessionManager.HasOrganization)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] No organization selected, showing selection form...");
                await FetchAndSelectOrganizationAsync();

                // If still no organization selected, cancel
                if (!SessionManager.HasOrganization)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Organization selection cancelled");
                    return;
                }
            }

            // User is logged in and has organization, navigate to Inventory module
            System.Diagnostics.Debug.WriteLine("[DEBUG] User logged in, navigating to Inventory module");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Username: {_loggedInUsername}, Instance: {_loggedInInstance}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Organization: {SessionManager.OrganizationDisplayName}");

            // Get path based on mode
            string basePath = _isDevelopmentMode ? DEV_BASE_PATH : DIST_BASE_PATH;
            string indexPath = Path.GetFullPath(Path.Combine(basePath, "inv", "index.html"));

            System.Diagnostics.Debug.WriteLine($"[DEBUG] INV Path: {indexPath}");

            if (File.Exists(indexPath))
            {
                string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                Navigate(fileUrl);

                // Send login info to the webview after navigation
                _ = SendLoginInfoToInventoryAsync();
            }
            else
            {
                string modeText = _isDevelopmentMode ? "Development" : "Distribution";
                MessageBox.Show(
                    $"Inventory module not found.\n\n" +
                    $"Mode: {modeText}\n" +
                    $"Expected path: {indexPath}\n\n" +
                    $"Please ensure the INV module is installed in the {modeText.ToLower()} folder.",
                    "Module Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async Task FetchAndSelectOrganizationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Fetching inventory organizations...");

                // Show loading cursor
                this.Cursor = Cursors.WaitCursor;

                // Fetch organizations from webservice
                var organizations = await _orgService.GetOrganizationsAsync(_loggedInUsername, _loggedInInstance);

                this.Cursor = Cursors.Default;

                if (organizations == null || organizations.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] No organizations found");
                    MessageBox.Show(
                        "No inventory organizations found for your account.\n\n" +
                        "Please contact your administrator to configure INVENTORY_ORGS endpoint in Settings.",
                        "No Organizations",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Store available organizations
                SessionManager.AvailableOrganizations = organizations;

                // Show organization selection form
                using (var orgForm = new OrganizationSelectionForm(organizations, SessionManager.SelectedOrganization))
                {
                    if (orgForm.ShowDialog() == DialogResult.OK && orgForm.SelectedOrganization != null)
                    {
                        SessionManager.SetOrganization(orgForm.SelectedOrganization);
                        UpdateOrganizationDisplay();
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Organization selected: {SessionManager.OrganizationDisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Error fetching organizations: {ex.Message}");
                MessageBox.Show(
                    $"Error fetching organizations:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UpdateOrganizationDisplay()
        {
            if (SessionManager.HasOrganization)
            {
                _orgLabel.Text = SessionManager.OrganizationDisplayName;
                _orgPanel.Visible = true;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Organization display updated: {SessionManager.OrganizationDisplayName}");
            }
            else
            {
                _orgLabel.Text = "No Organization";
                _orgPanel.Visible = false;
            }
        }

        private void OrgLabel_Click(object sender, EventArgs e)
        {
            // Show organization selection form
            ShowOrganizationSelectionForm();
        }

        private void OrgChangeButton_Click(object sender, EventArgs e)
        {
            // Show organization selection form
            ShowOrganizationSelectionForm();
        }

        private void ShowOrganizationSelectionForm()
        {
            if (!_isLoggedIn)
            {
                MessageBox.Show("Please login first.", "Not Logged In",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (SessionManager.AvailableOrganizations.Count == 0)
            {
                // Fetch organizations first
                _ = FetchAndSelectOrganizationAsync();
                return;
            }

            // Show organization selection form
            using (var orgForm = new OrganizationSelectionForm(SessionManager.AvailableOrganizations, SessionManager.SelectedOrganization))
            {
                if (orgForm.ShowDialog() == DialogResult.OK && orgForm.SelectedOrganization != null)
                {
                    SessionManager.SetOrganization(orgForm.SelectedOrganization);
                    UpdateOrganizationDisplay();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Organization changed to: {SessionManager.OrganizationDisplayName}");
                }
            }
        }

        private void HandleOrganizationChanged(JsonElement root)
        {
            try
            {
                if (root.TryGetProperty("organization", out var orgElement))
                {
                    var org = new InventoryOrganization
                    {
                        OrganizationId = orgElement.TryGetProperty("organization_id", out var idProp) ? idProp.GetInt64() : 0,
                        OrganizationName = orgElement.TryGetProperty("organization_name", out var nameProp) ? nameProp.GetString() : "",
                        OrganizationCode = orgElement.TryGetProperty("organization_code", out var codeProp) ? codeProp.GetString() : "",
                        ClassificationCode = orgElement.TryGetProperty("classification_code", out var classProp) ? classProp.GetString() : "",
                        Status = orgElement.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "",
                        MasterOrganizationId = orgElement.TryGetProperty("master_organization_id", out var masterProp) ? masterProp.GetInt64() : 0
                    };

                    // Update SessionManager
                    SessionManager.SetOrganization(org);

                    // Update toolbar display
                    this.Invoke((MethodInvoker)delegate
                    {
                        UpdateOrganizationDisplay();
                    });

                    System.Diagnostics.Debug.WriteLine($"[ORG] Organization changed from WebView: {org.OrganizationName} ({org.OrganizationCode})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ORG] Error handling organization change: {ex.Message}");
            }
        }

        private Task HandleGetEndpoint(WebView2 wv, JsonElement root, string requestId)
        {
            try
            {
                string integrationCode = root.TryGetProperty("integrationCode", out var codeProp) ? codeProp.GetString() : "";
                string instanceName = root.TryGetProperty("instanceName", out var instProp) ? instProp.GetString() : _loggedInInstance;

                System.Diagnostics.Debug.WriteLine($"[Endpoint] Getting endpoint for: {integrationCode} - {instanceName}");

                string endpoint = GetEndpointFromSettings(integrationCode, instanceName);

                var response = new
                {
                    requestId = requestId,
                    endpoint = endpoint,
                    success = !string.IsNullOrEmpty(endpoint)
                };

                string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsString(responseJson);

                System.Diagnostics.Debug.WriteLine($"[Endpoint] Returned: {endpoint}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Endpoint] Error: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private string GetEndpointFromSettings(string integrationCode, string instanceName)
        {
            try
            {
                string settingsPath = @"C:\fusionclient\ERP\settings\endpoints.xml";

                if (!File.Exists(settingsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[Endpoint] Settings file not found: {settingsPath}");
                    return null;
                }

                var doc = new System.Xml.XmlDocument();
                doc.Load(settingsPath);

                var endpoints = doc.SelectNodes("/Endpoints/Endpoint");
                if (endpoints != null)
                {
                    foreach (System.Xml.XmlNode endpoint in endpoints)
                    {
                        string source = endpoint.SelectSingleNode("Source")?.InnerText ?? "";
                        string code = endpoint.SelectSingleNode("IntegrationCode")?.InnerText ?? "";
                        string instance = endpoint.SelectSingleNode("InstanceName")?.InnerText ?? "";
                        string url = endpoint.SelectSingleNode("URL")?.InnerText ?? "";
                        string path = endpoint.SelectSingleNode("Path")?.InnerText
                            ?? endpoint.SelectSingleNode("Endpoint")?.InnerText
                            ?? "";

                        if (code.Equals(integrationCode, StringComparison.OrdinalIgnoreCase) &&
                            instance.Equals(instanceName, StringComparison.OrdinalIgnoreCase))
                        {
                            string fullUrl = url.TrimEnd('/') + path;
                            System.Diagnostics.Debug.WriteLine($"[Endpoint] Found: {integrationCode} -> {fullUrl}");
                            return fullUrl;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Endpoint] Not found: {integrationCode} - {instanceName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Endpoint] Error loading settings: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Handles getAllEndpoints request from webview - loads endpoints from APEX (with XML fallback)
        /// </summary>
        private async Task HandleGetAllEndpoints(WebView2 wv, string requestId)
        {
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] HandleGetAllEndpoints STARTED");
            System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] RequestId: {requestId}");
            System.Diagnostics.Debug.WriteLine($"========================================");

            try
            {
                List<EndpointConfig> endpoints = null;
                string dataSource = "unknown";
                string fullApexUrl = "";
                string rawJsonResponse = "";

                // Try to load from APEX first
                string settingsPath = EndpointConfigReader.GetSettingsPath();
                string apexUrlFilePath = System.IO.Path.Combine(settingsPath, "apexendpointurl.txt");

                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Settings Path: {settingsPath}");
                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] APEX URL File: {apexUrlFilePath}");
                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] File Exists: {System.IO.File.Exists(apexUrlFilePath)}");

                if (System.IO.File.Exists(apexUrlFilePath))
                {
                    string apexUrl = System.IO.File.ReadAllText(apexUrlFilePath).Trim();
                    fullApexUrl = apexUrl;
                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] ====== FULL URL ======");
                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] GET {apexUrl}");
                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] =======================");

                    if (!string.IsNullOrWhiteSpace(apexUrl))
                    {
                        System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Attempting to fetch from APEX...");

                        try
                        {
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(30);
                                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Making HTTP GET request to: {apexUrl}");

                                var response = await client.GetAsync(apexUrl);
                                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Response Status: {(int)response.StatusCode} ({response.StatusCode})");

                                if (response.IsSuccessStatusCode)
                                {
                                    string jsonResponse = await response.Content.ReadAsStringAsync();
                                    rawJsonResponse = jsonResponse;
                                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] ====== RAW RESPONSE ======");
                                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] {jsonResponse}");
                                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] ==========================");

                                    endpoints = ParseApexEndpointsResponse(jsonResponse);
                                    dataSource = "APEX";
                                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] *** SUCCESS - Parsed {endpoints?.Count ?? 0} endpoints from APEX ***");
                                }
                                else
                                {
                                    string errorBody = await response.Content.ReadAsStringAsync();
                                    rawJsonResponse = errorBody;
                                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] *** HTTP ERROR: {response.StatusCode} ***");
                                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Error Body: {errorBody}");
                                }
                            }
                        }
                        catch (Exception apexEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] *** APEX FETCH FAILED ***");
                            System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Exception: {apexEx.GetType().Name}");
                            System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Message: {apexEx.Message}");
                            rawJsonResponse = $"ERROR: {apexEx.Message}";
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] *** apexendpointurl.txt NOT FOUND ***");
                    rawJsonResponse = "ERROR: apexendpointurl.txt not found";
                }

                // Fallback to local XML if APEX failed
                if (endpoints == null || endpoints.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Falling back to local XML file...");
                    EndpointConfigReader.ClearCache();
                    endpoints = EndpointConfigReader.LoadEndpoints();
                    dataSource = "LOCAL_XML";
                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Loaded {endpoints?.Count ?? 0} endpoints from XML");
                }

                // Log each endpoint
                foreach (var ep in endpoints ?? new List<EndpointConfig>())
                {
                    System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS]   -> Sno={ep.Sno}, Source={ep.Source}, Code={ep.IntegrationCode}, Instance={ep.InstanceName}");
                }

                // Convert to JSON-friendly format
                var endpointsList = (endpoints ?? new List<EndpointConfig>()).Select(ep => new
                {
                    sno = ep.Sno,
                    source = ep.Source,
                    instanceName = ep.InstanceName,
                    integrationCode = ep.IntegrationCode,
                    url = ep.BaseUrl,
                    path = ep.Endpoint,
                    comments = ep.Comments,
                    parameters = "",
                    offset = 0,
                    limit = 100
                }).ToList();

                var responseObj = new
                {
                    requestId = requestId,
                    action = "getAllEndpointsResponse",
                    success = true,
                    source = dataSource,
                    endpoints = endpointsList,
                    settingsPath = settingsPath,
                    debug = new
                    {
                        fullUrl = fullApexUrl,
                        rawResponse = rawJsonResponse
                    }
                };

                string responseJson = System.Text.Json.JsonSerializer.Serialize(responseObj);
                wv.CoreWebView2.PostWebMessageAsString(responseJson);

                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] *** COMPLETE - Returned {endpointsList.Count} endpoints (source: {dataSource}) ***");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] *** FATAL ERROR ***");
                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Exception: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[APEX ENDPOINTS] StackTrace: {ex.StackTrace}");

                var errorResponse = new
                {
                    requestId = requestId,
                    action = "getAllEndpointsResponse",
                    success = false,
                    source = "ERROR",
                    error = ex.Message,
                    debug = new
                    {
                        fullUrl = "",
                        rawResponse = ex.Message
                    }
                };
                string errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                wv.CoreWebView2.PostWebMessageAsString(errorJson);
            }
        }

        /// <summary>
        /// Parses the APEX JSON response into EndpointConfig list
        /// </summary>
        private List<EndpointConfig> ParseApexEndpointsResponse(string jsonResponse)
        {
            var endpoints = new List<EndpointConfig>();

            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(jsonResponse))
                {
                    var root = doc.RootElement;

                    // Check if response is an array or has an "items" property
                    System.Text.Json.JsonElement itemsArray;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        itemsArray = root;
                    }
                    else if (root.TryGetProperty("items", out itemsArray))
                    {
                        // APEX REST often wraps results in "items"
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[APEX PARSE] Unexpected JSON structure");
                        return endpoints;
                    }

                    foreach (var item in itemsArray.EnumerateArray())
                    {
                        var endpoint = new EndpointConfig();

                        // Map fields (case-insensitive)
                        if (item.TryGetProperty("sno", out var snoEl) || item.TryGetProperty("SNO", out snoEl))
                            endpoint.Sno = snoEl.TryGetInt32(out int sno) ? sno : 0;

                        if (item.TryGetProperty("source", out var sourceEl) || item.TryGetProperty("SOURCE", out sourceEl))
                            endpoint.Source = sourceEl.GetString() ?? "";

                        if (item.TryGetProperty("integration_code", out var codeEl) || item.TryGetProperty("INTEGRATION_CODE", out codeEl))
                            endpoint.IntegrationCode = codeEl.GetString() ?? "";

                        if (item.TryGetProperty("instance_name", out var instEl) || item.TryGetProperty("INSTANCE_NAME", out instEl))
                            endpoint.InstanceName = instEl.GetString() ?? "";

                        if (item.TryGetProperty("base_url", out var urlEl) || item.TryGetProperty("BASE_URL", out urlEl))
                            endpoint.BaseUrl = urlEl.GetString() ?? "";

                        if (item.TryGetProperty("endpoint", out var epEl) || item.TryGetProperty("ENDPOINT", out epEl))
                            endpoint.Endpoint = epEl.GetString() ?? "";

                        if (item.TryGetProperty("comments", out var commEl) || item.TryGetProperty("COMMENTS", out commEl))
                            endpoint.Comments = commEl.GetString() ?? "";

                        endpoints.Add(endpoint);
                        System.Diagnostics.Debug.WriteLine($"[APEX PARSE] Parsed: Sno={endpoint.Sno}, Source={endpoint.Source}, Code={endpoint.IntegrationCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX PARSE] Error parsing JSON: {ex.Message}");
            }

            return endpoints;
        }

        /// <summary>
        /// Handles getAllInstances request from webview - fetches instances from APEX URL
        /// </summary>
        private async Task HandleGetAllInstances(WebView2 wv, string requestId)
        {
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] HandleGetAllInstances STARTED");
            System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] RequestId: {requestId}");
            System.Diagnostics.Debug.WriteLine($"========================================");

            string dataSource = "NONE";
            string fullApexUrl = "";
            string rawJsonResponse = "";
            List<Dictionary<string, object>> instances = null;

            try
            {
                // Try to load from APEX
                string settingsPath = EndpointConfigReader.GetSettingsPath();
                string apexUrlFilePath = System.IO.Path.Combine(settingsPath, "apexinstances.txt");

                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Settings Path: {settingsPath}");
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Settings Directory Exists: {System.IO.Directory.Exists(settingsPath)}");
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] APEX URL File: {apexUrlFilePath}");
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] File Exists: {System.IO.File.Exists(apexUrlFilePath)}");

                // List all files in settings directory for debugging
                if (System.IO.Directory.Exists(settingsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Files in settings directory:");
                    try
                    {
                        foreach (var file in System.IO.Directory.GetFiles(settingsPath))
                        {
                            System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES]   -> {System.IO.Path.GetFileName(file)}");
                        }
                    }
                    catch (Exception dirEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Error listing directory: {dirEx.Message}");
                    }
                }

                if (System.IO.File.Exists(apexUrlFilePath))
                {
                    string apexUrl = System.IO.File.ReadAllText(apexUrlFilePath).Trim();
                    fullApexUrl = apexUrl;
                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] ====== FULL URL ======");
                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] GET {apexUrl}");
                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] =======================");

                    if (!string.IsNullOrWhiteSpace(apexUrl))
                    {
                        System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Attempting to fetch from APEX...");

                        try
                        {
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(30);
                                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Making HTTP GET request to: {apexUrl}");

                                var response = await client.GetAsync(apexUrl);
                                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Response Status: {(int)response.StatusCode} ({response.StatusCode})");

                                if (response.IsSuccessStatusCode)
                                {
                                    string jsonResponse = await response.Content.ReadAsStringAsync();
                                    rawJsonResponse = jsonResponse;
                                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] ====== RAW RESPONSE ======");
                                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] {jsonResponse}");
                                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] ==========================");

                                    instances = ParseApexInstancesResponse(jsonResponse);
                                    dataSource = "APEX";
                                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] *** SUCCESS - Parsed {instances?.Count ?? 0} instances from APEX ***");
                                }
                                else
                                {
                                    string errorBody = await response.Content.ReadAsStringAsync();
                                    rawJsonResponse = errorBody;
                                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] *** HTTP ERROR: {response.StatusCode} ***");
                                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Error Body: {errorBody}");
                                }
                            }
                        }
                        catch (Exception apexEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] *** APEX FETCH FAILED ***");
                            System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Exception: {apexEx.GetType().Name}");
                            System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Message: {apexEx.Message}");
                            rawJsonResponse = $"ERROR: {apexEx.Message}";
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] *** apexinstances.txt NOT FOUND ***");
                    rawJsonResponse = "ERROR: apexinstances.txt not found";
                }

                // Log each instance
                foreach (var inst in instances ?? new List<Dictionary<string, object>>())
                {
                    System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES]   -> Instance: {inst.GetValueOrDefault("name", "N/A")}");
                }

                var responseObj = new
                {
                    requestId = requestId,
                    action = "getAllInstancesResponse",
                    success = instances != null && instances.Count > 0,
                    source = dataSource,
                    instances = instances ?? new List<Dictionary<string, object>>(),
                    settingsPath = settingsPath,
                    debug = new
                    {
                        fullUrl = fullApexUrl,
                        rawResponse = rawJsonResponse
                    }
                };

                string responseJson = System.Text.Json.JsonSerializer.Serialize(responseObj);
                wv.CoreWebView2.PostWebMessageAsString(responseJson);

                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] *** COMPLETE - Returned {instances?.Count ?? 0} instances (source: {dataSource}) ***");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] *** FATAL ERROR ***");
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Exception: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES] StackTrace: {ex.StackTrace}");

                var errorResponse = new
                {
                    requestId = requestId,
                    action = "getAllInstancesResponse",
                    success = false,
                    source = "ERROR",
                    error = ex.Message,
                    debug = new
                    {
                        fullUrl = fullApexUrl,
                        rawResponse = ex.Message
                    }
                };
                string errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                wv.CoreWebView2.PostWebMessageAsString(errorJson);
            }
        }

        /// <summary>
        /// Parses the APEX JSON response into a list of instance dictionaries
        /// </summary>
        private List<Dictionary<string, object>> ParseApexInstancesResponse(string jsonResponse)
        {
            var instances = new List<Dictionary<string, object>>();

            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(jsonResponse))
                {
                    var root = doc.RootElement;

                    // Check if response is an array or has an "items" property
                    System.Text.Json.JsonElement itemsArray;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        itemsArray = root;
                    }
                    else if (root.TryGetProperty("items", out itemsArray))
                    {
                        // APEX REST often wraps results in "items"
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES PARSE] Unexpected JSON structure");
                        return instances;
                    }

                    foreach (var item in itemsArray.EnumerateArray())
                    {
                        var instance = new Dictionary<string, object>();

                        // Map all fields dynamically - preserve original field names
                        foreach (var prop in item.EnumerateObject())
                        {
                            string key = prop.Name.ToLower();
                            object value = prop.Value.ValueKind switch
                            {
                                System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                                System.Text.Json.JsonValueKind.Number => prop.Value.TryGetInt32(out int i) ? i : prop.Value.GetDouble(),
                                System.Text.Json.JsonValueKind.True => true,
                                System.Text.Json.JsonValueKind.False => false,
                                System.Text.Json.JsonValueKind.Null => null,
                                _ => prop.Value.GetRawText()
                            };
                            instance[key] = value;
                        }

                        instances.Add(instance);
                        System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES PARSE] Parsed instance: {string.Join(", ", instance.Select(kv => $"{kv.Key}={kv.Value}"))}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX INSTANCES PARSE] Error parsing JSON: {ex.Message}");
            }

            return instances;
        }

        /// <summary>
        /// Handles saveAllEndpoints request from webview - saves all endpoints to XML file
        /// </summary>
        private Task HandleSaveAllEndpoints(WebView2 wv, JsonElement root, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Endpoints] Saving all endpoints to XML");

                if (!root.TryGetProperty("endpoints", out var endpointsArray))
                {
                    throw new Exception("No endpoints array in request");
                }

                var endpoints = new List<EndpointConfig>();
                int snoCounter = 1;

                foreach (var ep in endpointsArray.EnumerateArray())
                {
                    // Parse sourceInstance like "APEX:PROD" or "FUSION:TEST"
                    string sourceInstance = ep.TryGetProperty("sourceInstance", out var siProp) ? siProp.GetString() : "APEX:PROD";
                    string[] parts = sourceInstance.Split(':');
                    string source = parts.Length > 0 ? parts[0] : "APEX";
                    string instanceName = parts.Length > 1 ? parts[1] : "PROD";

                    var endpoint = new EndpointConfig
                    {
                        Sno = ep.TryGetProperty("sno", out var snoProp) ? snoProp.GetInt32() : snoCounter++,
                        Source = source,
                        IntegrationCode = ep.TryGetProperty("integrationCode", out var codeProp) ? codeProp.GetString() : "",
                        InstanceName = instanceName,
                        BaseUrl = ep.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : "",
                        Endpoint = ep.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : "",
                        Comments = ep.TryGetProperty("comments", out var commentsProp) ? commentsProp.GetString() : ""
                    };

                    endpoints.Add(endpoint);
                }

                // Save to XML file
                EndpointConfigReader.SaveEndpoints(endpoints);

                var response = new
                {
                    requestId = requestId,
                    action = "saveAllEndpointsResponse",
                    success = true,
                    count = endpoints.Count,
                    settingsPath = EndpointConfigReader.GetSettingsPath()
                };

                string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsString(responseJson);

                System.Diagnostics.Debug.WriteLine($"[Endpoints] Saved {endpoints.Count} endpoints to XML");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Endpoints] Error saving endpoints: {ex.Message}");

                var errorResponse = new
                {
                    requestId = requestId,
                    action = "saveAllEndpointsResponse",
                    success = false,
                    error = ex.Message
                };
                string errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                wv.CoreWebView2.PostWebMessageAsString(errorJson);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles getApexEndpointUrl request from webview - reads URL from apexendpointurl.txt
        /// </summary>
        private Task HandleGetApexEndpointUrl(WebView2 wv, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] Getting APEX endpoint URL from file");

                string settingsPath = EndpointConfigReader.GetSettingsPath();
                string apexUrlFilePath = System.IO.Path.Combine(settingsPath, "apexendpointurl.txt");

                string url = "";
                if (System.IO.File.Exists(apexUrlFilePath))
                {
                    url = System.IO.File.ReadAllText(apexUrlFilePath).Trim();
                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] Loaded URL: {url}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] File not found: {apexUrlFilePath}");
                }

                var response = new
                {
                    requestId = requestId,
                    action = "getApexEndpointUrlResponse",
                    success = !string.IsNullOrEmpty(url),
                    url = url
                };

                string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsString(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] Error getting APEX endpoint URL: {ex.Message}");

                var errorResponse = new
                {
                    requestId = requestId,
                    action = "getApexEndpointUrlResponse",
                    success = false,
                    error = ex.Message
                };
                string errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                wv.CoreWebView2.PostWebMessageAsString(errorJson);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles postEndpointToApex request from webview - POSTs endpoint data to APEX REST API
        /// </summary>
        private async Task HandlePostEndpointToApex(WebView2 wv, JsonElement root, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] ========================================");
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] Posting endpoint to APEX");
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] ========================================");

                // Get the endpoint data from the request
                if (!root.TryGetProperty("endpointData", out var endpointData))
                {
                    throw new Exception("No endpointData in request");
                }

                // Get APEX endpoint URL from file
                string settingsPath = EndpointConfigReader.GetSettingsPath();
                string apexUrlFilePath = System.IO.Path.Combine(settingsPath, "apexendpointurl.txt");

                if (!System.IO.File.Exists(apexUrlFilePath))
                {
                    throw new Exception("APEX endpoint URL file not found");
                }

                string baseUrl = System.IO.File.ReadAllText(apexUrlFilePath).Trim();
                if (string.IsNullOrEmpty(baseUrl))
                {
                    throw new Exception("APEX endpoint URL is empty");
                }

                // Append /save to the base URL
                string postUrl = baseUrl.EndsWith("/") ? baseUrl + "save" : baseUrl + "/save";
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] ========================================");
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] FULL URL: {postUrl}");
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] ========================================");

                // Serialize the endpoint data with indentation for debug
                string jsonBody = endpointData.GetRawText();

                // Pretty print the JSON for debug output
                try
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonBody);
                    var prettyOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    string prettyJson = System.Text.Json.JsonSerializer.Serialize(jsonDoc.RootElement, prettyOptions);
                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] ========================================");
                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] FULL JSON PAYLOAD:");
                    System.Diagnostics.Debug.WriteLine(prettyJson);
                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] ========================================");
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] FULL JSON PAYLOAD: {jsonBody}");
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                    var httpResponse = await httpClient.PostAsync(postUrl, content);
                    string responseBody = await httpResponse.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] Response status: {httpResponse.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] Response body: {responseBody}");

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var response = new
                        {
                            requestId = requestId,
                            action = "postEndpointToApexResponse",
                            success = true,
                            postUrl = postUrl,
                            data = responseBody
                        };
                        string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                        wv.CoreWebView2.PostWebMessageAsString(responseJson);
                    }
                    else
                    {
                        var response = new
                        {
                            requestId = requestId,
                            action = "postEndpointToApexResponse",
                            success = false,
                            postUrl = postUrl,
                            error = $"HTTP {(int)httpResponse.StatusCode}: {responseBody}"
                        };
                        string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                        wv.CoreWebView2.PostWebMessageAsString(responseJson);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApexEndpoint] Error posting to APEX: {ex.Message}");

                var errorResponse = new
                {
                    requestId = requestId,
                    action = "postEndpointToApexResponse",
                    success = false,
                    error = ex.Message
                };
                string errorJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
                wv.CoreWebView2.PostWebMessageAsString(errorJson);
            }
        }

        private async Task SendLoginInfoToInventoryAsync()
        {
            // Wait for page to load
            await Task.Delay(800);

            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null)
            {
                // Include organization info in the script
                string orgName = SessionManager.OrganizationDisplayName?.Replace("'", "\\'") ?? "";
                string orgCode = SessionManager.OrganizationCode?.Replace("'", "\\'") ?? "";
                long orgId = SessionManager.OrganizationId;

                // Serialize organizations list for the dropdown
                string orgsJson = "[]";
                if (SessionManager.AvailableOrganizations?.Count > 0)
                {
                    var orgsList = SessionManager.AvailableOrganizations.Select(o => new
                    {
                        organization_id = o.OrganizationId,
                        organization_name = o.OrganizationName,
                        organization_code = o.OrganizationCode,
                        classification_code = o.ClassificationCode,
                        status = o.Status,
                        master_organization_id = o.MasterOrganizationId
                    });
                    orgsJson = System.Text.Json.JsonSerializer.Serialize(orgsList);
                }

                string script = $@"
                    localStorage.setItem('wms_username', '{_loggedInUsername}');
                    localStorage.setItem('wms_instance', '{_loggedInInstance}');
                    localStorage.setItem('wms_org_name', '{orgName}');
                    localStorage.setItem('wms_org_code', '{orgCode}');
                    localStorage.setItem('wms_org_id', '{orgId}');
                    localStorage.setItem('wms_organizations', '{orgsJson.Replace("'", "\\'")}');
                    if(document.getElementById('usernameDisplay'))
                        document.getElementById('usernameDisplay').textContent = '{_loggedInUsername}';
                    if(document.getElementById('instanceDisplay'))
                        document.getElementById('instanceDisplay').textContent = '{_loggedInInstance}';

                    // Populate organizations dropdown if function exists
                    if(typeof setOrganizations === 'function') {{
                        setOrganizations('{orgsJson.Replace("'", "\\'")}');
                    }}
                ";

                await wv.CoreWebView2.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("[DEBUG] Sent login and org info to Inventory webview");
            }
        }

        private void UrlPanel_Paint(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            using (GraphicsPath path = GetRoundedRectangle(panel.ClientRectangle, 16))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(248, 248, 248)))
                {
                    e.Graphics.FillPath(brush, path);
                }

                using (Pen pen = new Pen(Color.FromArgb(220, 220, 220), 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private async Task LoadModule(string moduleCode, string apexHtmlUrl)
        {
            try
            {
                string localFileName = GetHtmlFileNameForModule(moduleCode);
                string localFileUrl = await _apexDownloader.DownloadApexHtmlFileAsync(apexHtmlUrl, localFileName);
                Navigate(localFileUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load module '{moduleCode}': {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetHtmlFileNameForModule(string moduleCode)
        {
            return moduleCode switch
            {
                "GL" => "general_ledger.html",
                "AR" => "accounts_receivable.html",
                "AP" => "accounts_payable.html",
                "OM" => "order_management.html",
                "FA" => "fixed_assets.html",
                "CA" => "cash_management.html",
                "WMS" => "warehouse.html",
                _ => $"{moduleCode.ToLower()}.html"
            };
        }

        private void ModuleButton_Paint(object sender, PaintEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            string moduleCode = btn.Tag?.ToString() ?? "";
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int iconX = 12;
            int iconY = btn.Height / 2;

            switch (moduleCode)
            {
                case "GL":
                    using (Brush blueBrush = new SolidBrush(Color.FromArgb(30, 120, 255)))
                    using (Pen bluePen = new Pen(Color.FromArgb(30, 120, 255), 1.5f))
                    {
                        e.Graphics.FillRectangle(blueBrush, iconX - 6, iconY + 1, 3, 4);
                        e.Graphics.FillRectangle(blueBrush, iconX - 2, iconY - 2, 3, 7);
                        e.Graphics.FillRectangle(blueBrush, iconX + 2, iconY - 4, 3, 9);
                    }
                    break;

                case "AR":
                    using (Font iconFont = new Font("Arial", 11, FontStyle.Bold))
                    using (Brush greenBrush = new SolidBrush(Color.FromArgb(50, 180, 50)))
                    {
                        e.Graphics.DrawString("$", iconFont, greenBrush, iconX - 5, iconY - 9);
                    }
                    break;

                case "AP":
                    using (Brush orangeBrush = new SolidBrush(Color.FromArgb(255, 120, 60)))
                    using (Pen orangePen = new Pen(Color.FromArgb(255, 120, 60), 1.5f))
                    {
                        Rectangle cardRect = new Rectangle(iconX - 6, iconY - 4, 12, 8);
                        e.Graphics.DrawRectangle(orangePen, cardRect);
                        e.Graphics.FillRectangle(orangeBrush, iconX - 6, iconY - 2, 12, 2);
                    }
                    break;

                case "OM":
                    using (Pen purplePen = new Pen(Color.FromArgb(140, 80, 220), 1.5f))
                    {
                        Rectangle boxRect = new Rectangle(iconX - 6, iconY - 4, 11, 8);
                        e.Graphics.DrawRectangle(purplePen, boxRect);
                        e.Graphics.DrawLine(purplePen, iconX - 6, iconY - 4, iconX, iconY - 1);
                        e.Graphics.DrawLine(purplePen, iconX + 5, iconY - 4, iconX, iconY - 1);
                        e.Graphics.DrawLine(purplePen, iconX, iconY - 1, iconX, iconY + 4);
                    }
                    break;

                case "FA":
                    using (Brush brownBrush = new SolidBrush(Color.FromArgb(160, 120, 80)))
                    {
                        Rectangle buildingRect = new Rectangle(iconX - 5, iconY - 5, 10, 10);
                        e.Graphics.FillRectangle(brownBrush, buildingRect);
                        using (Brush whiteBrush = new SolidBrush(Color.White))
                        {
                            e.Graphics.FillRectangle(whiteBrush, iconX - 4, iconY - 4, 2, 2);
                            e.Graphics.FillRectangle(whiteBrush, iconX + 1, iconY - 4, 2, 2);
                            e.Graphics.FillRectangle(whiteBrush, iconX - 4, iconY, 2, 2);
                            e.Graphics.FillRectangle(whiteBrush, iconX + 1, iconY, 2, 2);
                        }
                    }
                    break;

                case "CA":
                    using (Brush tealBrush = new SolidBrush(Color.FromArgb(60, 180, 160)))
                    {
                        e.Graphics.FillRectangle(tealBrush, iconX - 6, iconY - 1, 2, 6);
                        e.Graphics.FillRectangle(tealBrush, iconX - 2, iconY - 1, 2, 6);
                        e.Graphics.FillRectangle(tealBrush, iconX + 2, iconY - 1, 2, 6);
                        Point[] roof = new Point[]
                        {
                            new Point(iconX - 7, iconY - 1),
                            new Point(iconX, iconY - 5),
                            new Point(iconX + 5, iconY - 1)
                        };
                        e.Graphics.FillPolygon(tealBrush, roof);
                        e.Graphics.FillRectangle(tealBrush, iconX - 7, iconY + 5, 12, 1);
                    }
                    break;

                case "POS":
                    using (Pen orangePen = new Pen(Color.FromArgb(255, 140, 60), 1.5f))
                    using (Brush orangeBrush = new SolidBrush(Color.FromArgb(255, 140, 60)))
                    {
                        e.Graphics.DrawRectangle(orangePen, iconX - 4, iconY - 4, 8, 6);
                        e.Graphics.DrawLine(orangePen, iconX - 4, iconY - 4, iconX - 6, iconY - 6);
                        e.Graphics.FillEllipse(orangeBrush, iconX - 2, iconY + 3, 2, 2);
                        e.Graphics.FillEllipse(orangeBrush, iconX + 2, iconY + 3, 2, 2);
                    }
                    break;
            }

            Color textColor = Color.FromArgb(50, 50, 50);
            using (Font textFont = new Font("Segoe UI", 9, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(moduleCode, textFont, textBrush, iconX + 14, iconY, sf);
            }
        }

        private void SecurityIcon_Click(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null)
            {
                string url = wv.Source?.ToString() ?? "";
                string securityInfo;

                if (url.StartsWith("https://"))
                    securityInfo = "🔒 Secure connection (HTTPS)";
                else if (url.StartsWith("http://"))
                    securityInfo = "⚠️ Not secure (HTTP)";
                else if (url.StartsWith("file://"))
                    securityInfo = "📄 Local file";
                else
                    securityInfo = "ℹ️ Unknown protocol";

                MessageBox.Show(securityInfo, "Site Security", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddNewTab(string url)
        {
            CustomTabButton tabButton = new CustomTabButton
            {
                TabText = "New Tab",
                Width = 200,
                Height = 26,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White
            };

            WebView2 newWebView = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            tabButton.WebView = newWebView;
            tabButton.Click += TabButton_Click;
            tabButton.CloseClicked += TabButton_CloseClicked;

            int insertIndex = tabBar.Controls.Count - 1;
            tabBar.Controls.Add(tabButton);
            tabBar.Controls.SetChildIndex(tabButton, insertIndex);

            Panel contentPanel = this.Controls[0] as Panel;
            contentPanel.Controls.Add(newWebView);

            SelectTab(tabButton);
            InitializeWebView(newWebView, url);
        }

        private void TabButton_Click(object sender, EventArgs e)
        {
            SelectTab(sender as CustomTabButton);
        }

        private void SelectTab(CustomTabButton tabButton)
        {
            foreach (Control ctrl in tabBar.Controls)
            {
                if (ctrl is CustomTabButton tab)
                {
                    tab.IsSelected = false;
                    tab.BackColor = Color.FromArgb(50, 50, 50);
                    if (tab.WebView != null)
                        tab.WebView.Visible = false;
                }
            }

            tabButton.IsSelected = true;
            tabButton.BackColor = Color.FromArgb(70, 70, 70);
            if (tabButton.WebView != null)
            {
                tabButton.WebView.Visible = true;
                if (tabButton.WebView.CoreWebView2 != null)
                {
                    urlTextBox.Text = tabButton.WebView.Source?.ToString() ?? "";
                    UpdateNavigationButtons(tabButton.WebView);
                    UpdateSecurityIcon(tabButton.WebView);
                }
            }
        }

        private void TabButton_CloseClicked(object sender, EventArgs e)
        {
            CustomTabButton tabButton = sender as CustomTabButton;

            int index = tabBar.Controls.IndexOf(tabButton);
            CustomTabButton nextTab = null;

            if (index > 0)
                nextTab = tabBar.Controls[index - 1] as CustomTabButton;
            else if (tabBar.Controls.Count > 2)
                nextTab = tabBar.Controls[index + 1] as CustomTabButton;

            if (tabButton.WebView != null)
            {
                Panel contentPanel = this.Controls[0] as Panel;
                contentPanel.Controls.Remove(tabButton.WebView);
                tabButton.WebView.Dispose();
            }

            tabBar.Controls.Remove(tabButton);

            if (nextTab != null)
                SelectTab(nextTab);
            else
                this.Close();
        }

        private async void InitializeWebView(WebView2 wv, string url)
        {
            try
            {
                await wv.EnsureCoreWebView2Async(null);

                // CACHE FIX: Clear browser cache to ensure tabs load properly (preserve localStorage for login state)
                try
                {
                    await wv.CoreWebView2.Profile.ClearBrowsingDataAsync(
                        CoreWebView2BrowsingDataKinds.CacheStorage |
                        CoreWebView2BrowsingDataKinds.DiskCache);
                    System.Diagnostics.Debug.WriteLine("[CACHE] Browser cache cleared successfully (localStorage preserved)");
                }
                catch (Exception cacheEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[CACHE] Warning: Could not clear cache: {cacheEx.Message}");
                }

                wv.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                wv.CoreWebView2.Settings.AreDevToolsEnabled = true;
                wv.CoreWebView2.Settings.IsWebMessageEnabled = true;

                wv.CoreWebView2.WebMessageReceived += async (sender, args) =>
                {
                    try
                    {
                        string messageJson = args.WebMessageAsJson;
                        System.Diagnostics.Debug.WriteLine($"[C# RECEIVED] {messageJson}");

                        using (var doc = JsonDocument.Parse(messageJson))
                        {
                            var root = doc.RootElement;

                            // Handle double-serialized messages (when JS uses JSON.stringify with postMessage)
                            if (root.ValueKind == JsonValueKind.String)
                            {
                                string innerJson = root.GetString();
                                messageJson = innerJson; // FIX: Update messageJson to use the inner JSON
                                System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Double-serialized message detected, using inner JSON");
                                using (var innerDoc = JsonDocument.Parse(innerJson))
                                {
                                    root = innerDoc.RootElement.Clone();
                                }
                            }

                            // Check both "action" and "type" properties for compatibility
                            string action = root.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : "";
                            if (string.IsNullOrEmpty(action))
                            {
                                action = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "";
                            }
                            string requestId = root.TryGetProperty("requestId", out var reqIdProp) ? reqIdProp.GetString() : "";

                            if (string.IsNullOrEmpty(action))
                            {
                                System.Diagnostics.Debug.WriteLine($"[C#] No action/type in message, skipping");
                                return;
                            }

                            System.Diagnostics.Debug.WriteLine($"[C#] Action: {action}, RequestId: {requestId}");

                            switch (action)
                            {
                                case "loginSuccess":
                                    HandleLoginSuccess(root);
                                    break;

                                case "validateLogin":
                                    await HandleValidateLogin(wv, root, requestId);
                                    break;

                                case "executeGet":
                                    await HandleRestApiRequest(wv, messageJson, requestId);
                                    break;

                                case "executePost":
                                    await HandleRestApiPostRequest(wv, messageJson, requestId);
                                    break;

                                case "executeDelete":
                                    await HandleRestApiDeleteRequest(wv, messageJson, requestId);
                                    break;

                                case "claudeApiTest":
                                    await HandleClaudeApiTest(wv, messageJson, requestId);
                                    break;

                                case "claudeApiRequest":
                                    await HandleClaudeApiRequest(wv, messageJson, requestId);
                                    break;

                                // Print Management Cases
                                case "enableAutoPrint":
                                    await HandleEnableAutoPrint(wv, messageJson, requestId);
                                    break;

                                case "disableAutoPrint":
                                    await HandleDisableAutoPrint(wv, messageJson, requestId);
                                    break;

                                case "downloadOrderPdf":
                                    await HandleDownloadOrderPdf(wv, messageJson, requestId);
                                    break;

                                // 🔧 NEW: Check if PDF exists locally
                                case "checkPdfExists":
                                    await HandleCheckPdfExists(wv, messageJson, requestId);
                                    break;

                                case "printOrder":
                                    await HandlePrintOrder(wv, messageJson, requestId);
                                    break;

                                case "printSalesOrder":
                                    await HandlePrintSalesOrder(wv, messageJson, requestId);
                                    break;

                                case "getPrintJobs":
                                    await HandleGetPrintJobs(wv, messageJson, requestId);
                                    break;

                                case "retryFailedJobs":
                                    await HandleRetryFailedJobs(wv, messageJson, requestId);
                                    break;

                                case "configurePrinter":
                                    await HandleConfigurePrinter(wv, messageJson, requestId);
                                    break;

                                case "getPrinterConfig":
                                    await HandleGetPrinterConfig(wv, messageJson, requestId);
                                    break;

                                case "getInstalledPrinters":
                                    await HandleGetInstalledPrinters(wv, messageJson, requestId);
                                    break;

                                case "testPrinter":
                                    await HandleTestPrinter(wv, messageJson, requestId);
                                    break;

                                case "discoverBluetoothPrinters":
                                    await HandleDiscoverBluetoothPrinters(wv, messageJson, requestId);
                                    break;

                                case "discoverWifiPrinters":
                                    await HandleDiscoverWifiPrinters(wv, messageJson, requestId);
                                    break;

                                case "setInstanceSetting":
                                    await HandleSetInstanceSetting(wv, messageJson, requestId);
                                    break;

                                case "getAllPrintJobs":
                                    await HandleGetAllPrintJobs(wv, messageJson, requestId);
                                    break;

                                case "getPdfAsBase64":
                                    await HandleGetPdfAsBase64(wv, messageJson, requestId);
                                    break;

                                case "openFileInExplorer":
                                    await HandleOpenFileInExplorer(wv, messageJson, requestId);
                                    break;

                                // 🔧 NEW: Open PDF file with default viewer
                                case "openPdfFile":
                                    await HandleOpenPdfFile(wv, messageJson, requestId);
                                    break;

                                // 🔧 NEW: Print Store Transaction Report
                                case "printStoreTransaction":
                                    await HandlePrintStoreTransaction(wv, messageJson, requestId);
                                    break;

                                // 🔧 NEW: Run SOAP Report (Generic handler for any Oracle BI Publisher report)
                                case "runSoapReport":
                                    await HandleRunSoapReport(wv, messageJson, requestId);
                                    break;

                                // Distribution System Cases
                                case "check-distribution-folder":
                                    await HandleCheckDistributionFolder(wv, messageJson, requestId);
                                    break;

                                case "download-distribution":
                                    await HandleDownloadDistribution(wv, messageJson, requestId);
                                    break;

                                case "launch-wms-module":
                                    await HandleLaunchWMSModule(wv, messageJson, requestId);
                                    break;

                                case "loadLocalFile":
                                    await HandleLoadLocalFile(wv, messageJson, requestId);
                                    break;

                                case "logout":
                                    HandleLogout();
                                    break;

                                // Organization change from inventory page
                                case "organizationChanged":
                                    HandleOrganizationChanged(root);
                                    break;

                                // Get endpoint for integration code
                                case "getEndpoint":
                                    await HandleGetEndpoint(wv, root, requestId);
                                    break;

                                // Get all endpoints from XML file
                                case "getAllEndpoints":
                                    await HandleGetAllEndpoints(wv, requestId);
                                    break;

                                // Save all endpoints to XML file
                                case "saveAllEndpoints":
                                    await HandleSaveAllEndpoints(wv, root, requestId);
                                    break;

                                // Get APEX endpoint URL from file
                                case "getApexEndpointUrl":
                                    await HandleGetApexEndpointUrl(wv, requestId);
                                    break;

                                // POST endpoint to APEX REST API
                                case "postEndpointToApex":
                                    await HandlePostEndpointToApex(wv, root, requestId);
                                    break;

                                // MRA Interface Processing
                                case "processMRAInterface":
                                    await HandleProcessMRAInterface(wv, messageJson, requestId);
                                    break;

                                // Release Manager
                                case "createRelease":
                                    await HandleCreateRelease(wv, messageJson, requestId);
                                    break;

                                case "getVersionInfo":
                                    await HandleGetVersionInfo(wv, messageJson, requestId);
                                    break;

                                case "getPickersView":
                                    await HandleGetPickersView(wv, messageJson, requestId);
                                    break;

                                case "postToTeams":
                                    await HandlePostToTeams(wv, messageJson, requestId);
                                    break;

                                case "getOutlookEmail":
                                    await HandleGetOutlookEmail(wv, messageJson, requestId);
                                    break;

                                case "sendOutlookEmail":
                                    await HandleSendOutlookEmail(wv, messageJson, requestId);
                                    break;

                                case "sendSmtpEmail":
                                    await HandleSendSmtpEmail(wv, messageJson, requestId);
                                    break;

                                case "testSmtpConnection":
                                    await HandleTestSmtpConnection(wv, messageJson, requestId);
                                    break;

                                // Endpoint Testing
                                case "testEndpoint":
                                    await HandleTestEndpoint(wv, messageJson, requestId);
                                    break;

                                // Get all instances from APEX
                                case "getAllInstances":
                                    await HandleGetAllInstances(wv, requestId);
                                    break;

                                default:
                                    System.Diagnostics.Debug.WriteLine($"[C#] Unknown action: {action}");
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C# ERROR] Message processing failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[C# ERROR] Stack trace: {ex.StackTrace}");
                    }
                };

                wv.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                wv.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                wv.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                wv.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing browser: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== REST API HANDLER ==========

        private async Task HandleRestApiRequest(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<RestApiWebMessage>(  // ← CHANGED HERE
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Processing executeGet request: {message.FullUrl}");

                // Check if credentials are provided
                bool hasCredentials = !string.IsNullOrEmpty(message.Username) && !string.IsNullOrEmpty(message.Password);
                if (hasCredentials)
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] Using Basic Authentication for user: {message.Username}");
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(300);

                    // Create request message instead of using GetAsync directly
                    var request = new HttpRequestMessage(HttpMethod.Get, message.FullUrl);

                    // Add Basic Authentication header if credentials are provided
                    if (hasCredentials)
                    {
                        string credentials = Convert.ToBase64String(
                            System.Text.Encoding.ASCII.GetBytes($"{message.Username}:{message.Password}")
                        );
                        request.Headers.Add("Authorization", $"Basic {credentials}");
                        System.Diagnostics.Debug.WriteLine($"[C#] Added Authorization header with Basic authentication");
                    }

                    System.Diagnostics.Debug.WriteLine($"[C#] Making GET request to: {message.FullUrl}");

                    // Use SendAsync instead of GetAsync to support custom headers
                    var response = await httpClient.SendAsync(request);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[C#] REST call completed. Status: {response.StatusCode}, Length: {responseContent.Length}");

                    // Log error responses for debugging
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C# ERROR] HTTP {response.StatusCode}: {response.ReasonPhrase}");
                        System.Diagnostics.Debug.WriteLine($"[C# ERROR] Response body: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                    }

                    // Log first 200 chars of successful responses for debugging
                    if (response.IsSuccessStatusCode && responseContent.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C#] Response preview: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...");
                    }

                    var resultMessage = new
                    {
                        action = "restResponse",
                        requestId = requestId,
                        success = true,
                        data = responseContent
                    };

                    string resultJson = JsonSerializer.Serialize(resultMessage);
                    System.Diagnostics.Debug.WriteLine($"[C#] Sending response back to JS. RequestId: {requestId}, DataLength: {responseContent.Length}");
                    System.Diagnostics.Debug.WriteLine($"[C#] Response JSON (first 200 chars): {resultJson.Substring(0, Math.Min(200, resultJson.Length))}");
                    wv.CoreWebView2.PostWebMessageAsJson(resultJson);
                    System.Diagnostics.Debug.WriteLine($"[C#] ✓ Response sent successfully to WebView2");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] REST call failed: {ex.Message}");

                var errorMessage = new
                {
                    action = "error",
                    requestId = requestId,
                    data = new { message = ex.Message }
                };

                string errorJson = JsonSerializer.Serialize(errorMessage);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);
            }
        }

        // ========== TEST ENDPOINT HANDLER ==========

        private async Task HandleTestEndpoint(WebView2 wv, string messageJson, string requestId)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;

                    // Handle double-serialized messages (safeguard)
                    if (root.ValueKind == JsonValueKind.String)
                    {
                        string innerJson = root.GetString();
                        System.Diagnostics.Debug.WriteLine($"[C# DEBUG] HandleTestEndpoint: Detected double-serialized, parsing inner JSON");
                        using (var innerDoc = JsonDocument.Parse(innerJson))
                        {
                            root = innerDoc.RootElement.Clone();
                        }
                    }

                    string url = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : "";
                    string username = root.TryGetProperty("username", out var userProp) ? userProp.GetString() : "";
                    string password = root.TryGetProperty("password", out var passProp) ? passProp.GetString() : "";
                    bool noAuth = root.TryGetProperty("noAuth", out var noAuthProp) && noAuthProp.GetBoolean();
                    string integrationCode = root.TryGetProperty("integrationCode", out var icProp) ? icProp.GetString() : "";

                    // DEBUG: Log all received values
                    System.Diagnostics.Debug.WriteLine($"[C# DEBUG] ========== TEST ENDPOINT REQUEST ==========");
                    System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Raw messageJson: {messageJson}");
                    System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Parsed URL: {url}");
                    System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Parsed username: '{username}' (length: {username?.Length ?? 0})");
                    System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Parsed password: '{(string.IsNullOrEmpty(password) ? "(empty)" : "***")}' (length: {password?.Length ?? 0})");
                    System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Parsed noAuth: {noAuth}");
                    System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Parsed integrationCode: {integrationCode}");

                    System.Diagnostics.Debug.WriteLine($"[C#] Testing endpoint: {url}");
                    System.Diagnostics.Debug.WriteLine($"[C#] Integration Code: {integrationCode}, NoAuth: {noAuth}");

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(30);

                        var request = new HttpRequestMessage(HttpMethod.Get, url);

                        // Add Basic Authentication header if credentials are provided
                        bool hasUsername = !string.IsNullOrEmpty(username);
                        bool hasPassword = !string.IsNullOrEmpty(password);
                        System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Auth check: noAuth={noAuth}, hasUsername={hasUsername}, hasPassword={hasPassword}");

                        if (!noAuth && hasUsername && hasPassword)
                        {
                            string rawCredentials = $"{username}:{password}";
                            string credentials = Convert.ToBase64String(
                                System.Text.Encoding.ASCII.GetBytes(rawCredentials)
                            );
                            request.Headers.Add("Authorization", $"Basic {credentials}");
                            System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Added Basic Auth header for user: {username}");
                            System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Raw credentials format: '{username}:***' (total length: {rawCredentials.Length})");
                            System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Base64 credentials (first 10 chars): {credentials.Substring(0, Math.Min(10, credentials.Length))}...");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[C# DEBUG] NOT adding auth header - noAuth:{noAuth}, hasUser:{hasUsername}, hasPass:{hasPassword}");
                        }

                        var response = await httpClient.SendAsync(request);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        stopwatch.Stop();

                        System.Diagnostics.Debug.WriteLine($"[C#] Test completed. Status: {response.StatusCode}, Duration: {stopwatch.ElapsedMilliseconds}ms");

                        // Log response body for debugging auth failures
                        if (!response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Response Headers:");
                            foreach (var header in response.Headers)
                            {
                                System.Diagnostics.Debug.WriteLine($"[C# DEBUG]   {header.Key}: {string.Join(", ", header.Value)}");
                            }
                            System.Diagnostics.Debug.WriteLine($"[C# DEBUG] Response Body (first 500 chars): {(responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent)}");
                        }

                        // Send result back to JavaScript
                        var resultMessage = new
                        {
                            action = "testEndpointResult",
                            requestId = requestId,
                            success = response.IsSuccessStatusCode,
                            statusCode = (int)response.StatusCode,
                            statusText = response.ReasonPhrase,
                            duration = stopwatch.ElapsedMilliseconds,
                            data = responseContent,
                            error = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                        };

                        string resultJson = JsonSerializer.Serialize(resultMessage);
                        wv.CoreWebView2.PostWebMessageAsJson(resultJson);

                        // Also call handleTestResult in JavaScript
                        string jsCall = $"if(typeof handleTestResult === 'function') handleTestResult({resultJson});";
                        await wv.CoreWebView2.ExecuteScriptAsync(jsCall);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                var errorMessage = new
                {
                    action = "testEndpointResult",
                    requestId = requestId,
                    success = false,
                    statusCode = 0,
                    statusText = "Timeout",
                    duration = stopwatch.ElapsedMilliseconds,
                    data = "",
                    error = "Request timed out after 30 seconds"
                };

                string errorJson = JsonSerializer.Serialize(errorMessage);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);

                string jsCall = $"if(typeof handleTestResult === 'function') handleTestResult({errorJson});";
                await wv.CoreWebView2.ExecuteScriptAsync(jsCall);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Test endpoint failed: {ex.Message}");

                var errorMessage = new
                {
                    action = "testEndpointResult",
                    requestId = requestId,
                    success = false,
                    statusCode = 0,
                    statusText = "Error",
                    duration = stopwatch.ElapsedMilliseconds,
                    data = "",
                    error = ex.Message
                };

                string errorJson = JsonSerializer.Serialize(errorMessage);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);

                string jsCall = $"if(typeof handleTestResult === 'function') handleTestResult({errorJson});";
                await wv.CoreWebView2.ExecuteScriptAsync(jsCall);
            }
        }

        private async Task HandleRestApiPostRequest(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<RestApiPostWebMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                string method = message.Method?.ToUpper() ?? "POST";
                System.Diagnostics.Debug.WriteLine($"[C#] Processing {method} request: {message.FullUrl}");
                System.Diagnostics.Debug.WriteLine($"[C#] Body: {message.Body}");

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(300);

                    HttpResponseMessage response;

                    if (method == "POST")
                    {
                        var content = new StringContent(
                            message.Body ?? "{}",
                            Encoding.UTF8,
                            "application/json"
                        );
                        response = await httpClient.PostAsync(message.FullUrl, content);
                    }
                    else if (method == "PUT")
                    {
                        var content = new StringContent(
                            message.Body ?? "{}",
                            Encoding.UTF8,
                            "application/json"
                        );
                        response = await httpClient.PutAsync(message.FullUrl, content);
                    }
                    else if (method == "DELETE")
                    {
                        response = await httpClient.DeleteAsync(message.FullUrl);
                    }
                    else
                    {
                        throw new Exception($"Unsupported HTTP method: {method}");
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[C#] REST {method} completed. Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[C#] Response: {responseContent}");

                    var resultMessage = new
                    {
                        action = "restResponse",
                        requestId = requestId,
                        success = true,
                        data = responseContent
                    };

                    string resultJson = JsonSerializer.Serialize(resultMessage);
                    System.Diagnostics.Debug.WriteLine($"[C#] Sending response back to JS. RequestId: {requestId}, DataLength: {responseContent.Length}");
                    System.Diagnostics.Debug.WriteLine($"[C#] Response JSON (first 200 chars): {resultJson.Substring(0, Math.Min(200, resultJson.Length))}");
                    wv.CoreWebView2.PostWebMessageAsJson(resultJson);
                    System.Diagnostics.Debug.WriteLine($"[C#] ✓ Response sent successfully to WebView2");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] REST call failed: {ex.Message}");

                var errorMessage = new
                {
                    action = "error",
                    requestId = requestId,
                    data = new { message = ex.Message }
                };

                string errorJson = JsonSerializer.Serialize(errorMessage);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);
            }
        }

        private async Task HandleRestApiDeleteRequest(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<RestApiWebMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Processing executeDelete request: {message.FullUrl}");

                // Check if credentials are provided
                bool hasCredentials = !string.IsNullOrEmpty(message.Username) && !string.IsNullOrEmpty(message.Password);
                if (hasCredentials)
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] Using Basic Authentication for user: {message.Username}");
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(300);

                    // Create request message
                    var request = new HttpRequestMessage(HttpMethod.Delete, message.FullUrl);

                    // Add Basic Authentication header if credentials are provided
                    if (hasCredentials)
                    {
                        string credentials = Convert.ToBase64String(
                            System.Text.Encoding.ASCII.GetBytes($"{message.Username}:{message.Password}")
                        );
                        request.Headers.Add("Authorization", $"Basic {credentials}");
                        System.Diagnostics.Debug.WriteLine($"[C#] Added Authorization header with Basic authentication");
                    }

                    System.Diagnostics.Debug.WriteLine($"[C#] Making DELETE request to: {message.FullUrl}");

                    // Use SendAsync to support custom headers
                    var response = await httpClient.SendAsync(request);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine($"[C#] DELETE call completed. Status: {response.StatusCode}, Length: {responseContent.Length}");

                    // Log error responses for debugging
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C# ERROR] HTTP {response.StatusCode}: {response.ReasonPhrase}");
                        System.Diagnostics.Debug.WriteLine($"[C# ERROR] Response body: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
                    }

                    // Log first 200 chars of successful responses for debugging
                    if (response.IsSuccessStatusCode && responseContent.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C#] Response preview: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...");
                    }

                    var resultMessage = new
                    {
                        action = "restResponse",
                        requestId = requestId,
                        success = true,
                        data = responseContent
                    };

                    string resultJson = JsonSerializer.Serialize(resultMessage);
                    System.Diagnostics.Debug.WriteLine($"[C#] Sending response back to JS. RequestId: {requestId}, DataLength: {responseContent.Length}");
                    wv.CoreWebView2.PostWebMessageAsJson(resultJson);
                    System.Diagnostics.Debug.WriteLine($"[C#] ✓ DELETE response sent successfully to WebView2");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] DELETE call failed: {ex.Message}");

                var errorMessage = new
                {
                    action = "error",
                    requestId = requestId,
                    data = new { message = ex.Message }
                };

                string errorJson = JsonSerializer.Serialize(errorMessage);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);
            }
        }

        // ========== CLAUDE API HANDLERS ==========
        private async Task HandleClaudeApiTest(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ClaudeApiTestMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Testing Claude API key...");

                var testResult = await _claudeApiHandler.TestApiKeyAsync(message.ApiKey);

                var resultMessage = new
                {
                    action = "claudeResponse",
                    requestId = requestId,
                    success = testResult.Success,
                    data = testResult.Success ? "API key is valid" : null,
                    error = testResult.Success ? null : testResult.Message
                };

                string resultJson = JsonSerializer.Serialize(resultMessage);
                wv.CoreWebView2.PostWebMessageAsJson(resultJson);

                System.Diagnostics.Debug.WriteLine($"[C#] Claude API test result: {testResult.Success}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Claude API test failed: {ex.Message}");

                var errorMessage = new
                {
                    action = "claudeResponse",
                    requestId = requestId,
                    success = false,
                    error = ex.Message
                };

                string errorJson = JsonSerializer.Serialize(errorMessage);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);
            }
        }

        private async Task HandleClaudeApiRequest(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ClaudeApiRequestMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Processing Claude API request: {message.UserQuery}");

                var queryResult = await _claudeApiHandler.QueryClaudeAsync(
                    message.ApiKey,
                    message.UserQuery,
                    message.SystemPrompt,
                    message.DataJson
                );

                var resultMessage = new
                {
                    action = "claudeResponse",
                    requestId = requestId,
                    success = queryResult.Success,
                    data = queryResult.Success ? queryResult.ResponseJson : null,
                    error = queryResult.Success ? null : queryResult.Error
                };

                string resultJson = JsonSerializer.Serialize(resultMessage);
                wv.CoreWebView2.PostWebMessageAsJson(resultJson);

                System.Diagnostics.Debug.WriteLine($"[C#] Claude API query completed: {queryResult.Success}");

                if (queryResult.Success)
                {
                    this.Invoke(new Action(() =>
                    {
                        PromptSaveAfterResponse(message.UserQuery, queryResult.ResponseJson);
                    }));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Claude API request failed: {ex.Message}");

                var errorMessage = new
                {
                    action = "claudeResponse",
                    requestId = requestId,
                    success = false,
                    error = ex.Message
                };

                string errorJson = JsonSerializer.Serialize(errorMessage);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);
            }
        }

        private void PromptSaveAfterResponse(string prompt, string responseJson)
        {
            try
            {
                string responseText = ExtractClaudeResponseText(responseJson);

                var result = MessageBox.Show(
                    "💾 Would you like to save this prompt and response?",
                    "Save Prompt",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    bool saved = _promptHistoryManager.SavePrompt(prompt, responseText, "WMS Query");

                    if (saved)
                    {
                        MessageBox.Show(
                            "✅ Prompt saved successfully!\n\nView saved prompts by clicking the 📚 button.",
                            "Saved",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "❌ Failed to save prompt.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to prompt save: {ex.Message}");
            }
        }

        private string ExtractClaudeResponseText(string responseJson)
        {
            try
            {
                using (var doc = JsonDocument.Parse(responseJson))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("content", out var content) && content.GetArrayLength() > 0)
                    {
                        var firstContent = content[0];
                        if (firstContent.TryGetProperty("text", out var text))
                        {
                            return text.GetString();
                        }
                    }
                }
            }
            catch { }

            return responseJson;
        }

        // ========== PRINT MANAGEMENT HANDLERS ==========

        private async Task HandleEnableAutoPrint(WebView2 wv, string messageJson, string requestId)
        {
            //MessageBox.Show($"HandleEnableAutoPrint called!\n\nRequestId: {requestId}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);

            try
            {
                System.Diagnostics.Debug.WriteLine("====================================");
                System.Diagnostics.Debug.WriteLine($"[C#] ⭐ HandleEnableAutoPrint STARTED");
                System.Diagnostics.Debug.WriteLine($"[C#] RequestId: {requestId}");
                System.Diagnostics.Debug.WriteLine($"[C#] Message JSON: {messageJson}");
                System.Diagnostics.Debug.WriteLine("====================================");

                var message = System.Text.Json.JsonSerializer.Deserialize<ToggleAutoPrintMessage>(
                    messageJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Deserialized Message:");
                System.Diagnostics.Debug.WriteLine($"[C#]   - TripId: {message.TripId}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - TripDate: {message.TripDate}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Enabled: {message.Enabled}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Orders Count: {message.Orders?.Count ?? 0}");

                if (message.Orders != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] Order Details:");
                    foreach (var order in message.Orders)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C#]   - Order: {order.OrderNumber}, Customer: {order.CustomerName}, Account: {order.AccountNumber}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[C#] Calling _printJobManager.EnableAutoPrintAsync...");

                var result = await _printJobManager.EnableAutoPrintAsync(
                    message.TripId,
                    message.TripDate,
                    message.Orders
                );

                System.Diagnostics.Debug.WriteLine($"[C#] EnableAutoPrintAsync Result:");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Success: {result.Success}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Message: {result.Message}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - TripConfig: {(result.TripConfig != null ? "Not Null" : "NULL")}");

                if (result.TripConfig != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[C#]   - TripConfig.Orders.Count: {result.TripConfig.Orders?.Count ?? 0}");
                }

                var response = new
                {
                    action = "autoPrintResponse",
                    requestId = requestId,
                    success = result.Success,
                    message = result.Message,
                    data = result.TripConfig
                };

                string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                System.Diagnostics.Debug.WriteLine($"[C#] Sending response to JavaScript:");
                System.Diagnostics.Debug.WriteLine($"[C#] {responseJson}");

                wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                System.Diagnostics.Debug.WriteLine($"[C#] ✅ HandleEnableAutoPrint COMPLETED");
                System.Diagnostics.Debug.WriteLine("====================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] ❌ HandleEnableAutoPrint FAILED");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Stack Trace: {ex.StackTrace}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }
        private async Task HandleDisableAutoPrint(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ToggleAutoPrintMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Disabling auto-print for trip {message.TripId}");

                bool success = _printJobManager.DisableAutoPrint(message.TripId, message.TripDate);

                var response = new
                {
                    action = "autoPrintResponse",
                    requestId = requestId,
                    success = success,
                    message = success ? "Auto-print disabled" : "Failed to disable auto-print"
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Disable auto-print failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        // 2. ADD THESE METHODS AT THE END OF Form1 CLASS:

        /// <summary>
        /// Get all print jobs with actual file status
        /// </summary>
        private async Task HandleGetAllPrintJobs(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Getting all print jobs...");

                // Get all print jobs from PrintJobManager
                var jobs = await PrintJobManager.GetAllPrintJobsAsync();

                System.Diagnostics.Debug.WriteLine($"[C#] Found {jobs.Count} print jobs");

                // Check actual file status for each job
                foreach (var job in jobs)
                {
                    // Check if PDF file actually exists
                    if (!string.IsNullOrEmpty(job.FilePath) && File.Exists(job.FilePath))
                    {
                        // File exists - update status if needed
                        if (job.Status == PrintJobStatus.Pending ||
                            job.Status == PrintJobStatus.Failed)
                        {
                            job.Status = PrintJobStatus.Completed;
                        }
                    }
                    else
                    {
                        // File doesn't exist - clear file path
                        job.FilePath = null;

                        // If status was completed but file is missing, mark as pending
                        if (job.Status == PrintJobStatus.Completed)
                        {
                            job.Status = PrintJobStatus.Pending;
                        }
                    }
                }

                // Convert to JSON-friendly format
                var jobsData = jobs.Select(j => new
                {
                    orderNumber = j.OrderNumber,
                    tripId = j.TripId,
                    tripDate = j.TripDate,
                    status = j.Status.ToString(),
                    filePath = j.FilePath ?? "",
                    errorMessage = j.ErrorMessage ?? "",
                    createdAt = j.CreatedAt.ToString("o"),
                    updatedAt = j.UpdatedAt?.ToString("o") ?? ""
                }).ToList();

                var response = new
                {
                    action = "getAllPrintJobsResponse",
                    requestId = requestId,
                    success = true,
                    data = new
                    {
                        jobs = jobsData,
                        count = jobsData.Count
                    }
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                System.Diagnostics.Debug.WriteLine($"[C#] Sent {jobsData.Count} print jobs to UI");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Get all print jobs failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        /// <summary>
        /// Get PDF file as Base64 for preview
        /// </summary>
        private async Task HandleGetPdfAsBase64(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Getting PDF as Base64...");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string filePath = root.GetProperty("filePath").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] File path: {filePath}");

                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"PDF file not found: {filePath}");
                    }

                    // Read file and convert to Base64
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    string base64String = Convert.ToBase64String(fileBytes);

                    System.Diagnostics.Debug.WriteLine($"[C#] PDF size: {fileBytes.Length} bytes");
                    System.Diagnostics.Debug.WriteLine($"[C#] Base64 length: {base64String.Length} chars");

                    var response = new
                    {
                        action = "getPdfAsBase64Response",
                        requestId = requestId,
                        success = true,
                        data = new
                        {
                            base64 = base64String,
                            fileName = Path.GetFileName(filePath),
                            fileSize = fileBytes.Length
                        }
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] PDF sent to UI for preview");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Get PDF as Base64 failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        /// <summary>
        /// Open file location in Windows Explorer
        /// </summary>
        private async Task HandleOpenFileInExplorer(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Opening file in Explorer...");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string filePath = root.GetProperty("filePath").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] File path: {filePath}");

                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"File not found: {filePath}");
                    }

                    // Open Windows Explorer and select the file
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");

                    var response = new
                    {
                        action = "openFileInExplorerResponse",
                        requestId = requestId,
                        success = true,
                        message = "File location opened in Explorer"
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] Explorer opened");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Open file in Explorer failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        // 🔧 NEW: Open PDF file with default PDF viewer
        private async Task HandleOpenPdfFile(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Opening PDF file with default viewer...");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string filePath = root.GetProperty("filePath").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] File path: {filePath}");

                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"PDF file not found: {filePath}");
                    }

                    // Open PDF with default application (e.g., Adobe Reader, Edge, etc.)
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true  // This uses the Windows file association
                    };

                    System.Diagnostics.Process.Start(processStartInfo);

                    var response = new
                    {
                        action = "openPdfFileResponse",
                        requestId = requestId,
                        success = true,
                        message = "PDF opened with default viewer"
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ PDF opened successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Open PDF file failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        // Load local HTML file content (for Sync module external pages)
        private async Task HandleLoadLocalFile(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Loading local file...");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string filePath = root.GetProperty("filePath").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] File path: {filePath}");

                    // Resolve path relative to sync folder
                    string repoRoot = GetWebFilesBasePath();
                    string fullPath = Path.GetFullPath(Path.Combine(repoRoot, "sync", filePath));

                    System.Diagnostics.Debug.WriteLine($"[C#] Full path: {fullPath}");

                    if (!File.Exists(fullPath))
                    {
                        throw new FileNotFoundException($"File not found: {fullPath}");
                    }

                    // Read file content
                    string content = await File.ReadAllTextAsync(fullPath);

                    var response = new
                    {
                        action = "loadLocalFileResponse",
                        requestId = requestId,
                        success = true,
                        content = content,
                        filePath = filePath
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ File loaded successfully: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Load local file failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        // ========== MRA INTERFACE HANDLER ==========

        private async Task HandleProcessMRAInterface(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] ========== MRA INTERFACE REQUEST ==========");
                System.Diagnostics.Debug.WriteLine($"[C#] Raw messageJson: {messageJson}");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string orderNumber = root.GetProperty("orderNumber").GetString();
                    string fusionUsername = root.GetProperty("fusionUsername").GetString();
                    string fusionPassword = root.GetProperty("fusionPassword").GetString();
                    string instance = root.GetProperty("instance").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] Parsed values:");
                    System.Diagnostics.Debug.WriteLine($"[C#]   - Order Number: {orderNumber}");
                    System.Diagnostics.Debug.WriteLine($"[C#]   - Username: {fusionUsername}");
                    System.Diagnostics.Debug.WriteLine($"[C#]   - Instance: '{instance}'");
                    System.Diagnostics.Debug.WriteLine($"[C#] ============================================");

                    // Create MRA processor
                    var mraProcessor = new WMSApp.MRA.MRAProcessor(
                        fusionUsername,
                        fusionPassword,
                        instance
                    );

                    // Process with progress updates and data callbacks
                    var result = await mraProcessor.ProcessMRAInterfaceAsync(
                        orderNumber,
                        // Progress callback
                        (message, step) =>
                        {
                            // Send progress update to JavaScript
                            var progressUpdate = new
                            {
                                action = "mraProcessingProgress",
                                requestId = requestId,
                                step = step.ToString(),
                                message = message
                            };

                            string progressJson = JsonSerializer.Serialize(progressUpdate);
                            wv.CoreWebView2.PostWebMessageAsJson(progressJson);
                            System.Diagnostics.Debug.WriteLine($"[C#] MRA Progress: {step} - {message}");
                        },
                        // Order data callback (for Tab 1)
                        (headerData, linesData) =>
                        {
                            var orderDataMsg = new
                            {
                                action = "mraOrderData",
                                requestId = requestId,
                                header = headerData,
                                lines = linesData
                            };

                            string orderDataJson = JsonSerializer.Serialize(orderDataMsg);
                            wv.CoreWebView2.PostWebMessageAsJson(orderDataJson);
                            System.Diagnostics.Debug.WriteLine($"[C#] MRA Order Data sent to JS");
                        },
                        // MRA Request callback (for Tab 2)
                        (endpoint, requestObj) =>
                        {
                            var requestDataMsg = new
                            {
                                action = "mraRequestData",
                                requestId = requestId,
                                endpoint = endpoint,
                                request = requestObj
                            };

                            string requestDataJson = JsonSerializer.Serialize(requestDataMsg);
                            wv.CoreWebView2.PostWebMessageAsJson(requestDataJson);
                            System.Diagnostics.Debug.WriteLine($"[C#] MRA Request Data sent to JS");
                        },
                        // MRA Response callback (for Tab 2)
                        (success, responseObj) =>
                        {
                            var responseDataMsg = new
                            {
                                action = "mraResponseData",
                                requestId = requestId,
                                success = success,
                                response = responseObj
                            };

                            string responseDataJson = JsonSerializer.Serialize(responseDataMsg);
                            wv.CoreWebView2.PostWebMessageAsJson(responseDataJson);
                            System.Diagnostics.Debug.WriteLine($"[C#] MRA Response Data sent to JS");
                        }
                    );

                    // Send final result
                    var response = new
                    {
                        action = "processMRAInterfaceResponse",
                        requestId = requestId,
                        success = result.Success,
                        message = result.Message,
                        irnCode = result.IrnCode,
                        qrCodeBase64 = result.QrCodeBase64,
                        orderNumber = result.OrderNumber,
                        headerId = result.HeaderId,
                        currentStep = result.CurrentStep.ToString(),
                        errorDetails = result.ErrorDetails
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ MRA Processing completed: {result.Success}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] MRA processing failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Stack trace: {ex.StackTrace}");
                SendErrorResponse(wv, requestId, $"MRA processing error: {ex.Message}");
            }
        }

        // ========== DISTRIBUTION SYSTEM HANDLERS ==========

        private async Task HandleCheckDistributionFolder(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Checking distribution folder...");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string folder = root.GetProperty("folder").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] Distribution folder: {folder}");

                    // Check if folder exists and contains index.html
                    bool exists = Directory.Exists(folder) &&
                                  File.Exists(Path.Combine(folder, "index.html"));

                    System.Diagnostics.Debug.WriteLine($"[C#] Folder exists with index.html: {exists}");

                    var response = new
                    {
                        type = "distribution-folder-exists",
                        exists = exists,
                        folder = folder,
                        requestId = requestId
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ Distribution folder check complete");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Check distribution folder failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        private async Task HandleDownloadDistribution(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Starting distribution download...");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string version = root.GetProperty("version").GetString();
                    string packageUrl = root.GetProperty("packageUrl").GetString();
                    string extractTo = root.GetProperty("extractTo").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] Version: {version}");
                    System.Diagnostics.Debug.WriteLine($"[C#] Package URL: {packageUrl}");
                    System.Diagnostics.Debug.WriteLine($"[C#] Extract to: {extractTo}");

                    // Create temp path for download
                    string tempZipPath = Path.Combine(Path.GetTempPath(), $"wms-distribution-{version}.zip");

                    System.Diagnostics.Debug.WriteLine($"[C#] Temp ZIP path: {tempZipPath}");

                    // Download file with progress reporting
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadProgressChanged += (s, e) =>
                        {
                            // Send progress updates to JavaScript
                            var progressResponse = new
                            {
                                type = "distribution-download-progress",
                                percent = e.ProgressPercentage,
                                bytesReceived = e.BytesReceived,
                                totalBytes = e.TotalBytesToReceive,
                                requestId = requestId
                            };

                            string progressJson = JsonSerializer.Serialize(progressResponse);
                            wv.CoreWebView2.PostWebMessageAsJson(progressJson);
                        };

                        System.Diagnostics.Debug.WriteLine($"[C#] Downloading from GitHub...");
                        await client.DownloadFileTaskAsync(packageUrl, tempZipPath);
                        System.Diagnostics.Debug.WriteLine($"[C#] Download complete!");
                    }

                    // Create extraction folder if it doesn't exist
                    if (!Directory.Exists(extractTo))
                    {
                        System.Diagnostics.Debug.WriteLine($"[C#] Creating distribution folder: {extractTo}");
                        Directory.CreateDirectory(extractTo);
                    }

                    // Backup existing files if folder already has content
                    string backupFolder = extractTo + ".backup";
                    if (Directory.Exists(extractTo) && Directory.GetFiles(extractTo).Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C#] Backing up existing files to: {backupFolder}");

                        if (Directory.Exists(backupFolder))
                        {
                            Directory.Delete(backupFolder, true);
                        }

                        // Copy current files to backup
                        CopyDirectory(extractTo, backupFolder);
                    }

                    // Extract ZIP file
                    System.Diagnostics.Debug.WriteLine($"[C#] Extracting ZIP file...");
                    ZipFile.ExtractToDirectory(tempZipPath, extractTo, overwriteFiles: true);
                    System.Diagnostics.Debug.WriteLine($"[C#] Extraction complete!");

                    // Clean up temp file
                    File.Delete(tempZipPath);
                    System.Diagnostics.Debug.WriteLine($"[C#] Temp file deleted");

                    // Send success message
                    var response = new
                    {
                        type = "distribution-download-complete",
                        version = version,
                        folder = extractTo,
                        success = true,
                        requestId = requestId
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ Distribution download complete!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Distribution download failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Stack trace: {ex.StackTrace}");

                // Send error message
                var errorResponse = new
                {
                    type = "distribution-download-failed",
                    error = ex.Message,
                    requestId = requestId
                };

                string errorJson = JsonSerializer.Serialize(errorResponse);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);
            }
        }

        private async Task HandleLaunchWMSModule(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Launching WMS module...");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string indexPath = root.GetProperty("indexPath").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] Index path: {indexPath}");

                    // Verify file exists
                    if (!File.Exists(indexPath))
                    {
                        throw new FileNotFoundException($"WMS module not found at: {indexPath}");
                    }

                    // Convert Windows path to file:/// URL format
                    string fileUrl = "file:///" + indexPath.Replace("\\", "/");
                    System.Diagnostics.Debug.WriteLine($"[C#] Navigating to: {fileUrl}");

                    // Navigate the WebView2 control to the local HTML file
                    wv.CoreWebView2.Navigate(fileUrl);

                    // Send success message
                    var response = new
                    {
                        type = "launch-wms-module",
                        success = true,
                        indexPath = indexPath,
                        requestId = requestId
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ WMS module launched successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Launch WMS module failed: {ex.Message}");

                // Send error message
                var errorResponse = new
                {
                    type = "launch-wms-module",
                    success = false,
                    error = ex.Message,
                    requestId = requestId
                };

                string errorJson = JsonSerializer.Serialize(errorResponse);
                wv.CoreWebView2.PostWebMessageAsJson(errorJson);
            }
        }

        // Helper method: Copy directory recursively
        private void CopyDirectory(string sourceDir, string destDir)
        {
            // Create destination directory
            Directory.CreateDirectory(destDir);

            // Copy files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
            }

            // Copy subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        private async Task HandleDownloadOrderPdf(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<DownloadPdfMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Downloading PDF for order {message.OrderNumber}");

                var result = await _printJobManager.DownloadSingleOrderAsync(
                    message.OrderNumber,
                    message.TripId,
                    message.TripDate
                );

                var response = new
                {
                    action = "downloadPdfResponse",
                    requestId = requestId,
                    success = result.Success,
                    message = result.Message,
                    filePath = result.FilePath
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Download PDF failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        // 🔧 NEW: Check if PDF exists locally
        private async Task HandleCheckPdfExists(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<CheckPdfExistsMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Checking PDF existence for order {message.OrderNumber}");

                // Construct PDF path: C:\fusion\{tripDate}\{tripId}\{orderNumber}.pdf
                string pdfPath = Path.Combine(@"C:\fusion", message.TripDate, message.TripId.ToString(), $"{message.OrderNumber}.pdf");
                bool exists = File.Exists(pdfPath);

                System.Diagnostics.Debug.WriteLine($"[C#] PDF Path: {pdfPath}");
                System.Diagnostics.Debug.WriteLine($"[C#] PDF Exists: {exists}");

                var response = new
                {
                    action = "checkPdfExistsResponse",
                    requestId = requestId,
                    exists = exists,
                    filePath = exists ? pdfPath : null,
                    orderNumber = message.OrderNumber
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Check PDF exists failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        /// <summary>
        /// Gets the active printer configuration from APEX REST API
        /// Returns null if not found or on error (caller should fallback to local config)
        /// </summary>
        private async Task<PrinterConfig> GetActivePrinterConfigFromApexAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Form1] Getting printer config from APEX REST...");

                string url = EndpointRegistry.GetEndpointUrl("WMS", "GETPRINTERCONFIG");
                if (string.IsNullOrEmpty(url))
                {
                    System.Diagnostics.Debug.WriteLine($"[Form1] ❌ Printer config endpoint not found in registry");
                    return null;
                }

                string jsonResponse = await _restApiClient.ExecuteGetAsync(url);

                // Parse JSON response to get active printer
                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                var items = jsonDoc.RootElement.GetProperty("items");

                foreach (var item in items.EnumerateArray())
                {
                    string isActive = item.GetProperty("isActive").GetString();
                    if (isActive == "Y")
                    {
                        var config = new PrinterConfig
                        {
                            PrinterName = item.GetProperty("printerName").GetString(),
                            PaperSize = item.GetProperty("paperSize").GetString(),
                            Orientation = item.GetProperty("orientation").GetString(),
                            FusionInstance = item.GetProperty("fusionInstance").GetString(),
                            FusionUsername = item.GetProperty("fusionUsername").GetString(),
                            FusionPassword = item.GetProperty("fusionPassword").GetString(),
                            AutoDownload = item.GetProperty("autoDownload").GetString() == "Y",
                            AutoPrint = item.GetProperty("autoPrint").GetString() == "Y"
                        };

                        System.Diagnostics.Debug.WriteLine($"[Form1] ✅ Got active printer config from APEX");
                        System.Diagnostics.Debug.WriteLine($"[Form1] Fusion Username: {config.FusionUsername}, Instance: {config.FusionInstance}");
                        return config;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Form1] ❌ No active printer found in APEX");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Form1] ❌ Failed to get printer config from APEX: {ex.Message}");
                return null;
            }
        }

        // 🔧 NEW: Print Store Transaction Report (Generic Handler)
        private async Task HandlePrintStoreTransaction(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string orderNumber = root.GetProperty("orderNumber").GetString();
                    string instance = root.GetProperty("instance").GetString();
                    string reportPath = root.GetProperty("reportPath").GetString();
                    string parameterName = root.GetProperty("parameterName").GetString();
                    string tripId = root.GetProperty("tripId").GetString();
                    string tripDate = root.GetProperty("tripDate").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] Printing report: {reportPath}");
                    System.Diagnostics.Debug.WriteLine($"[C#] Parameter: {parameterName}={orderNumber}, Instance: {instance}");
                    System.Diagnostics.Debug.WriteLine($"[C#] TripId: {tripId}, TripDate: {tripDate}");

                    // ✅ FIX: Get credentials from APEX REST API instead of local config
                    var printerConfig = await GetActivePrinterConfigFromApexAsync();

                    if (printerConfig == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C#] ❌ Failed to get printer config from APEX, falling back to local config");
                        // Fallback to local config if APEX fails
                        printerConfig = _storageManager.LoadPrinterConfig();
                    }

                    if (printerConfig == null)
                    {
                        SendErrorResponse(wv, requestId, "Printer configuration not found in APEX or local storage");
                        return;
                    }

                    var username = printerConfig.FusionUsername;
                    var password = printerConfig.FusionPassword;

                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    {
                        SendErrorResponse(wv, requestId, "Fusion credentials not configured. Please check APEX printer configuration.");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ Using credentials from APEX - Username: {username}, Instance: {instance}");

                    // Download PDF using Generic method
                    var downloader = new WMSApp.PrintManagement.FusionPdfDownloader();
                    var result = await downloader.DownloadGenericReportPdfAsync(
                        reportPath,
                        parameterName,
                        orderNumber,
                        instance,
                        username,
                        password
                    );

                    if (!result.Success)
                    {
                        SendErrorResponse(wv, requestId, $"Failed to generate report: {result.ErrorMessage}");
                        return;
                    }

                    // Convert base64 to bytes and save to C:\fusion\{tripDate}\{tripId}\ folder
                    byte[] pdfBytes = Convert.FromBase64String(result.Base64Content);

                    // Create folder structure: C:\fusion\{tripDate}\{tripId}
                    string folderPath = Path.Combine(@"C:\fusion", tripDate, tripId);
                    Directory.CreateDirectory(folderPath);

                    // Simple filename: {orderNumber}.pdf
                    string fileName = $"{orderNumber}.pdf";
                    string filePath = Path.Combine(folderPath, fileName);

                    await File.WriteAllBytesAsync(filePath, pdfBytes);

                    System.Diagnostics.Debug.WriteLine($"[C#] Report PDF saved to: {filePath}");

                    // Send success response
                    var response = new
                    {
                        action = "printStoreTransactionResponse",
                        requestId = requestId,
                        success = true,
                        message = $"Report saved to: {filePath}",
                        filePath = filePath
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Print Store Transaction failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Stack trace: {ex.StackTrace}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        // 🔧 NEW: Generic SOAP Report Handler - Runs any Oracle BI Publisher report and returns Base64 data
        private async Task HandleRunSoapReport(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string reportPath = root.GetProperty("reportPath").GetString();
                    string parameterName = root.GetProperty("parameterName").GetString();
                    string parameterValue = root.GetProperty("parameterValue").GetString();
                    string instance = root.GetProperty("instance").GetString();
                    string username = root.GetProperty("username").GetString();
                    string password = root.GetProperty("password").GetString();

                    System.Diagnostics.Debug.WriteLine("====================================");
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] HandleRunSoapReport STARTED");
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] Report Path: {reportPath}");
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] Parameter: {parameterName} = {parameterValue}");
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] Instance: {instance}");
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] Username: {username}");
                    System.Diagnostics.Debug.WriteLine("====================================");

                    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    {
                        System.Diagnostics.Debug.WriteLine($"[C# SOAP] ❌ ERROR: Credentials are missing");
                        SendErrorResponse(wv, requestId, "Fusion credentials not provided");
                        return;
                    }

                    // Download report and parse data
                    var downloader = new WMSApp.PrintManagement.FusionPdfDownloader();
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] Calling DownloadGenericReportAsync...");

                    var result = await downloader.DownloadGenericReportAsync(
                        reportPath,
                        parameterName,
                        parameterValue,
                        instance,
                        username,
                        password
                    );

                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] DownloadGenericReportAsync returned - Success: {result.Success}");

                    if (!result.Success)
                    {
                        System.Diagnostics.Debug.WriteLine($"[C# SOAP] ❌ ERROR: {result.ErrorMessage}");
                        SendErrorResponse(wv, requestId, $"Failed to run SOAP report: {result.ErrorMessage}");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] ✅ SUCCESS - Data records count: {result.DataRecords?.Count ?? 0}");

                    // Return data records as JSON array (not Base64)
                    var response = new
                    {
                        action = "runSoapReportResponse",
                        requestId = requestId,
                        success = true,
                        dataRecords = result.DataRecords,  // Send parsed data directly
                        recordCount = result.DataRecords?.Count ?? 0,
                        message = "SOAP report data extracted successfully"
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] Sending {result.DataRecords?.Count ?? 0} records to JavaScript...");
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);
                    System.Diagnostics.Debug.WriteLine($"[C# SOAP] ✅ Response sent successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# SOAP] ❌ EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[C# SOAP] Stack trace: {ex.StackTrace}");
                SendErrorResponse(wv, requestId, $"SOAP report error: {ex.Message}");
            }
        }

        private async Task HandlePrintOrder(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<PrintPdfMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Printing order {message.OrderNumber}");

                var result = await _printJobManager.PrintSingleOrderAsync(
                    message.OrderNumber,
                    message.TripId,
                    message.TripDate,
                    message.PrinterName ?? ""
                );

                var response = new
                {
                    action = "printOrderResponse",
                    requestId = requestId,
                    success = result.Success,
                    message = result.Success ? "Print job sent" : result.ErrorMessage
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Print order failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        /// <summary>
        /// Handles printSalesOrder action from JavaScript
        /// Downloads PDF from Oracle Fusion and saves it locally
        /// </summary>
        private async Task HandlePrintSalesOrder(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("====================================");
                System.Diagnostics.Debug.WriteLine($"[C#] ⭐ HandlePrintSalesOrder STARTED");
                System.Diagnostics.Debug.WriteLine($"[C#] RequestId: {requestId}");
                System.Diagnostics.Debug.WriteLine($"[C#] Message: {messageJson}");
                System.Diagnostics.Debug.WriteLine("====================================");

                var message = JsonSerializer.Deserialize<PrintSalesOrderMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] PrintSalesOrder Parameters:");
                System.Diagnostics.Debug.WriteLine($"[C#]   - OrderNumber: {message.OrderNumber}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Instance: {message.Instance}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - ReportPath: {message.ReportPath}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - ParameterName: {message.ParameterName}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - TripId: {message.TripId}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - TripDate: {message.TripDate}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - OrderType: {message.OrderType}");

                // Get credentials from storage
                var credentials = _storageManager.GetFusionCredentials();
                if (string.IsNullOrEmpty(credentials.Username))
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] ❌ No Fusion credentials found");
                    var errorResponse = new
                    {
                        action = "printSalesOrderResponse",
                        requestId = requestId,
                        success = false,
                        message = "Fusion credentials not configured. Please set up credentials in Settings."
                    };
                    wv.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(errorResponse));
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[C#] Using credentials for user: {credentials.Username}");

                // Download PDF using FusionPdfDownloader
                var downloader = new WMSApp.PrintManagement.FusionPdfDownloader();
                WMSApp.PrintManagement.PdfDownloadResult result;

                if (!string.IsNullOrEmpty(message.ReportPath))
                {
                    // Use generic report download with custom report path
                    System.Diagnostics.Debug.WriteLine($"[C#] Using generic report: {message.ReportPath}");
                    result = await downloader.DownloadGenericReportPdfAsync(
                        message.ReportPath,
                        message.ParameterName ?? "Order_Number",
                        message.OrderNumber,
                        message.Instance,
                        credentials.Username,
                        credentials.Password
                    );
                }
                else
                {
                    // Use default Sales Order report
                    System.Diagnostics.Debug.WriteLine($"[C#] Using default Sales Order report");
                    result = await downloader.DownloadSalesOrderPdfAsync(
                        message.OrderNumber,
                        message.Instance,
                        credentials.Username,
                        credentials.Password
                    );
                }

                if (result.Success)
                {
                    // Save PDF to file
                    string tripDate = message.TripDate ?? DateTime.Now.ToString("yyyy-MM-dd");
                    string tripId = message.TripId ?? "manual";
                    string folderPath = Path.Combine(@"C:\fusion", tripDate, tripId);
                    Directory.CreateDirectory(folderPath);

                    string fileName = $"{message.OrderNumber}.pdf";
                    string filePath = Path.Combine(folderPath, fileName);

                    byte[] pdfBytes = Convert.FromBase64String(result.Base64Content);
                    await File.WriteAllBytesAsync(filePath, pdfBytes);

                    System.Diagnostics.Debug.WriteLine($"[C#] ✅ PDF saved to: {filePath}");

                    var successResponse = new
                    {
                        action = "printSalesOrderResponse",
                        requestId = requestId,
                        success = true,
                        pdfPath = filePath,
                        filePath = filePath,
                        message = "PDF generated successfully"
                    };
                    wv.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(successResponse));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] ❌ PDF download failed: {result.ErrorMessage}");
                    var errorResponse = new
                    {
                        action = "printSalesOrderResponse",
                        requestId = requestId,
                        success = false,
                        message = result.ErrorMessage
                    };
                    wv.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(errorResponse));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] HandlePrintSalesOrder failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Stack trace: {ex.StackTrace}");

                var errorResponse = new
                {
                    action = "printSalesOrderResponse",
                    requestId = requestId,
                    success = false,
                    message = $"Error: {ex.Message}"
                };
                wv.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(errorResponse));
            }
        }

        private async Task HandleGetPrintJobs(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("====================================");
                System.Diagnostics.Debug.WriteLine($"[C#] ⭐ HandleGetPrintJobs STARTED");
                System.Diagnostics.Debug.WriteLine($"[C#] RequestId: {requestId}");
                System.Diagnostics.Debug.WriteLine("====================================");

                var message = System.Text.Json.JsonSerializer.Deserialize<GetPrintJobsMessage>(
                    messageJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Message Details:");
                System.Diagnostics.Debug.WriteLine($"[C#]   - TripId: {message.TripId ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - StartDate: {message.StartDate ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - EndDate: {message.EndDate ?? "NULL"}");

                List<PrintJob> jobs;

                if (!string.IsNullOrEmpty(message.TripId))
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] Getting jobs for specific trip: {message.TripId}");
                    jobs = _storageManager.GetTripPrintJobs(message.StartDate, message.TripId);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] Getting ALL print jobs");
                    DateTime? startDate = string.IsNullOrEmpty(message.StartDate) ? null : DateTime.Parse(message.StartDate);
                    DateTime? endDate = string.IsNullOrEmpty(message.EndDate) ? null : DateTime.Parse(message.EndDate);
                    jobs = _storageManager.GetAllPrintJobs(startDate, endDate);
                }

                System.Diagnostics.Debug.WriteLine($"[C#] Retrieved {jobs.Count} print jobs");

                if (jobs.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[C#] Sample jobs:");
                    foreach (var job in jobs.Take(3))
                    {
                        System.Diagnostics.Debug.WriteLine($"[C#]   - Order: {job.OrderNumber}, Trip: {job.TripId}, Date: {job.TripDate}, Status: {job.DownloadStatus}");
                    }
                }

                var stats = _storageManager.GetPrintJobStats();

                System.Diagnostics.Debug.WriteLine($"[C#] Print Job Stats:");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Total: {stats.TotalJobs}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Pending Download: {stats.PendingDownload}");
                System.Diagnostics.Debug.WriteLine($"[C#]   - Completed: {stats.DownloadCompleted}");

                var response = new
                {
                    action = "printJobsResponse",
                    requestId = requestId,
                    success = true,
                    data = new
                    {
                        jobs = jobs,
                        stats = stats
                    }
                };

                string responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                System.Diagnostics.Debug.WriteLine($"[C#] Sending response with {jobs.Count} jobs to JavaScript");

                wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                System.Diagnostics.Debug.WriteLine($"[C#] ✅ HandleGetPrintJobs COMPLETED");
                System.Diagnostics.Debug.WriteLine("====================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] ❌ HandleGetPrintJobs FAILED");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Stack Trace: {ex.StackTrace}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }
        private async Task HandleRetryFailedJobs(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<GetPrintJobsMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Retrying failed jobs for trip {message.TripId}");

                var result = await _printJobManager.RetryFailedJobsAsync(message.TripId, message.StartDate);

                var response = new
                {
                    action = "retryJobsResponse",
                    requestId = requestId,
                    success = result.Success,
                    message = result.Message,
                    retriedCount = result.RetriedCount
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Retry failed jobs error: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        private async Task HandleConfigurePrinter(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                var message = JsonSerializer.Deserialize<ConfigurePrinterMessage>(
                    messageJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                System.Diagnostics.Debug.WriteLine($"[C#] Configuring printer: {message.Config.PrinterName}");

                bool success = _storageManager.SavePrinterConfig(message.Config);

                var response = new
                {
                    action = "configurePrinterResponse",
                    requestId = requestId,
                    success = success,
                    message = success ? "Printer configuration saved" : "Failed to save configuration"
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Configure printer failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        private async Task HandleSetInstanceSetting(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Setting instance setting");

                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string instance = root.GetProperty("instance").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] Instance setting: {instance}");

                    bool success = _storageManager.SaveInstanceSetting(instance);

                    var response = new
                    {
                        action = "setInstanceSettingResponse",
                        requestId = requestId,
                        success = success,
                        message = success ? $"Instance setting saved: {instance}" : "Failed to save instance setting"
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Set instance setting failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }

            await Task.CompletedTask;
        }

        private async Task HandleGetPrinterConfig(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Getting printer configuration");

                var config = _storageManager.LoadPrinterConfig();

                var response = new
                {
                    action = "printerConfigResponse",
                    requestId = requestId,
                    success = true,
                    data = config
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Get printer config failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        private async Task HandleGetInstalledPrinters(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Getting installed printers");

                var printers = _printerService.GetInstalledPrinters();
                var defaultPrinter = _printerService.GetDefaultPrinter();

                var response = new
                {
                    action = "installedPrintersResponse",
                    requestId = requestId,
                    success = true,
                    data = new
                    {
                        printers = printers,
                        defaultPrinter = defaultPrinter
                    }
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                System.Diagnostics.Debug.WriteLine($"[C#] Found {printers.Count} printers");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Get printers failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        private async Task HandleTestPrinter(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string printerName = root.GetProperty("printerName").GetString();

                    System.Diagnostics.Debug.WriteLine($"[C#] Testing printer: {printerName}");

                    var result = await _printerService.TestPrinterAsync(printerName);

                    var response = new
                    {
                        action = "testPrinterResponse",
                        requestId = requestId,
                        success = result.Success,
                        message = result.Message,
                        data = result
                    };

                    string responseJson = JsonSerializer.Serialize(response);
                    wv.CoreWebView2.PostWebMessageAsJson(responseJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Test printer failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }
        }

        private async Task HandleDiscoverBluetoothPrinters(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Discovering Bluetooth printers...");

                // Get Bluetooth printers using PrinterSettings
                var bluetoothPrinters = new List<string>();

                // Note: Windows doesn't provide a direct API to filter only Bluetooth printers
                // We'll get all printers and try to identify Bluetooth ones by name patterns
                foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    // Common Bluetooth printer patterns in names
                    if (printerName.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                        printerName.Contains("BT", StringComparison.OrdinalIgnoreCase) ||
                        printerName.Contains("Wireless", StringComparison.OrdinalIgnoreCase))
                    {
                        bluetoothPrinters.Add(printerName);
                    }
                }

                var response = new
                {
                    action = "discoverBluetoothPrintersResponse",
                    requestId = requestId,
                    success = true,
                    printers = bluetoothPrinters,
                    count = bluetoothPrinters.Count
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                System.Diagnostics.Debug.WriteLine($"[C#] Found {bluetoothPrinters.Count} Bluetooth printer(s)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] Bluetooth printer discovery failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }

            await Task.CompletedTask;
        }

        private async Task HandleDiscoverWifiPrinters(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[C#] Discovering WiFi/Network printers...");

                // Get network printers
                var networkPrinters = new List<string>();

                foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    // Network printers typically have UNC paths (\\server\printer) or contain "Network"
                    if (printerName.StartsWith("\\\\") ||
                        printerName.Contains("Network", StringComparison.OrdinalIgnoreCase) ||
                        printerName.Contains("IP", StringComparison.OrdinalIgnoreCase) ||
                        printerName.Contains("WiFi", StringComparison.OrdinalIgnoreCase))
                    {
                        networkPrinters.Add(printerName);
                    }
                }

                var response = new
                {
                    action = "discoverWifiPrintersResponse",
                    requestId = requestId,
                    success = true,
                    printers = networkPrinters,
                    count = networkPrinters.Count
                };

                string responseJson = JsonSerializer.Serialize(response);
                wv.CoreWebView2.PostWebMessageAsJson(responseJson);

                System.Diagnostics.Debug.WriteLine($"[C#] Found {networkPrinters.Count} WiFi/Network printer(s)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[C# ERROR] WiFi printer discovery failed: {ex.Message}");
                SendErrorResponse(wv, requestId, ex.Message);
            }

            await Task.CompletedTask;
        }

        private void SendErrorResponse(WebView2 wv, string requestId, string errorMessage)
        {
            var errorResponse = new
            {
                action = "error",
                requestId = requestId,
                success = false,
                message = errorMessage
            };

            string errorJson = JsonSerializer.Serialize(errorResponse);
            wv.CoreWebView2.PostWebMessageAsJson(errorJson);
        }

        // ========== NAVIGATION METHODS ==========

        private WebView2 GetCurrentWebView()
        {
            foreach (Control ctrl in tabBar.Controls)
            {
                if (ctrl is CustomTabButton tab && tab.IsSelected)
                    return tab.WebView;
            }
            return null;
        }

        private CustomTabButton GetCurrentTab()
        {
            foreach (Control ctrl in tabBar.Controls)
            {
                if (ctrl is CustomTabButton tab && tab.IsSelected)
                    return tab;
            }
            return null;
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null && wv.CoreWebView2.CanGoBack)
            {
                wv.CoreWebView2.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null && wv.CoreWebView2.CanGoForward)
            {
                wv.CoreWebView2.GoForward();
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            var wv = GetCurrentWebView();
            wv?.CoreWebView2?.Reload();
        }

        private async void ClearCacheButton_Click(object sender, EventArgs e)
        {
            try
            {
                var wv = GetCurrentWebView();
                if (wv?.CoreWebView2 != null)
                {
                    var result = MessageBox.Show(
                        "Clear browser cache and reload?\n\nThis will refresh all pages and clear cached files.",
                        "Clear Cache",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Clear all browsing data
                        await wv.CoreWebView2.Profile.ClearBrowsingDataAsync(
                            CoreWebView2BrowsingDataKinds.AllDomStorage |
                            CoreWebView2BrowsingDataKinds.CacheStorage |
                            CoreWebView2BrowsingDataKinds.DiskCache |
                            CoreWebView2BrowsingDataKinds.IndexedDb |
                            CoreWebView2BrowsingDataKinds.LocalStorage |
                            CoreWebView2BrowsingDataKinds.WebSql
                        );

                        // Reload the page
                        wv.Reload();

                        MessageBox.Show(
                            "✓ Cache cleared successfully!\n\nPage is reloading...",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error clearing cache:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            Navigate("https://www.google.com");
        }

        private void OpenFileButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "HTML Files (*.html;*.htm)|*.html;*.htm|All Files (*.*)|*.*";
                openFileDialog.Title = "Open HTML File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    Navigate("file:///" + filePath.Replace("\\", "/"));
                }
            }
        }

        private void FavoriteButton_Click(object sender, EventArgs e)
        {
            if (favoriteButton.Text == "☆")
            {
                favoriteButton.Text = "★";
                favoriteButton.ForeColor = Color.FromArgb(255, 200, 0);
                MessageBox.Show("Added to favorites!", "Favorites", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                favoriteButton.Text = "☆";
                favoriteButton.ForeColor = Color.Gray;
                MessageBox.Show("Removed from favorites!", "Favorites", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ProfileButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Profile settings", "Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CopilotButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Ask Copilot feature coming soon!", "Copilot", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Navigate(urlTextBox.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.T)
            {
                AddNewTab("https://www.google.com");
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                var currentTab = GetCurrentTab();
                if (currentTab != null)
                    TabButton_CloseClicked(currentTab, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                OpenFileButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void Navigate(string url)
        {
            var wv = GetCurrentWebView();
            if (wv?.CoreWebView2 != null)
            {
                if (url.StartsWith("file:///") || url.StartsWith("file://"))
                {
                    try
                    {
                        // Clear cache before loading local files (preserve localStorage for login state)
                        System.Diagnostics.Debug.WriteLine("[CACHE] Clearing cache before loading local file (preserving localStorage)...");
                        await wv.CoreWebView2.Profile.ClearBrowsingDataAsync(
                            CoreWebView2BrowsingDataKinds.CacheStorage |
                            CoreWebView2BrowsingDataKinds.DiskCache);
                        System.Diagnostics.Debug.WriteLine("[CACHE] ✅ Cache cleared successfully!");

                        wv.Source = new Uri(url);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                try
                {
                    wv.Source = new Uri(url);
                }
                catch
                {
                    wv.Source = new Uri("https://www.google.com/search?q=" + Uri.EscapeDataString(url));
                }
            }
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            urlTextBox.Text = e.Uri;
            UpdateSecurityIcon(GetCurrentWebView());
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var wv = sender as CoreWebView2;
            if (wv != null)
            {
                urlTextBox.Text = wv.Source;

                var currentTab = GetCurrentTab();
                if (currentTab != null)
                {
                    string title = wv.DocumentTitle;
                    if (string.IsNullOrEmpty(title))
                        title = "New Tab";
                    else if (title.Length > 20)
                        title = title.Substring(0, 20) + "...";

                    currentTab.TabText = title;
                }

                UpdateNavigationButtons(GetCurrentWebView());
                UpdateSecurityIcon(GetCurrentWebView());

                // Send user session to WMS pages after navigation completes
                string source = wv.Source.ToLower();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Navigation completed: {source}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] _isLoggedIn: {_isLoggedIn}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Contains wms/index.html: {source.Contains("/wms/index.html") || source.Contains("\\wms\\index.html")}");

                if (_isLoggedIn && (source.Contains("/wms/index.html") || source.Contains("\\wms\\index.html")))
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Conditions met! Sending session to WebView...");
                    // Add a small delay to ensure JavaScript is fully loaded
                    await System.Threading.Tasks.Task.Delay(500);
                    SendUserSessionToWebView(wv);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Conditions NOT met for sending session");
                }
            }
        }

        private async void SendUserSessionToWebView(CoreWebView2 wv)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] ========================================");
            System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] SendUserSessionToWebView CALLED");

            try
            {
                // Escape strings for JavaScript
                string username = _loggedInUsername?.Replace("'", "\\'") ?? "";
                string password = _loggedInPassword?.Replace("'", "\\'") ?? "";
                string instance = _loggedInInstance?.Replace("'", "\\'") ?? "";
                string loginDateTime = ("Logged in: " + _loggedInDateTime)?.Replace("'", "\\'") ?? "";

                System.Diagnostics.Debug.WriteLine($"[DEBUG SendSession] Username: {username}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG SendSession] Instance: {instance}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG SendSession] LoginDateTime: {loginDateTime}");

                // Set localStorage values for login state (required for web page auth check)
                // Also set fusionCloudUsername/Password for API calls
                string setLocalStorageScript = $@"
                    console.log('[C# INJECT] Setting localStorage values...');
                    localStorage.setItem('loggedIn', 'true');
                    localStorage.setItem('username', '{username}');
                    localStorage.setItem('password', '{password}');
                    localStorage.setItem('instanceName', '{instance}');
                    localStorage.setItem('loginTime', '{loginDateTime}');
                    localStorage.setItem('fusionCloudUsername', '{username}');
                    localStorage.setItem('fusionCloudPassword', '{password}');
                    localStorage.setItem('fusionInstance', '{instance}');
                    console.log('[C# INJECT] localStorage values set:');
                    console.log('[C# INJECT] loggedIn:', localStorage.getItem('loggedIn'));
                    console.log('[C# INJECT] username:', localStorage.getItem('username'));
                    console.log('[C# INJECT] instanceName:', localStorage.getItem('instanceName'));
                    console.log('[C# INJECT] fusionCloudUsername:', localStorage.getItem('fusionCloudUsername'));
                ";
                System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] Executing localStorage script...");
                await wv.ExecuteScriptAsync(setLocalStorageScript);
                System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] localStorage script executed");

                // Call the JavaScript function to update UI
                string script = $@"
                    console.log('[C# INJECT] Calling setLoggedInUser...');
                    if (typeof setLoggedInUser === 'function') {{
                        setLoggedInUser('{username}', '{instance}', '{loginDateTime}');
                        console.log('[C# INJECT] setLoggedInUser called successfully');
                    }} else {{
                        console.log('[C# INJECT] WARNING: setLoggedInUser function NOT FOUND!');
                    }}
                ";
                System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] Executing setLoggedInUser script...");
                await wv.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] setLoggedInUser script executed");

                // Remove auth-pending class to show page content
                System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] Removing auth-pending class...");
                await wv.ExecuteScriptAsync(@"
                    console.log('[C# INJECT] Removing auth-pending class...');
                    document.body.classList.remove('auth-pending');
                    console.log('[C# INJECT] auth-pending class removed, body should be visible');
                ");
                System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] auth-pending class removed");

                System.Diagnostics.Debug.WriteLine($"[DEBUG SendSession] ✅ SUCCESS - Sent session to WebView");
                System.Diagnostics.Debug.WriteLine("[DEBUG SendSession] ========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG SendSession] ❌ ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG SendSession] Stack: {ex.StackTrace}");
            }
        }

        private void UpdateNavigationButtons(WebView2 wv)
        {
            if (wv?.CoreWebView2 != null)
            {
                backButton.Enabled = wv.CoreWebView2.CanGoBack;
                forwardButton.Enabled = wv.CoreWebView2.CanGoForward;
            }
        }

        private void UpdateSecurityIcon(WebView2 wv)
        {
            if (wv?.CoreWebView2 != null)
            {
                string url = wv.Source?.ToString() ?? "";
                if (url.StartsWith("https://"))
                {
                    securityIcon.Text = "🔒";
                    securityIcon.ForeColor = Color.Green;
                }
                else if (url.StartsWith("http://"))
                {
                    securityIcon.Text = "⚠️";
                    securityIcon.ForeColor = Color.Orange;
                }
                else if (url.StartsWith("file://"))
                {
                    securityIcon.Text = "📄";
                    securityIcon.ForeColor = Color.Blue;
                }
                else
                {
                    securityIcon.Text = "ℹ️";
                    securityIcon.ForeColor = Color.Gray;
                }
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            AddNewTab(e.Uri);
        }

        // ========== RELEASE MANAGER HANDLER ==========
        private async Task HandleCreateRelease(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string versionBump = root.TryGetProperty("versionBump", out var vb) ? vb.GetString() : "patch";
                    string changelog = root.TryGetProperty("changelog", out var cl) ? cl.GetString() : "";
                    string githubToken = root.TryGetProperty("githubToken", out var gt) ? gt.GetString() : "";
                    string repoOwner = root.TryGetProperty("repoOwner", out var ro) ? ro.GetString() : "javeedin";
                    string repoName = root.TryGetProperty("repoName", out var rn) ? rn.GetString() : "graysWMSwebviewnew";

                    System.Diagnostics.Debug.WriteLine($"[RELEASE] Starting release process - bump: {versionBump}");

                    // Send progress to frontend
                    await SendReleaseProgress(wv, 1, "Starting release process...", "info");

                    // Get the repo root directory
                    await SendReleaseProgress(wv, 1, "Finding repository root...", "info");
                    string repoRoot = FindRepoRoot();
                    if (string.IsNullOrEmpty(repoRoot))
                    {
                        await SendReleaseError(wv, "Could not find repository root directory. Make sure version.json exists in your repo folder.");
                        return;
                    }
                    await SendReleaseProgress(wv, 1, $"Repository found: {repoRoot}", "info");

                    string versionFilePath = Path.Combine(repoRoot, "version.json");
                    if (!File.Exists(versionFilePath))
                    {
                        await SendReleaseError(wv, $"version.json not found at: {versionFilePath}");
                        return;
                    }

                    // Read current version
                    await SendReleaseProgress(wv, 1, "Reading current version...", "info");
                    string versionJson = File.ReadAllText(versionFilePath);
                    using (var versionDoc = JsonDocument.Parse(versionJson))
                    {
                        string currentVersion = versionDoc.RootElement.GetProperty("version").GetString();
                        string[] parts = currentVersion.Split('.');
                        int major = int.Parse(parts[0]);
                        int minor = int.Parse(parts[1]);
                        int patch = int.Parse(parts[2]);

                        // Bump version
                        switch (versionBump)
                        {
                            case "major": major++; minor = 0; patch = 0; break;
                            case "minor": minor++; patch = 0; break;
                            case "patch": patch++; break;
                        }

                        string newVersion = $"{major}.{minor}.{patch}";
                        await SendReleaseProgress(wv, 1, $"Version bump: {currentVersion} -> {newVersion}", "success");

                        // Step 2: Run PowerShell script
                        await SendReleaseProgress(wv, 2, "Running PowerShell release script...", "info");

                        string psScriptPath = Path.Combine(repoRoot, "create-release.ps1");
                        if (!File.Exists(psScriptPath))
                        {
                            await SendReleaseError(wv, $"create-release.ps1 not found at: {psScriptPath}");
                            return;
                        }
                        await SendReleaseProgress(wv, 2, $"Script found: {psScriptPath}", "info");

                        // Run PowerShell script with timeout
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-ExecutionPolicy Bypass -NonInteractive -File \"{psScriptPath}\" -VersionBump {versionBump} -SkipGitPush",
                            WorkingDirectory = repoRoot,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        await SendReleaseProgress(wv, 2, "Executing PowerShell script (this may take a minute)...", "info");

                        using (var process = new System.Diagnostics.Process { StartInfo = psi })
                        {
                            process.Start();

                            // Read output asynchronously
                            var outputTask = process.StandardOutput.ReadToEndAsync();
                            var errorTask = process.StandardError.ReadToEndAsync();

                            // Wait for process with timeout (2 minutes)
                            bool exited = process.WaitForExit(120000);

                            if (!exited)
                            {
                                process.Kill();
                                await SendReleaseError(wv, "PowerShell script timed out after 2 minutes");
                                return;
                            }

                            string output = await outputTask;
                            string error = await errorTask;

                            // Log output for debugging
                            System.Diagnostics.Debug.WriteLine($"[RELEASE] PowerShell exit code: {process.ExitCode}");
                            System.Diagnostics.Debug.WriteLine($"[RELEASE] PowerShell output: {output}");
                            if (!string.IsNullOrEmpty(error))
                            {
                                System.Diagnostics.Debug.WriteLine($"[RELEASE] PowerShell error: {error}");
                            }

                            // Send output lines to frontend
                            if (!string.IsNullOrEmpty(output))
                            {
                                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines.Take(20)) // Show first 20 lines
                                {
                                    if (line.Contains("SUCCESS") || line.Contains("OK"))
                                        await SendReleaseProgress(wv, 2, line.Trim(), "success");
                                    else if (line.Contains("ERROR") || line.Contains("FAIL"))
                                        await SendReleaseProgress(wv, 2, line.Trim(), "error");
                                    else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("="))
                                        await SendReleaseProgress(wv, 2, line.Trim(), "info");
                                }
                            }

                            if (process.ExitCode != 0)
                            {
                                await SendReleaseError(wv, $"Release script failed (exit code {process.ExitCode}): {error}");
                                return;
                            }
                        }

                        await SendReleaseProgress(wv, 2, "Distribution folder and ZIP created successfully!", "success");

                        // Step 3: Verify ZIP file exists
                        await SendReleaseProgress(wv, 3, "Verifying ZIP file...", "info");
                        string zipFileName = $"wms-webview-html-{newVersion}.zip";
                        string zipFilePath = Path.Combine(repoRoot, zipFileName);

                        if (!File.Exists(zipFilePath))
                        {
                            await SendReleaseError(wv, $"ZIP file not found: {zipFileName}");
                            return;
                        }

                        var zipInfo = new FileInfo(zipFilePath);
                        await SendReleaseProgress(wv, 3, $"ZIP created: {zipFileName} ({zipInfo.Length / 1024 / 1024:F2} MB)", "success");

                        // Step 4: Git commit and push
                        await SendReleaseProgress(wv, 4, "Committing changes to Git...", "info");

                        // Git add and commit
                        await RunGitCommand(repoRoot, "add version.json latest-release.json");
                        await RunGitCommand(repoRoot, $"commit -m \"Release: WMS v{newVersion}\"");

                        // Get current branch and push
                        string branch = await RunGitCommand(repoRoot, "rev-parse --abbrev-ref HEAD");
                        branch = branch.Trim();
                        await RunGitCommand(repoRoot, $"push origin {branch}");

                        await SendReleaseProgress(wv, 4, $"Pushed to branch: {branch}", "success");

                        // Step 5: Create GitHub Release
                        await SendReleaseProgress(wv, 5, "Creating GitHub release...", "info");

                        using (var httpClient = new HttpClient())
                        {
                            httpClient.DefaultRequestHeaders.Add("Authorization", $"token {githubToken}");
                            httpClient.DefaultRequestHeaders.Add("User-Agent", "WMS-Release-Manager");
                            httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                            var releasePayload = new
                            {
                                tag_name = $"v{newVersion}",
                                name = $"WMS WebView HTML Package v{newVersion}",
                                body = changelog,
                                draft = false,
                                prerelease = false
                            };

                            string releaseJson = JsonSerializer.Serialize(releasePayload);
                            var content = new StringContent(releaseJson, System.Text.Encoding.UTF8, "application/json");

                            var releaseResponse = await httpClient.PostAsync(
                                $"https://api.github.com/repos/{repoOwner}/{repoName}/releases",
                                content
                            );

                            string releaseResponseBody = await releaseResponse.Content.ReadAsStringAsync();

                            if (!releaseResponse.IsSuccessStatusCode)
                            {
                                await SendReleaseError(wv, $"Failed to create release: {releaseResponseBody}");
                                return;
                            }

                            using (var releaseDoc = JsonDocument.Parse(releaseResponseBody))
                            {
                                string uploadUrl = releaseDoc.RootElement.GetProperty("upload_url").GetString();
                                uploadUrl = uploadUrl.Replace("{?name,label}", "");
                                string releaseUrl = releaseDoc.RootElement.GetProperty("html_url").GetString();

                                await SendReleaseProgress(wv, 5, $"Release created: {releaseUrl}", "success");

                                // Step 6: Upload ZIP file
                                await SendReleaseProgress(wv, 6, "Uploading ZIP file...", "info");

                                byte[] zipBytes = File.ReadAllBytes(zipFilePath);
                                var uploadContent = new ByteArrayContent(zipBytes);
                                uploadContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");

                                var uploadResponse = await httpClient.PostAsync(
                                    $"{uploadUrl}?name={Uri.EscapeDataString(zipFileName)}",
                                    uploadContent
                                );

                                if (!uploadResponse.IsSuccessStatusCode)
                                {
                                    string uploadError = await uploadResponse.Content.ReadAsStringAsync();
                                    await SendReleaseError(wv, $"Failed to upload ZIP: {uploadError}");
                                    return;
                                }

                                await SendReleaseProgress(wv, 6, "ZIP file uploaded successfully!", "success");

                                // Complete
                                await SendReleaseComplete(wv, newVersion);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RELEASE ERROR] {ex.Message}");
                await SendReleaseError(wv, ex.Message);
            }
        }

        private async Task<string> RunGitCommand(string workingDir, string args)
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new System.Diagnostics.Process { StartInfo = psi })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    System.Diagnostics.Debug.WriteLine($"[GIT] Error: {error}");
                }

                return output;
            }
        }

        private async Task HandleGetPickersView(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string fromDate = root.TryGetProperty("fromDate", out var fd) ? fd.GetString() : "";
                    string instance = root.TryGetProperty("instance", out var inst) ? inst.GetString() : "PROD";

                    System.Diagnostics.Debug.WriteLine($"[PICKERS VIEW] Loading data for date: {fromDate}, instance: {instance}");

                    // Get LOGIN endpoint URL from EndpointConfigReader using IntegrationCode (WAREHOUSEMANAGEMENT)
                    string baseUrl = EndpointConfigReader.GetEndpointUrl("APEX", "LOGIN", instance);
                    if (string.IsNullOrEmpty(baseUrl))
                    {
                        throw new Exception($"LOGIN endpoint not configured for instance '{instance}'. Please add LOGIN endpoint in Settings.");
                    }
                    string apiUrl = $"{baseUrl}/trip/getpickersview?fromDate={fromDate}&instance={instance}";

                    System.Diagnostics.Debug.WriteLine($"[PICKERS VIEW] API URL: {apiUrl}");

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(300);
                        var response = await httpClient.GetAsync(apiUrl);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        System.Diagnostics.Debug.WriteLine($"[PICKERS VIEW] Response status: {response.StatusCode}, Length: {responseContent.Length}");

                        if (response.IsSuccessStatusCode)
                        {
                            // Send data back to JavaScript
                            string escapedJson = responseContent.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "");
                            await wv.CoreWebView2.ExecuteScriptAsync($@"
                                if (typeof window.handlePickersViewData === 'function') {{
                                    window.handlePickersViewData({responseContent}, null);
                                }}
                            ");
                        }
                        else
                        {
                            await wv.CoreWebView2.ExecuteScriptAsync($@"
                                if (typeof window.handlePickersViewData === 'function') {{
                                    window.handlePickersViewData(null, 'HTTP {(int)response.StatusCode}: {response.ReasonPhrase}');
                                }}
                            ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PICKERS VIEW ERROR] {ex.Message}");
                string escapedError = ex.Message.Replace("\\", "\\\\").Replace("'", "\\'");
                await wv.CoreWebView2.ExecuteScriptAsync($@"
                    if (typeof window.handlePickersViewData === 'function') {{
                        window.handlePickersViewData(null, '{escapedError}');
                    }}
                ");
            }
        }

        /// <summary>
        /// Posts a message to Microsoft Teams via Incoming Webhook
        /// </summary>
        private async Task HandlePostToTeams(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string webhookUrl = root.TryGetProperty("webhookUrl", out var wh) ? wh.GetString() : "";
                    string cardJson = root.TryGetProperty("cardPayload", out var cp) ? cp.GetRawText() : "";

                    System.Diagnostics.Debug.WriteLine($"[TEAMS] Posting to Teams webhook...");
                    System.Diagnostics.Debug.WriteLine($"[TEAMS] Webhook URL: {webhookUrl?.Substring(0, Math.Min(50, webhookUrl?.Length ?? 0))}...");
                    System.Diagnostics.Debug.WriteLine($"[TEAMS] Card JSON length: {cardJson?.Length}");

                    if (string.IsNullOrEmpty(webhookUrl))
                    {
                        await wv.CoreWebView2.ExecuteScriptAsync($@"
                            if (typeof window.handleTeamsPostResult === 'function') {{
                                window.handleTeamsPostResult(false, 'Webhook URL is required');
                            }}
                        ");
                        return;
                    }

                    if (string.IsNullOrEmpty(cardJson))
                    {
                        await wv.CoreWebView2.ExecuteScriptAsync($@"
                            if (typeof window.handleTeamsPostResult === 'function') {{
                                window.handleTeamsPostResult(false, 'Card payload is required');
                            }}
                        ");
                        return;
                    }

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(300);

                        var content = new StringContent(cardJson, Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync(webhookUrl, content);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        System.Diagnostics.Debug.WriteLine($"[TEAMS] Response status: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"[TEAMS] Response content: {responseContent}");

                        if (response.IsSuccessStatusCode)
                        {
                            await wv.CoreWebView2.ExecuteScriptAsync($@"
                                if (typeof window.handleTeamsPostResult === 'function') {{
                                    window.handleTeamsPostResult(true, 'Message posted successfully to Teams');
                                }}
                            ");
                        }
                        else
                        {
                            string escapedError = responseContent.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "");
                            await wv.CoreWebView2.ExecuteScriptAsync($@"
                                if (typeof window.handleTeamsPostResult === 'function') {{
                                    window.handleTeamsPostResult(false, 'HTTP {(int)response.StatusCode}: {escapedError}');
                                }}
                            ");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TEAMS ERROR] {ex.Message}");
                string escapedError = ex.Message.Replace("\\", "\\\\").Replace("'", "\\'");
                await wv.CoreWebView2.ExecuteScriptAsync($@"
                    if (typeof window.handleTeamsPostResult === 'function') {{
                        window.handleTeamsPostResult(false, '{escapedError}');
                    }}
                ");
            }
        }

        /// <summary>
        /// Gets the current Outlook user's email address (with crash protection)
        /// </summary>
        private async Task HandleGetOutlookEmail(WebView2 wv, string messageJson, string requestId)
        {
            string email = "";

            try
            {
                System.Diagnostics.Debug.WriteLine("[OUTLOOK] Getting current user email...");

                // Method 1: Try Windows username as safest fallback first
                try
                {
                    email = Environment.UserName;
                    if (!string.IsNullOrEmpty(email))
                    {
                        email = email + "@grfruits.com";
                    }
                }
                catch { }

                // Method 2: Try to get from Outlook (may fail if Outlook not installed)
                try
                {
                    Type outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = null;
                        try
                        {
                            outlookApp = Activator.CreateInstance(outlookType);
                            if (outlookApp != null)
                            {
                                dynamic ns = outlookApp.GetNamespace("MAPI");
                                if (ns != null)
                                {
                                    dynamic currentUser = ns.CurrentUser;
                                    if (currentUser != null)
                                    {
                                        string outlookEmail = null;
                                        try { outlookEmail = currentUser.Address; } catch { }

                                        if (!string.IsNullOrEmpty(outlookEmail))
                                        {
                                            // If X500 format, try to get SMTP address
                                            if (outlookEmail.StartsWith("/O=") || outlookEmail.StartsWith("/o="))
                                            {
                                                try
                                                {
                                                    dynamic exchUser = currentUser.GetExchangeUser();
                                                    if (exchUser != null)
                                                    {
                                                        string smtpAddr = exchUser.PrimarySmtpAddress;
                                                        if (!string.IsNullOrEmpty(smtpAddr))
                                                        {
                                                            outlookEmail = smtpAddr;
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }

                                            email = outlookEmail;
                                            System.Diagnostics.Debug.WriteLine($"[OUTLOOK] Found email from Outlook: {email}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception comEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[OUTLOOK] COM error (non-fatal): {comEx.Message}");
                        }
                    }
                }
                catch (Exception outlookEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[OUTLOOK] Outlook access error (non-fatal): {outlookEx.Message}");
                }

                // Ensure we have something
                if (string.IsNullOrEmpty(email))
                {
                    email = Environment.UserName + "@grfruits.com";
                }

                System.Diagnostics.Debug.WriteLine($"[OUTLOOK] Final email: {email}");

                // Send result back to JavaScript
                try
                {
                    string safeEmail = email.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "");
                    await wv.CoreWebView2.ExecuteScriptAsync($@"
                        if (typeof window.handleOutlookEmail === 'function') {{
                            window.handleOutlookEmail('{safeEmail}', null);
                        }}
                    ");
                }
                catch (Exception jsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[OUTLOOK] JS callback error: {jsEx.Message}");
                }
            }
            catch (Exception ex)
            {
                // Ultimate fallback - this should never crash
                System.Diagnostics.Debug.WriteLine($"[OUTLOOK CRITICAL ERROR] {ex.Message}");
                try
                {
                    string fallbackEmail = "user@grfruits.com";
                    await wv.CoreWebView2.ExecuteScriptAsync($@"
                        if (typeof window.handleOutlookEmail === 'function') {{
                            window.handleOutlookEmail('{fallbackEmail}', null);
                        }}
                    ");
                }
                catch
                {
                    // Silently fail - don't crash the app
                }
            }
        }

        /// <summary>
        /// Sends an email via Outlook (with crash protection)
        /// </summary>
        private async Task HandleSendOutlookEmail(WebView2 wv, string messageJson, string requestId)
        {
            string to = "";
            string subject = "";
            string htmlBody = "";

            try
            {
                // Parse message JSON safely
                try
                {
                    using (var doc = JsonDocument.Parse(messageJson))
                    {
                        var root = doc.RootElement;
                        to = root.TryGetProperty("to", out var toEl) ? toEl.GetString() ?? "" : "";
                        subject = root.TryGetProperty("subject", out var subEl) ? subEl.GetString() ?? "" : "";
                        htmlBody = root.TryGetProperty("htmlBody", out var bodyEl) ? bodyEl.GetString() ?? "" : "";
                    }
                }
                catch (Exception parseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMAIL] JSON parse error: {parseEx.Message}");
                    await SendEmailResult(wv, false, "Failed to parse email data");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[EMAIL] Sending email to: {to}");
                System.Diagnostics.Debug.WriteLine($"[EMAIL] Subject: {subject}");

                if (string.IsNullOrEmpty(to))
                {
                    await SendEmailResult(wv, false, "Recipient email is required");
                    return;
                }

                // Try Outlook first
                bool outlookSuccess = false;
                string outlookError = "";

                try
                {
                    Type outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic outlookApp = Activator.CreateInstance(outlookType);
                        if (outlookApp != null)
                        {
                            dynamic mailItem = outlookApp.CreateItem(0); // 0 = olMailItem
                            if (mailItem != null)
                            {
                                mailItem.To = to;
                                mailItem.Subject = subject;
                                mailItem.HTMLBody = htmlBody;
                                mailItem.Send();
                                outlookSuccess = true;
                                System.Diagnostics.Debug.WriteLine("[EMAIL] Email sent successfully via Outlook");
                            }
                        }
                    }
                    else
                    {
                        outlookError = "Outlook is not installed";
                    }
                }
                catch (Exception outlookEx)
                {
                    outlookError = outlookEx.Message;
                    System.Diagnostics.Debug.WriteLine($"[EMAIL] Outlook error (non-fatal): {outlookEx.Message}");
                }

                if (outlookSuccess)
                {
                    await SendEmailResult(wv, true, "Email sent successfully");
                    return;
                }

                // Fallback: Try to open default email client
                try
                {
                    string plainBody = "";
                    try
                    {
                        plainBody = System.Text.RegularExpressions.Regex.Replace(htmlBody ?? "", "<.*?>", " ");
                        plainBody = System.Text.RegularExpressions.Regex.Replace(plainBody, @"\s+", " ").Trim();
                        if (plainBody.Length > 1500) plainBody = plainBody.Substring(0, 1500) + "...";
                    }
                    catch { plainBody = "See attached content"; }

                    string mailto = $"mailto:{Uri.EscapeDataString(to)}?subject={Uri.EscapeDataString(subject ?? "")}&body={Uri.EscapeDataString(plainBody)}";

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = mailto,
                        UseShellExecute = true
                    });

                    await SendEmailResult(wv, true, "Email client opened. Please send manually.");
                }
                catch (Exception mailtoEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[EMAIL] Mailto fallback error: {mailtoEx.Message}");
                    string errorMsg = !string.IsNullOrEmpty(outlookError) ? outlookError : mailtoEx.Message;
                    await SendEmailResult(wv, false, $"Could not send email: {errorMsg}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMAIL CRITICAL ERROR] {ex.Message}");
                await SendEmailResult(wv, false, $"Email error: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to send email result back to JavaScript safely
        /// </summary>
        private async Task SendEmailResult(WebView2 wv, bool success, string message)
        {
            try
            {
                string safeMessage = (message ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
                await wv.CoreWebView2.ExecuteScriptAsync($@"
                    if (typeof window.handleEmailSendResult === 'function') {{
                        window.handleEmailSendResult({(success ? "true" : "false")}, '{safeMessage}');
                    }}
                ");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EMAIL] Failed to send result to JS: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends an email via SMTP (Office 365, Gmail, etc.)
        /// </summary>
        private async Task HandleSendSmtpEmail(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string smtpServer = root.TryGetProperty("smtpServer", out var srv) ? srv.GetString() ?? "" : "";
                    int smtpPort = root.TryGetProperty("smtpPort", out var prt) ? prt.GetInt32() : 587;
                    string username = root.TryGetProperty("username", out var usr) ? usr.GetString() ?? "" : "";
                    string password = root.TryGetProperty("password", out var pwd) ? pwd.GetString() ?? "" : "";
                    bool useSsl = root.TryGetProperty("useSsl", out var ssl) ? ssl.GetBoolean() : true;
                    string from = root.TryGetProperty("from", out var frm) ? frm.GetString() ?? "" : "";
                    string to = root.TryGetProperty("to", out var toEl) ? toEl.GetString() ?? "" : "";
                    string subject = root.TryGetProperty("subject", out var sub) ? sub.GetString() ?? "" : "";
                    string htmlBody = root.TryGetProperty("htmlBody", out var body) ? body.GetString() ?? "" : "";

                    System.Diagnostics.Debug.WriteLine($"[SMTP] Sending email via {smtpServer}:{smtpPort}");
                    System.Diagnostics.Debug.WriteLine($"[SMTP] From: {from}, To: {to}");

                    if (string.IsNullOrEmpty(to))
                    {
                        await SendEmailResult(wv, false, "Recipient email is required");
                        return;
                    }

                    try
                    {
                        using (var client = new System.Net.Mail.SmtpClient(smtpServer, smtpPort))
                        {
                            client.EnableSsl = useSsl;
                            client.Credentials = new System.Net.NetworkCredential(username, password);
                            client.Timeout = 30000; // 30 seconds

                            var mailMessage = new System.Net.Mail.MailMessage();
                            mailMessage.From = new System.Net.Mail.MailAddress(from);

                            // Handle multiple recipients
                            foreach (var recipient in to.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                mailMessage.To.Add(recipient.Trim());
                            }

                            mailMessage.Subject = subject;
                            mailMessage.Body = htmlBody;
                            mailMessage.IsBodyHtml = true;

                            await Task.Run(() => client.Send(mailMessage));

                            System.Diagnostics.Debug.WriteLine("[SMTP] Email sent successfully");
                            await SendEmailResult(wv, true, "Email sent successfully via SMTP");
                        }
                    }
                    catch (System.Net.Mail.SmtpException smtpEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SMTP] SMTP error: {smtpEx.Message}");
                        string errorMsg = smtpEx.Message;
                        if (smtpEx.StatusCode == System.Net.Mail.SmtpStatusCode.MustIssueStartTlsFirst)
                        {
                            errorMsg = "TLS required. Please enable 'Use TLS/SSL' in settings.";
                        }
                        else if (smtpEx.Message.Contains("5.7.57") || smtpEx.Message.Contains("authentication"))
                        {
                            errorMsg = "Authentication failed. Check your email and password. For Office 365 with MFA, use an App Password.";
                        }
                        await SendEmailResult(wv, false, errorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SMTP ERROR] {ex.Message}");
                await SendEmailResult(wv, false, $"SMTP error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests SMTP connection
        /// </summary>
        private async Task HandleTestSmtpConnection(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                using (var doc = JsonDocument.Parse(messageJson))
                {
                    var root = doc.RootElement;
                    string smtpServer = root.TryGetProperty("smtpServer", out var srv) ? srv.GetString() ?? "" : "";
                    int smtpPort = root.TryGetProperty("smtpPort", out var prt) ? prt.GetInt32() : 587;
                    string username = root.TryGetProperty("username", out var usr) ? usr.GetString() ?? "" : "";
                    string password = root.TryGetProperty("password", out var pwd) ? pwd.GetString() ?? "" : "";
                    bool useSsl = root.TryGetProperty("useSsl", out var ssl) ? ssl.GetBoolean() : true;

                    System.Diagnostics.Debug.WriteLine($"[SMTP TEST] Testing connection to {smtpServer}:{smtpPort}");

                    try
                    {
                        using (var client = new System.Net.Mail.SmtpClient(smtpServer, smtpPort))
                        {
                            client.EnableSsl = useSsl;
                            client.Credentials = new System.Net.NetworkCredential(username, password);
                            client.Timeout = 15000; // 15 seconds for test

                            // Try to connect by sending a NOOP command (no actual email sent)
                            // SmtpClient doesn't have a direct "test" method, so we'll try a minimal operation
                            // The connection will be established when we access the ServicePoint
                            await Task.Run(() =>
                            {
                                // Create a minimal test message
                                var testMsg = new System.Net.Mail.MailMessage();
                                testMsg.From = new System.Net.Mail.MailAddress(username);
                                testMsg.To.Add(username);
                                testMsg.Subject = "Test";
                                testMsg.Body = "Test";

                                // This will throw if connection fails
                                // We're not actually sending, just testing the connection setup
                                using (var tcpClient = new System.Net.Sockets.TcpClient())
                                {
                                    tcpClient.Connect(smtpServer, smtpPort);
                                    tcpClient.Close();
                                }
                            });

                            System.Diagnostics.Debug.WriteLine("[SMTP TEST] Connection successful");
                            await SendSmtpTestResult(wv, true, $"Successfully connected to {smtpServer}:{smtpPort}");
                        }
                    }
                    catch (Exception connEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SMTP TEST] Connection failed: {connEx.Message}");
                        string errorMsg = connEx.Message;
                        if (connEx.Message.Contains("5.7.57") || connEx.Message.Contains("authentication"))
                        {
                            errorMsg = "Authentication failed. Check your credentials.";
                        }
                        else if (connEx.Message.Contains("timed out"))
                        {
                            errorMsg = "Connection timed out. Check server address and port.";
                        }
                        await SendSmtpTestResult(wv, false, errorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SMTP TEST ERROR] {ex.Message}");
                await SendSmtpTestResult(wv, false, ex.Message);
            }
        }

        /// <summary>
        /// Helper method to send SMTP test result back to JavaScript
        /// </summary>
        private async Task SendSmtpTestResult(WebView2 wv, bool success, string message)
        {
            try
            {
                string safeMessage = (message ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
                await wv.CoreWebView2.ExecuteScriptAsync($@"
                    if (typeof window.handleSmtpTestResult === 'function') {{
                        window.handleSmtpTestResult({(success ? "true" : "false")}, '{safeMessage}');
                    }}
                ");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SMTP TEST] Failed to send result to JS: {ex.Message}");
            }
        }

        private async Task HandleGetVersionInfo(WebView2 wv, string messageJson, string requestId)
        {
            try
            {
                string repoRoot = FindRepoRoot();
                if (string.IsNullOrEmpty(repoRoot))
                {
                    await wv.CoreWebView2.ExecuteScriptAsync($@"
                        if (typeof window.handleVersionInfo === 'function') {{
                            window.handleVersionInfo(null, 'Could not find repository root');
                        }}
                    ");
                    return;
                }

                string versionFilePath = Path.Combine(repoRoot, "version.json");
                if (!File.Exists(versionFilePath))
                {
                    await wv.CoreWebView2.ExecuteScriptAsync($@"
                        if (typeof window.handleVersionInfo === 'function') {{
                            window.handleVersionInfo(null, 'version.json not found at: {versionFilePath.Replace("\\", "\\\\")}');
                        }}
                    ");
                    return;
                }

                string versionJson = File.ReadAllText(versionFilePath);
                string escapedJson = versionJson.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "");

                await wv.CoreWebView2.ExecuteScriptAsync($@"
                    if (typeof window.handleVersionInfo === 'function') {{
                        window.handleVersionInfo({versionJson}, null);
                    }}
                ");

                System.Diagnostics.Debug.WriteLine($"[VERSION INFO] Sent version info from: {versionFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VERSION INFO ERROR] {ex.Message}");
                string escapedError = ex.Message.Replace("\\", "\\\\").Replace("'", "\\'");
                await wv.CoreWebView2.ExecuteScriptAsync($@"
                    if (typeof window.handleVersionInfo === 'function') {{
                        window.handleVersionInfo(null, '{escapedError}');
                    }}
                ");
            }
        }

        private string FindRepoRoot()
        {
            // Try common locations
            string[] possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "javeedin", "graysWMSwebviewnew"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "graysWMSwebviewnew"),
                @"C:\fusion\fusionclientweb\graysWMSwebviewnew",
                Path.GetDirectoryName(Application.ExecutablePath)
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "version.json")))
                {
                    return path;
                }
            }

            // Try to find from current executable location
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            while (!string.IsNullOrEmpty(exeDir))
            {
                if (File.Exists(Path.Combine(exeDir, "version.json")))
                {
                    return exeDir;
                }
                exeDir = Path.GetDirectoryName(exeDir);
            }

            return null;
        }

        private async Task SendReleaseProgress(WebView2 wv, int step, string message, string type)
        {
            string escapedMessage = message.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n");
            await wv.CoreWebView2.ExecuteScriptAsync($@"
                if (typeof window.handleReleaseProgress === 'function') {{
                    window.handleReleaseProgress({step}, '{escapedMessage}', '{type}');
                }}
            ");
        }

        private async Task SendReleaseComplete(WebView2 wv, string version)
        {
            await wv.CoreWebView2.ExecuteScriptAsync($@"
                if (typeof window.handleReleaseComplete === 'function') {{
                    window.handleReleaseComplete('{version}');
                }}
            ");
        }

        private async Task SendReleaseError(WebView2 wv, string error)
        {
            string escapedError = error.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n");
            await wv.CoreWebView2.ExecuteScriptAsync($@"
                if (typeof window.handleReleaseError === 'function') {{
                    window.handleReleaseError('{escapedError}');
                }}
            ");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.T))
            {
                AddNewTab("https://www.google.com");
                return true;
            }
            else if (keyData == (Keys.Control | Keys.W))
            {
                var currentTab = GetCurrentTab();
                if (currentTab != null)
                    TabButton_CloseClicked(currentTab, EventArgs.Empty);
                return true;
            }
            else if (keyData == (Keys.Control | Keys.O))
            {
                OpenFileButton_Click(null, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    // ========== CUSTOM TAB BUTTON ==========
    public class CustomTabButton : Control
    {
        private string tabText = "New Tab";
        private bool isSelected = false;
        private bool closeHover = false;
        private Rectangle closeRect;
        public WebView2 WebView { get; set; }
        public event EventHandler CloseClicked;

        public string TabText
        {
            get => tabText;
            set
            {
                tabText = value;
                Invalidate();
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                Invalidate();
            }
        }

        public CustomTabButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            Rectangle iconRect = new Rectangle(8, 6, 16, 16);
            using (Pen pen = new Pen(Color.LightGray, 1.5f))
            {
                e.Graphics.DrawEllipse(pen, iconRect);
            }

            Rectangle textRect = new Rectangle(30, 0, Width - 60, Height);
            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                e.Graphics.DrawString(tabText, new Font("Segoe UI", 8), textBrush, textRect, sf);
            }

            closeRect = new Rectangle(Width - 22, 6, 16, 16);
            Color closeColor = closeHover ? Color.Red : Color.LightGray;
            using (Pen closePen = new Pen(closeColor, 2))
            {
                e.Graphics.DrawLine(closePen, closeRect.Left + 3, closeRect.Top + 3,
                                   closeRect.Right - 3, closeRect.Bottom - 3);
                e.Graphics.DrawLine(closePen, closeRect.Right - 3, closeRect.Top + 3,
                                   closeRect.Left + 3, closeRect.Bottom - 3);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool wasHover = closeHover;
            closeHover = closeRect.Contains(e.Location);
            if (wasHover != closeHover)
                Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            closeHover = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (closeRect.Contains(e.Location))
            {
                CloseClicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    // ========== MESSAGE CLASSES ==========
    // ========== MESSAGE CLASSES ==========
    public class RestApiWebMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("url")]
        public string FullUrl { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    public class RestApiPostWebMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        [JsonPropertyName("url")]
        public string FullUrl { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }
    }

}