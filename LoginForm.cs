using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace WMSApp
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private ComboBox cboInstance;
        private ComboBox cboBusinessUnit;
        private ComboBox cboInventoryOrg;
        private Label lblCompanyName;
        private Button btnLogin;
        private Button btnCancel;
        private Label lblError;
        private CheckBox chkRememberMe;

        // Company Name (hardcoded)
        private const string COMPANY_NAME = "Mitsumi";

        // Instance URLs - loaded from C:\fusionclient\ERP\settings\endpoints.xml
        private Dictionary<string, string> instanceUrls = new Dictionary<string, string>();

        public string Username { get; private set; }
        public string Password { get; private set; }
        public string InstanceName { get; private set; }
        public string BusinessUnit { get; private set; }
        public string InventoryOrg { get; private set; }
        public string CompanyName { get; private set; }
        public bool LoginSuccessful { get; private set; }

        public LoginForm()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] ========================================");
            System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] Constructor called");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] InitializeComponent completed");
            LoadSettings();
            System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] LoadSettings completed");
            System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] Form ready to be shown");
        }

        private void InitializeComponent()
        {
            this.Text = "Fusion Client Login";
            this.Size = new Size(600, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Logo/Title Panel
            Panel headerPanel = new Panel
            {
                Height = 120,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(102, 126, 234) // #667eea
            };

            Label lblTitle = new Label
            {
                Text = "Fusion Client",
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 10),
                Size = new Size(600, 50)
            };
            headerPanel.Controls.Add(lblTitle);

            // Company Name Label
            lblCompanyName = new Label
            {
                Text = COMPANY_NAME,
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 220, 255),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 60),
                Size = new Size(600, 30)
            };
            headerPanel.Controls.Add(lblCompanyName);

            // Main content panel
            Panel contentPanel = new Panel
            {
                Location = new Point(40, 140),
                Size = new Size(520, 450),
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            // Add subtle shadow effect
            contentPanel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, contentPanel.ClientRectangle,
                    Color.FromArgb(220, 220, 220), ButtonBorderStyle.Solid);
            };

            int yPosition = 20;

            // Username
            Label lblUsername = new Label
            {
                Text = "Username:",
                Location = new Point(20, yPosition),
                Size = new Size(480, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            contentPanel.Controls.Add(lblUsername);

            txtUsername = new TextBox
            {
                Location = new Point(20, yPosition + 22),
                Size = new Size(480, 30),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            contentPanel.Controls.Add(txtUsername);
            yPosition += 65;

            // Password
            Label lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(20, yPosition),
                Size = new Size(480, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            contentPanel.Controls.Add(lblPassword);

            txtPassword = new TextBox
            {
                Location = new Point(20, yPosition + 22),
                Size = new Size(480, 30),
                Font = new Font("Segoe UI", 10),
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            contentPanel.Controls.Add(txtPassword);
            yPosition += 65;

            // Instance Name
            Label lblInstance = new Label
            {
                Text = "Instance:",
                Location = new Point(20, yPosition),
                Size = new Size(480, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            contentPanel.Controls.Add(lblInstance);

            cboInstance = new ComboBox
            {
                Location = new Point(20, yPosition + 22),
                Size = new Size(480, 30),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // Hardcode Instance options
            cboInstance.Items.AddRange(new object[] { "PROD", "TEST" });
            cboInstance.SelectedIndex = 0;
            contentPanel.Controls.Add(cboInstance);
            yPosition += 65;

            // Load endpoint URLs from C:\fusionclient\ERP\settings\endpoints.xml
            LoadEndpointsFromSettings();

            // Business Unit
            Label lblBusinessUnit = new Label
            {
                Text = "Business Unit:",
                Location = new Point(20, yPosition),
                Size = new Size(480, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            contentPanel.Controls.Add(lblBusinessUnit);

            cboBusinessUnit = new ComboBox
            {
                Location = new Point(20, yPosition + 22),
                Size = new Size(480, 30),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = true
            };
            cboBusinessUnit.Items.AddRange(new object[] { "Mitsumi BU 01", "Mitsumi BU 02", "Mitsumi BU 03" });
            cboBusinessUnit.SelectedIndex = 0;
            contentPanel.Controls.Add(cboBusinessUnit);
            yPosition += 65;

            // Remember Me
            chkRememberMe = new CheckBox
            {
                Text = "Remember my credentials",
                Location = new Point(20, yPosition),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            contentPanel.Controls.Add(chkRememberMe);
            yPosition += 30;

            // Error Label
            lblError = new Label
            {
                Location = new Point(20, yPosition),
                Size = new Size(480, 40),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            contentPanel.Controls.Add(lblError);

            // Buttons Panel
            Panel buttonPanel = new Panel
            {
                Location = new Point(40, 605),
                Size = new Size(520, 50),
                BackColor = Color.Transparent
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(100, 10),
                Size = new Size(160, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 220, 220),
                ForeColor = Color.FromArgb(80, 80, 80),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += BtnCancel_Click;
            buttonPanel.Controls.Add(btnCancel);

            btnLogin = new Button
            {
                Text = "🔐 Login",
                Location = new Point(270, 10),
                Size = new Size(160, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(102, 126, 234),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;
            buttonPanel.Controls.Add(btnLogin);

            // Add all panels to form
            this.Controls.Add(buttonPanel);
            this.Controls.Add(contentPanel);
            this.Controls.Add(headerPanel);

            // Set Accept button
            this.AcceptButton = btnLogin;

            // Focus on username
            this.Shown += (s, e) => {
                System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] ========================================");
                System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] FORM SHOWN EVENT FIRED!");
                System.Diagnostics.Debug.WriteLine("[DEBUG LoginForm] Form is now visible to user");
                txtUsername.Focus();
            };
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            // Hide previous error
            lblError.Visible = false;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                ShowError("Please enter username");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                ShowError("Please enter password");
                txtPassword.Focus();
                return;
            }

            // Disable controls during login
            SetControlsEnabled(false);
            btnLogin.Text = "⏳ Logging in...";

            try
            {
                // Get selected instance
                string selectedInstance = cboInstance.SelectedItem.ToString();

                // Get base URL for the selected instance
                if (!instanceUrls.TryGetValue(selectedInstance, out string baseUrl))
                {
                    ShowError("Invalid instance selected");
                    SetControlsEnabled(true);
                    btnLogin.Text = "🔐 Login";
                    return;
                }

                // VALIDATE credentials against the API
                System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Validating credentials against API");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Username: {txtUsername.Text}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Instance: {selectedInstance}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] BaseURL: {baseUrl}");

                bool isValid = await ValidateLogin(baseUrl, txtUsername.Text, txtPassword.Text);

                if (!isValid)
                {
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] ✗ Login FAILED - Invalid credentials");
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");
                    ShowError("Invalid username or password. Please try again.");
                    SetControlsEnabled(true);
                    btnLogin.Text = "🔐 Login";
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[LOGIN] ✓ Login SUCCESSFUL (Validated)");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");

                // Store credentials
                Username = txtUsername.Text;
                Password = txtPassword.Text;
                InstanceName = selectedInstance;
                BusinessUnit = cboBusinessUnit.SelectedItem?.ToString();
                InventoryOrg = cboInventoryOrg?.SelectedItem?.ToString();
                LoginSuccessful = true;

                // Save settings if remember me is checked
                if (chkRememberMe.Checked)
                {
                    SaveSettings();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
                SetControlsEnabled(true);
                btnLogin.Text = "🔐 Login";
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Error: {ex.Message}");
            }
        }

        private async Task<bool> ValidateLogin(string baseUrl, string username, string password)
        {
            try
            {
                // Validate the base URL before making the request
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    throw new InvalidOperationException("Login URL is not configured. Please add LOGIN endpoints in Settings.");
                }

                if (!Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
                {
                    throw new InvalidOperationException($"Invalid login URL format: '{baseUrl}'. Please check LOGIN endpoints in Settings.");
                }

                // Use URL+Path as-is, just append query parameters
                string loginUrl = $"{baseUrl}?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

                System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Attempting login...");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] URL: {loginUrl}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Username: {username}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Instance: {baseUrl}");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Sending HTTP GET request...");
                    var response = await client.GetAsync(loginUrl);

                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Response Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Response Success: {response.IsSuccessStatusCode}");

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Response Length: {jsonResponse?.Length ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Response Body: {jsonResponse}");

                    if (response.IsSuccessStatusCode)
                    {
                        // Check if response has data (successful login)
                        if (!string.IsNullOrWhiteSpace(jsonResponse) && jsonResponse.Length > 10)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LOGIN] ✓ Login SUCCESSFUL");
                            System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[LOGIN] ✗ Login FAILED - Empty or invalid response");
                            System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LOGIN] ✗ Login FAILED - HTTP {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] ✗ EXCEPTION: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Stack: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] ========================================");
                throw;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            LoginSuccessful = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        private void SetControlsEnabled(bool enabled)
        {
            txtUsername.Enabled = enabled;
            txtPassword.Enabled = enabled;
            cboInstance.Enabled = enabled;
            btnLogin.Enabled = enabled;
            btnCancel.Enabled = enabled;
        }

        private void LoadSettings()
        {
            try
            {
                string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WMS", "login.json");
                if (File.Exists(settingsFile))
                {
                    string json = File.ReadAllText(settingsFile);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (settings != null)
                    {
                        if (settings.ContainsKey("username"))
                        {
                            txtUsername.Text = settings["username"];
                            chkRememberMe.Checked = true;
                        }
                        if (settings.ContainsKey("instance"))
                        {
                            cboInstance.SelectedItem = settings["instance"];
                        }

                        System.Diagnostics.Debug.WriteLine($"[LOGIN] Loaded saved credentials for: {settings.GetValueOrDefault("username", "N/A")}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Error loading settings: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WMS");
                Directory.CreateDirectory(settingsDir);

                string settingsFile = Path.Combine(settingsDir, "login.json");

                var settings = new Dictionary<string, string>
                {
                    { "username", txtUsername.Text },
                    { "instance", cboInstance.SelectedItem?.ToString() ?? "PROD" }
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, json);

                System.Diagnostics.Debug.WriteLine($"[LOGIN] Saved credentials for: {txtUsername.Text}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Error saving settings: {ex.Message}");
            }
        }

        private void LoadEndpointsFromSettings()
        {
            try
            {
                // Debug: Show where we're looking for settings
                string settingsPath = EndpointConfigReader.GetSettingsPath();
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Settings path: {settingsPath}");

                // Use EndpointConfigReader to get LOGIN endpoints by IntegrationCode
                var allEndpoints = EndpointConfigReader.LoadEndpoints();
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Total endpoints loaded: {allEndpoints.Count}");

                var loginEndpoints = EndpointConfigReader.GetByIntegrationCode("LOGIN");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] LOGIN endpoints found: {loginEndpoints.Count}");

                // Filter to APEX source only for login (supports "APEX", "APEX-PROD", "APEX-TEST", etc.)
                foreach (var endpoint in loginEndpoints)
                {
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Checking endpoint: Source={endpoint.Source}, Instance={endpoint.InstanceName}, URL={endpoint.FullUrl}");
                    if (endpoint.Source != null && endpoint.Source.StartsWith("APEX", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!instanceUrls.ContainsKey(endpoint.InstanceName) && !string.IsNullOrWhiteSpace(endpoint.FullUrl))
                        {
                            instanceUrls[endpoint.InstanceName] = endpoint.FullUrl;
                            System.Diagnostics.Debug.WriteLine($"[LOGIN] Added endpoint: {endpoint.InstanceName} -> {endpoint.FullUrl}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[LOGIN] Instance URLs count: {instanceUrls.Count}");

                // If PROD or TEST URLs not found in settings, use hardcoded defaults
                if (!instanceUrls.ContainsKey("PROD"))
                {
                    instanceUrls["PROD"] = "https://g09254cbbf8e7af-graysprod.adb.eu-frankfurt-1.oraclecloudapps.com/ords/WKSP_GRAYSAPP/wms_login";
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Using default PROD URL");
                }
                if (!instanceUrls.ContainsKey("TEST"))
                {
                    instanceUrls["TEST"] = "https://g09254cbbf8e7af-graystest.adb.eu-frankfurt-1.oraclecloudapps.com/ords/WKSP_GRAYSAPP/wms_login";
                    System.Diagnostics.Debug.WriteLine($"[LOGIN] Using default TEST URL");
                }

                System.Diagnostics.Debug.WriteLine($"[LOGIN] Final PROD URL: {instanceUrls["PROD"]}");
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Final TEST URL: {instanceUrls["TEST"]}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LOGIN] Error loading endpoints: {ex.Message}");
                // Don't call ShowError here - lblError may not be initialized yet
            }
        }
    }
}
