using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WMSApp
{
    /// <summary>
    /// Main settings form with tabbed navigation for all settings pages
    /// Page 1: Endpoint Settings
    /// Page 2: Inventory Settings
    /// Page 3: API Storage Settings
    /// </summary>
    public class SettingsForm : Form
    {
        private TabControl tabControl;
        private TabPage tabEndpoints;
        private TabPage tabInventory;
        private TabPage tabApiStorage;
        private Button btnClose;

        // Page controls
        private EndpointSettingsPanel _endpointPanel;
        private InventorySettingsPanel _inventoryPanel;
        private ApiStorageSettingsForm _apiStoragePanel;

        private string _settingsPath;

        public SettingsForm()
        {
            _settingsPath = @"C:\fusionclient\ERP\settings";
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Settings ({_settingsPath})";
            this.Size = new Size(1150, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(1000, 600);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Header Panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(102, 126, 234)
            };

            Label lblTitle = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            headerPanel.Controls.Add(lblTitle);

            // Tab Control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Padding = new Point(20, 8),
                ItemSize = new Size(180, 40)
            };

            // Style the tabs
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;

            // Page 1: Endpoint Settings
            tabEndpoints = new TabPage
            {
                Text = "1. Endpoint Settings",
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };
            _endpointPanel = new EndpointSettingsPanel(_settingsPath);
            _endpointPanel.Dock = DockStyle.Fill;
            tabEndpoints.Controls.Add(_endpointPanel);

            // Page 2: Inventory Settings
            tabInventory = new TabPage
            {
                Text = "2. Inventory Settings",
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };
            _inventoryPanel = new InventorySettingsPanel();
            _inventoryPanel.Dock = DockStyle.Fill;
            tabInventory.Controls.Add(_inventoryPanel);

            // Page 3: API Storage Settings
            tabApiStorage = new TabPage
            {
                Text = "3. API Storage",
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };
            _apiStoragePanel = new ApiStorageSettingsForm();
            _apiStoragePanel.Dock = DockStyle.Fill;
            tabApiStorage.Controls.Add(_apiStoragePanel);

            tabControl.TabPages.Add(tabEndpoints);
            tabControl.TabPages.Add(tabInventory);
            tabControl.TabPages.Add(tabApiStorage);

            // Footer Panel
            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White
            };

            btnClose = new Button
            {
                Text = "Close",
                Width = 100,
                Height = 35,
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(this.ClientSize.Width - 120, 12);
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnClose.Click += BtnClose_Click;

            footerPanel.Controls.Add(btnClose);

            // Add all panels to form
            this.Controls.Add(tabControl);
            this.Controls.Add(headerPanel);
            this.Controls.Add(footerPanel);

            // Handle form closing
            this.FormClosing += SettingsForm_FormClosing;
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl.TabPages[e.Index];
            Rectangle bounds = tabControl.GetTabRect(e.Index);

            // Determine if this tab is selected
            bool isSelected = (tabControl.SelectedIndex == e.Index);

            // Draw background
            Color backColor = isSelected ? Color.FromArgb(102, 126, 234) : Color.FromArgb(240, 240, 240);
            using (Brush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, bounds);
            }

            // Draw text
            Color textColor = isSelected ? Color.White : Color.FromArgb(60, 60, 60);
            TextRenderer.DrawText(e.Graphics, page.Text, tabControl.Font, bounds, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Check for unsaved changes in each panel
            if (_endpointPanel.HasUnsavedChanges)
            {
                if (!_endpointPanel.PromptSaveChanges())
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (_apiStoragePanel.HasUnsavedChanges)
            {
                if (!_apiStoragePanel.PromptSaveChanges())
                {
                    e.Cancel = true;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Panel for Endpoint Settings (converted from EndpointSettingsForm)
    /// </summary>
    public class EndpointSettingsPanel : UserControl
    {
        private DataGridView dgvEndpoints;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnSave;
        private Button btnRefresh;
        private List<EndpointConfig> _endpoints = new List<EndpointConfig>();
        private bool _isDirty = false;
        private string _settingsPath;
        private string _apexEndpointUrl;

        public bool HasUnsavedChanges => _isDirty;

        public EndpointSettingsPanel(string settingsPath)
        {
            _settingsPath = settingsPath;
            LoadApexEndpointUrl();
            InitializeComponent();
            // Load endpoints asynchronously from APEX
            LoadEndpointsFromApexAsync();
        }

        /// <summary>
        /// Loads the APEX endpoint URL from apexendpointurl.txt file
        /// </summary>
        private void LoadApexEndpointUrl()
        {
            try
            {
                string apexUrlFilePath = Path.Combine(_settingsPath, "apexendpointurl.txt");

                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[APEX URL] LoadApexEndpointUrl STARTED");
                System.Diagnostics.Debug.WriteLine($"[APEX URL] Looking for file: {apexUrlFilePath}");
                System.Diagnostics.Debug.WriteLine($"[APEX URL] Directory exists: {Directory.Exists(_settingsPath)}");
                System.Diagnostics.Debug.WriteLine($"[APEX URL] File exists: {File.Exists(apexUrlFilePath)}");

                if (File.Exists(apexUrlFilePath))
                {
                    _apexEndpointUrl = File.ReadAllText(apexUrlFilePath).Trim();
                    System.Diagnostics.Debug.WriteLine($"[APEX URL] *** SUCCESS - Loaded APEX URL: {_apexEndpointUrl}");
                    System.Diagnostics.Debug.WriteLine($"[APEX URL] URL Length: {_apexEndpointUrl.Length} characters");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[APEX URL] *** WARNING: File not found! ***");
                    System.Diagnostics.Debug.WriteLine($"[APEX URL] Please create file: {apexUrlFilePath}");
                    System.Diagnostics.Debug.WriteLine($"[APEX URL] With content: Your APEX endpoint URL");
                    _apexEndpointUrl = "";
                }
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX URL] *** ERROR loading APEX URL ***");
                System.Diagnostics.Debug.WriteLine($"[APEX URL] Exception: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[APEX URL] Message: {ex.Message}");
                _apexEndpointUrl = "";
            }
        }

        /// <summary>
        /// Loads endpoints from APEX endpoint via HTTP GET
        /// </summary>
        private async void LoadEndpointsFromApexAsync()
        {
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"[APEX FETCH] LoadEndpointsFromApexAsync STARTED");
            System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Settings Path: {_settingsPath}");
            System.Diagnostics.Debug.WriteLine($"[APEX FETCH] APEX URL configured: '{_apexEndpointUrl}'");
            System.Diagnostics.Debug.WriteLine($"========================================");

            if (string.IsNullOrWhiteSpace(_apexEndpointUrl))
            {
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] *** WARNING: No APEX URL configured! ***");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Expected file: {Path.Combine(_settingsPath, "apexendpointurl.txt")}");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Falling back to local XML file");
                LoadEndpointsFromLocal();
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Making HTTP GET request to: {_apexEndpointUrl}");

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Timeout set to 30 seconds");

                    System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Sending request...");
                    var response = await client.GetAsync(_apexEndpointUrl);

                    System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Response received!");
                    System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Status Code: {(int)response.StatusCode} ({response.StatusCode})");
                    System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Reason Phrase: {response.ReasonPhrase}");
                    System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Content-Type: {response.Content?.Headers?.ContentType}");

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] *** SUCCESS - Response received ***");
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Response Length: {jsonResponse?.Length ?? 0} characters");
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Response JSON (first 1000 chars):");
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] {jsonResponse?.Substring(0, Math.Min(jsonResponse?.Length ?? 0, 1000))}");

                        // Parse the JSON response
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Parsing JSON response...");
                        _endpoints = ParseApexResponse(jsonResponse);
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Parsed {_endpoints.Count} endpoints");

                        // Log each endpoint
                        foreach (var ep in _endpoints)
                        {
                            System.Diagnostics.Debug.WriteLine($"[APEX FETCH]   -> Sno={ep.Sno}, Source={ep.Source}, Code={ep.IntegrationCode}, Instance={ep.InstanceName}, BaseUrl={ep.BaseUrl}");
                        }

                        // Update UI on the UI thread
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Updating UI (InvokeRequired={this.InvokeRequired})...");
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() => RefreshGrid()));
                        }
                        else
                        {
                            RefreshGrid();
                        }

                        _isDirty = false;
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] *** COMPLETE - Loaded {_endpoints.Count} endpoints from APEX ***");
                    }
                    else
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] *** HTTP ERROR: {response.StatusCode} ***");
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Error Body: {errorBody}");
                        System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Falling back to local file");
                        LoadEndpointsFromLocal();
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] *** HTTP EXCEPTION ***");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Message: {httpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Inner Exception: {httpEx.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Falling back to local file");
                LoadEndpointsFromLocal();
            }
            catch (TaskCanceledException tcEx)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] *** TIMEOUT EXCEPTION ***");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] The request timed out after 30 seconds");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Message: {tcEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Falling back to local file");
                LoadEndpointsFromLocal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] *** GENERAL EXCEPTION ***");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[APEX FETCH] Falling back to local file");
                LoadEndpointsFromLocal();
            }
        }

        /// <summary>
        /// Parses the APEX JSON response into EndpointConfig list
        /// Values are used exactly as returned - no concatenation or appending
        /// </summary>
        private List<EndpointConfig> ParseApexResponse(string jsonResponse)
        {
            var endpoints = new List<EndpointConfig>();

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;

                    // Check if response is an array or has an "items" property
                    JsonElement itemsArray;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        itemsArray = root;
                    }
                    else if (root.TryGetProperty("items", out itemsArray))
                    {
                        // APEX REST often wraps results in "items"
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[EndpointSettingsPanel] Unexpected JSON structure");
                        return endpoints;
                    }

                    foreach (JsonElement item in itemsArray.EnumerateArray())
                    {
                        var endpoint = new EndpointConfig();

                        // Map fields exactly as they come from the API - no concatenation
                        if (item.TryGetProperty("sno", out JsonElement snoEl))
                            endpoint.Sno = snoEl.TryGetInt32(out int sno) ? sno : 0;
                        else if (item.TryGetProperty("SNO", out snoEl))
                            endpoint.Sno = snoEl.TryGetInt32(out int sno2) ? sno2 : 0;

                        if (item.TryGetProperty("source", out JsonElement sourceEl))
                            endpoint.Source = sourceEl.GetString() ?? "";
                        else if (item.TryGetProperty("SOURCE", out sourceEl))
                            endpoint.Source = sourceEl.GetString() ?? "";

                        if (item.TryGetProperty("integration_code", out JsonElement codeEl))
                            endpoint.IntegrationCode = codeEl.GetString() ?? "";
                        else if (item.TryGetProperty("INTEGRATION_CODE", out codeEl))
                            endpoint.IntegrationCode = codeEl.GetString() ?? "";

                        if (item.TryGetProperty("instance_name", out JsonElement instEl))
                            endpoint.InstanceName = instEl.GetString() ?? "";
                        else if (item.TryGetProperty("INSTANCE_NAME", out instEl))
                            endpoint.InstanceName = instEl.GetString() ?? "";

                        if (item.TryGetProperty("base_url", out JsonElement urlEl))
                            endpoint.BaseUrl = urlEl.GetString() ?? "";
                        else if (item.TryGetProperty("BASE_URL", out urlEl))
                            endpoint.BaseUrl = urlEl.GetString() ?? "";

                        if (item.TryGetProperty("endpoint", out JsonElement epEl))
                            endpoint.Endpoint = epEl.GetString() ?? "";
                        else if (item.TryGetProperty("ENDPOINT", out epEl))
                            endpoint.Endpoint = epEl.GetString() ?? "";

                        if (item.TryGetProperty("comments", out JsonElement commEl))
                            endpoint.Comments = commEl.GetString() ?? "";
                        else if (item.TryGetProperty("COMMENTS", out commEl))
                            endpoint.Comments = commEl.GetString() ?? "";

                        endpoints.Add(endpoint);
                        System.Diagnostics.Debug.WriteLine($"[EndpointSettingsPanel] Parsed endpoint: Sno={endpoint.Sno}, Source={endpoint.Source}, Code={endpoint.IntegrationCode}, Instance={endpoint.InstanceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettingsPanel] Error parsing JSON: {ex.Message}");
            }

            return endpoints;
        }

        /// <summary>
        /// Fallback: Load endpoints from local XML file
        /// </summary>
        private void LoadEndpointsFromLocal()
        {
            try
            {
                if (!Directory.Exists(_settingsPath))
                {
                    Directory.CreateDirectory(_settingsPath);
                }

                EndpointConfigReader.ClearCache();
                _endpoints = EndpointConfigReader.LoadEndpoints(_settingsPath);

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => RefreshGrid()));
                }
                else
                {
                    RefreshGrid();
                }

                _isDirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading endpoints: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _endpoints = new List<EndpointConfig>();
            }
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Description Panel
            Panel descPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            Label lblDesc = new Label
            {
                Text = "Manage endpoint configurations for APEX and Fusion integrations.\n" +
                       $"Storage: {_settingsPath}\\endpoints.xml",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 100, 100),
                Dock = DockStyle.Fill
            };
            descPanel.Controls.Add(lblDesc);

            // Toolbar Panel
            Panel toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            btnAdd = CreateButton("Add New", Color.FromArgb(76, 175, 80), 10);
            btnAdd.Click += BtnAdd_Click;

            btnEdit = CreateButton("Edit", Color.FromArgb(33, 150, 243), 110);
            btnEdit.Click += BtnEdit_Click;

            btnDelete = CreateButton("Delete", Color.FromArgb(244, 67, 54), 210);
            btnDelete.Click += BtnDelete_Click;

            btnRefresh = CreateButton("Refresh from APEX", Color.FromArgb(0, 150, 136), 310);
            btnRefresh.Width = 130; // Wider button for longer text
            btnRefresh.Click += BtnRefresh_Click;

            btnSave = CreateButton("Save", Color.FromArgb(76, 175, 80), 450);
            btnSave.Click += BtnSave_Click;

            toolbarPanel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh, btnSave });

            // DataGridView
            dgvEndpoints = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Font = new Font("Segoe UI", 9),
                RowTemplate = { Height = 30 }
            };

            // Style the header
            dgvEndpoints.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 60, 60);
            dgvEndpoints.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvEndpoints.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvEndpoints.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);
            dgvEndpoints.EnableHeadersVisualStyles = false;
            dgvEndpoints.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvEndpoints.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 255);
            dgvEndpoints.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvEndpoints.CellDoubleClick += DgvEndpoints_CellDoubleClick;

            // Define columns
            dgvEndpoints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Sno", HeaderText = "S.No", Width = 50, FillWeight = 5 });
            dgvEndpoints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "Source", Width = 80, FillWeight = 10 });
            dgvEndpoints.Columns.Add(new DataGridViewTextBoxColumn { Name = "IntegrationCode", HeaderText = "Integration Code", Width = 120, FillWeight = 12 });
            dgvEndpoints.Columns.Add(new DataGridViewTextBoxColumn { Name = "InstanceName", HeaderText = "Instance", Width = 80, FillWeight = 10 });
            dgvEndpoints.Columns.Add(new DataGridViewTextBoxColumn { Name = "BaseUrl", HeaderText = "Base URL", Width = 300, FillWeight = 35 });
            dgvEndpoints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Endpoint", HeaderText = "Endpoint", Width = 150, FillWeight = 15 });
            dgvEndpoints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comments", HeaderText = "Comments", Width = 150, FillWeight = 13 });

            // Content Panel
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 5, 15, 10),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            contentPanel.Controls.Add(dgvEndpoints);

            // Add panels
            this.Controls.Add(contentPanel);
            this.Controls.Add(toolbarPanel);
            this.Controls.Add(descPanel);
        }

        private Button CreateButton(string text, Color backColor, int left)
        {
            var btn = new Button
            {
                Text = text,
                Width = 90,
                Height = 32,
                Left = left,
                Top = 8,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadEndpoints()
        {
            // Reload APEX URL in case it was changed
            LoadApexEndpointUrl();
            // Load from APEX (with fallback to local)
            LoadEndpointsFromApexAsync();
        }

        private void RefreshGrid()
        {
            dgvEndpoints.Rows.Clear();
            foreach (var ep in _endpoints)
            {
                dgvEndpoints.Rows.Add(
                    ep.Sno, ep.Source, ep.IntegrationCode, ep.InstanceName,
                    ep.BaseUrl, ep.Endpoint, ep.Comments
                );
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            int nextSno = _endpoints.Count > 0 ? _endpoints.Max(ep => ep.Sno) + 1 : 1;
            var editForm = new EndpointEditForm(new EndpointConfig { Sno = nextSno });
            if (editForm.ShowDialog() == DialogResult.OK)
            {
                _endpoints.Add(editForm.Endpoint);
                RefreshGrid();
                _isDirty = true;
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            EditSelectedRow();
        }

        private void DgvEndpoints_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                EditSelectedRow();
        }

        private void EditSelectedRow()
        {
            if (dgvEndpoints.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rowIndex = dgvEndpoints.SelectedRows[0].Index;
            if (rowIndex >= 0 && rowIndex < _endpoints.Count)
            {
                var editForm = new EndpointEditForm(_endpoints[rowIndex]);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    _endpoints[rowIndex] = editForm.Endpoint;
                    RefreshGrid();
                    _isDirty = true;
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvEndpoints.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rowIndex = dgvEndpoints.SelectedRows[0].Index;
            if (rowIndex >= 0 && rowIndex < _endpoints.Count)
            {
                var endpoint = _endpoints[rowIndex];
                var result = MessageBox.Show(
                    $"Delete endpoint?\n\nSource: {endpoint.Source}\nCode: {endpoint.IntegrationCode}\nInstance: {endpoint.InstanceName}",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    _endpoints.RemoveAt(rowIndex);
                    RefreshGrid();
                    _isDirty = true;
                }
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"========================================");
            System.Diagnostics.Debug.WriteLine($"[APEX REFRESH] User clicked 'Refresh from APEX' button");
            System.Diagnostics.Debug.WriteLine($"========================================");

            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "Unsaved changes will be lost. Continue?",
                    "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    return;
            }

            // Show info about the refresh
            string apexUrlFilePath = Path.Combine(_settingsPath, "apexendpointurl.txt");
            string statusMessage = $"Refreshing from APEX...\n\n" +
                $"Settings Path: {_settingsPath}\n" +
                $"APEX URL File: {apexUrlFilePath}\n" +
                $"File Exists: {File.Exists(apexUrlFilePath)}\n" +
                $"Current APEX URL: {(string.IsNullOrWhiteSpace(_apexEndpointUrl) ? "(not configured)" : _apexEndpointUrl)}\n\n" +
                $"Check Visual Studio Output window for detailed logs.\n" +
                $"(Debug -> Windows -> Output)";

            MessageBox.Show(statusMessage, "Refresh from APEX", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadEndpoints();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                EndpointConfigReader.SaveEndpoints(_endpoints, _settingsPath);
                _isDirty = false;
                MessageBox.Show($"Saved {_endpoints.Count} endpoints.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool PromptSaveChanges()
        {
            if (!_isDirty) return true;

            var result = MessageBox.Show(
                "Endpoint settings have unsaved changes. Save now?",
                "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                BtnSave_Click(this, EventArgs.Empty);
                return !_isDirty;
            }
            else if (result == DialogResult.No)
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Panel for Inventory Settings
    /// </summary>
    public class InventorySettingsPanel : UserControl
    {
        public InventorySettingsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(245, 247, 250);

            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(30)
            };

            int yPos = 30;

            // Title
            Label lblTitle = new Label
            {
                Text = "Inventory Settings",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(30, yPos),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblTitle);
            yPos += 50;

            // Description
            Label lblDesc = new Label
            {
                Text = "Configure inventory-related settings and organization defaults.\n\n" +
                       "Note: Organization selection is managed through the Inventory button on the main toolbar.\n" +
                       "Endpoint configurations for inventory (INVENTORY_ORGS) should be set up in Page 1.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(30, yPos),
                Size = new Size(600, 80)
            };
            mainPanel.Controls.Add(lblDesc);
            yPos += 100;

            // Info box
            Panel infoBox = new Panel
            {
                Location = new Point(30, yPos),
                Size = new Size(600, 120),
                BackColor = Color.FromArgb(255, 248, 225),
                Padding = new Padding(15)
            };

            Label lblInfoTitle = new Label
            {
                Text = "Required Endpoint Configuration:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 124, 0),
                Location = new Point(15, 10),
                AutoSize = true
            };

            Label lblInfoContent = new Label
            {
                Text = "For inventory organization features to work, ensure you have configured:\n\n" +
                       "Source: APEX\n" +
                       "Integration Code: INVENTORY_ORGS\n" +
                       "Instance: Your instance (PROD/TEST/DEV)",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(15, 35),
                Size = new Size(560, 80)
            };

            infoBox.Controls.Add(lblInfoTitle);
            infoBox.Controls.Add(lblInfoContent);
            mainPanel.Controls.Add(infoBox);
            yPos += 140;

            // Current organization info
            Label lblCurrentOrg = new Label
            {
                Text = $"Current Organization: {(SessionManager.HasOrganization ? SessionManager.OrganizationDisplayName : "None selected")}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location = new Point(30, yPos),
                AutoSize = true
            };
            mainPanel.Controls.Add(lblCurrentOrg);

            this.Controls.Add(mainPanel);
        }
    }
}
