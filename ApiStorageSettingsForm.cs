using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WMSApp
{
    /// <summary>
    /// Form for configuring API Storage endpoint settings
    /// This endpoint is used to store all endpoints and instances remotely
    /// </summary>
    public class ApiStorageSettingsForm : UserControl
    {
        private TextBox txtUrl;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private CheckBox chkNoAuthentication;
        private Button btnTest;
        private Button btnSave;
        private Label lblStatus;
        private ApiStorageConfig _config;
        private bool _isDirty = false;

        public event EventHandler ConfigSaved;

        public ApiStorageSettingsForm()
        {
            _config = ApiStorageConfig.Load();
            InitializeComponent();
            LoadConfig();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Padding = new Padding(20);

            // Main container panel with scrolling
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(30)
            };

            int yPos = 30;
            int labelWidth = 150;
            int fieldLeft = 180;
            int fieldWidth = 500;
            int rowHeight = 60;

            // Title
            Label lblTitle = new Label
            {
                Text = "API Storage Endpoint Configuration",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(30, yPos),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblTitle);
            yPos += 40;

            // Description
            Label lblDescription = new Label
            {
                Text = "Configure the REST API endpoint used to store and retrieve endpoints and instances.\n" +
                       "This allows centralized management of configuration across multiple clients.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location = new Point(30, yPos),
                Size = new Size(fieldWidth + 150, 45)
            };
            mainPanel.Controls.Add(lblDescription);
            yPos += 60;

            // Separator
            Panel separator = new Panel
            {
                BackColor = Color.FromArgb(230, 230, 230),
                Location = new Point(30, yPos),
                Size = new Size(fieldWidth + 150, 1)
            };
            mainPanel.Controls.Add(separator);
            yPos += 30;

            // URL Field
            AddLabel(mainPanel, "API URL:", 30, yPos);
            txtUrl = new TextBox
            {
                Location = new Point(fieldLeft, yPos - 3),
                Width = fieldWidth,
                Height = 30,
                Font = new Font("Segoe UI", 11)
            };
            txtUrl.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(txtUrl);

            // URL hint
            Label lblUrlHint = new Label
            {
                Text = "Full URL to the REST API (e.g., https://your-apex-server/ords/schema/endpoints/)",
                Location = new Point(fieldLeft, yPos + 25),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            mainPanel.Controls.Add(lblUrlHint);
            yPos += rowHeight + 10;

            // Username Field
            AddLabel(mainPanel, "Username:", 30, yPos);
            txtUsername = new TextBox
            {
                Location = new Point(fieldLeft, yPos - 3),
                Width = 300,
                Height = 30,
                Font = new Font("Segoe UI", 11)
            };
            txtUsername.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(txtUsername);
            yPos += rowHeight;

            // Password Field
            AddLabel(mainPanel, "Password:", 30, yPos);
            txtPassword = new TextBox
            {
                Location = new Point(fieldLeft, yPos - 3),
                Width = 300,
                Height = 30,
                Font = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };
            txtPassword.TextChanged += OnFieldChanged;
            mainPanel.Controls.Add(txtPassword);

            // Show/Hide password button
            Button btnShowPassword = new Button
            {
                Text = "Show",
                Location = new Point(fieldLeft + 310, yPos - 3),
                Width = 60,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(240, 240, 240),
                Cursor = Cursors.Hand
            };
            btnShowPassword.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnShowPassword.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                btnShowPassword.Text = txtPassword.UseSystemPasswordChar ? "Show" : "Hide";
            };
            mainPanel.Controls.Add(btnShowPassword);
            yPos += rowHeight;

            // No Authentication Checkbox
            chkNoAuthentication = new CheckBox
            {
                Text = "No Authentication Required",
                Location = new Point(fieldLeft, yPos),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            chkNoAuthentication.CheckedChanged += ChkNoAuthentication_CheckedChanged;
            mainPanel.Controls.Add(chkNoAuthentication);

            // Checkbox hint
            Label lblAuthHint = new Label
            {
                Text = "Check this if the API endpoint does not require authentication",
                Location = new Point(fieldLeft + 220, yPos + 3),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            mainPanel.Controls.Add(lblAuthHint);
            yPos += rowHeight + 20;

            // Separator
            Panel separator2 = new Panel
            {
                BackColor = Color.FromArgb(230, 230, 230),
                Location = new Point(30, yPos),
                Size = new Size(fieldWidth + 150, 1)
            };
            mainPanel.Controls.Add(separator2);
            yPos += 30;

            // Action buttons panel
            Panel buttonPanel = new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(fieldWidth + 150, 50),
                BackColor = Color.Transparent
            };

            btnTest = new Button
            {
                Text = "Test Connection",
                Width = 140,
                Height = 40,
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.Click += BtnTest_Click;

            btnSave = new Button
            {
                Text = "Save Settings",
                Width = 130,
                Height = 40,
                Location = new Point(150, 0),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            buttonPanel.Controls.Add(btnTest);
            buttonPanel.Controls.Add(btnSave);
            mainPanel.Controls.Add(buttonPanel);
            yPos += 60;

            // Status label
            lblStatus = new Label
            {
                Text = "",
                Location = new Point(30, yPos),
                Size = new Size(fieldWidth + 150, 25),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray
            };
            mainPanel.Controls.Add(lblStatus);
            yPos += 40;

            // Info box about API endpoints
            Panel infoBox = new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(fieldWidth + 150, 160),
                BackColor = Color.FromArgb(232, 245, 253),
                Padding = new Padding(15)
            };

            Label lblInfoTitle = new Label
            {
                Text = "API Endpoints Used:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 136, 229),
                Location = new Point(15, 10),
                AutoSize = true
            };

            Label lblInfoContent = new Label
            {
                Text = "GET  {base_url}/endpoints          - Retrieve all endpoints\n" +
                       "POST {base_url}/endpoints          - Create new endpoint\n" +
                       "PUT  {base_url}/endpoints/{id}     - Update endpoint\n" +
                       "DELETE {base_url}/endpoints/{id}   - Delete endpoint\n\n" +
                       "GET  {base_url}/instances          - Retrieve all instances\n" +
                       "POST {base_url}/instances          - Create/Update instance",
                Font = new Font("Consolas", 9),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(15, 35),
                Size = new Size(fieldWidth + 100, 120)
            };

            infoBox.Controls.Add(lblInfoTitle);
            infoBox.Controls.Add(lblInfoContent);
            mainPanel.Controls.Add(infoBox);

            this.Controls.Add(mainPanel);
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            parent.Controls.Add(label);
        }

        private void LoadConfig()
        {
            txtUrl.Text = _config.Url ?? "";
            txtUsername.Text = _config.Username ?? "";
            txtPassword.Text = _config.Password ?? "";
            chkNoAuthentication.Checked = _config.NoAuthentication;
            UpdateAuthFieldsState();
            _isDirty = false;
            UpdateStatus("");
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            _isDirty = true;
            UpdateStatus("Changes not saved");
        }

        private void ChkNoAuthentication_CheckedChanged(object sender, EventArgs e)
        {
            UpdateAuthFieldsState();
            _isDirty = true;
            UpdateStatus("Changes not saved");
        }

        private void UpdateAuthFieldsState()
        {
            bool requiresAuth = !chkNoAuthentication.Checked;
            txtUsername.Enabled = requiresAuth;
            txtPassword.Enabled = requiresAuth;
            txtUsername.BackColor = requiresAuth ? Color.White : Color.FromArgb(245, 245, 245);
            txtPassword.BackColor = requiresAuth ? Color.White : Color.FromArgb(245, 245, 245);
        }

        private void UpdateStatus(string message, bool isError = false, bool isSuccess = false)
        {
            lblStatus.Text = message;
            if (isError)
                lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
            else if (isSuccess)
                lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
            else
                lblStatus.ForeColor = Color.Gray;
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                UpdateStatus("Please enter an API URL", isError: true);
                return;
            }

            btnTest.Enabled = false;
            btnTest.Text = "Testing...";
            UpdateStatus("Testing connection...");

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // Add authentication if required
                    if (!chkNoAuthentication.Checked &&
                        !string.IsNullOrEmpty(txtUsername.Text) &&
                        !string.IsNullOrEmpty(txtPassword.Text))
                    {
                        var credentials = Convert.ToBase64String(
                            Encoding.ASCII.GetBytes($"{txtUsername.Text}:{txtPassword.Text}"));
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                    }

                    // Test the endpoints endpoint
                    string testUrl = txtUrl.Text.TrimEnd('/') + "/endpoints";
                    var response = await client.GetAsync(testUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        UpdateStatus($"Connection successful! Status: {(int)response.StatusCode}", isSuccess: true);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        UpdateStatus("Authentication failed. Check username/password.", isError: true);
                    }
                    else
                    {
                        UpdateStatus($"Connection failed. Status: {(int)response.StatusCode} - {response.ReasonPhrase}", isError: true);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                UpdateStatus($"Connection error: {ex.Message}", isError: true);
            }
            catch (TaskCanceledException)
            {
                UpdateStatus("Connection timed out", isError: true);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error: {ex.Message}", isError: true);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "Test Connection";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                MessageBox.Show("API URL is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUrl.Focus();
                return;
            }

            if (!chkNoAuthentication.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Username is required when authentication is enabled.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Password is required when authentication is enabled.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.Focus();
                    return;
                }
            }

            try
            {
                _config.Url = txtUrl.Text.Trim();
                _config.Username = txtUsername.Text.Trim();
                _config.Password = txtPassword.Text;
                _config.NoAuthentication = chkNoAuthentication.Checked;
                _config.Save();

                _isDirty = false;
                UpdateStatus($"Settings saved to: {ApiStorageConfig.GetConfigPath()}", isSuccess: true);
                ConfigSaved?.Invoke(this, EventArgs.Empty);

                MessageBox.Show("API Storage settings saved successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving: {ex.Message}", isError: true);
                MessageBox.Show($"Error saving settings:\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool HasUnsavedChanges => _isDirty;

        public bool PromptSaveChanges()
        {
            if (!_isDirty)
                return true;

            var result = MessageBox.Show(
                "API Storage settings have unsaved changes. Do you want to save them?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                BtnSave_Click(this, EventArgs.Empty);
                return !_isDirty; // Return true if save succeeded
            }
            else if (result == DialogResult.No)
            {
                return true;
            }
            else
            {
                return false; // Cancel
            }
        }
    }
}
