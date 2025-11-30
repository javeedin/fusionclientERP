using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WMSApp
{
    /// <summary>
    /// Form for managing endpoint configurations
    /// </summary>
    public class EndpointSettingsForm : Form
    {
        private DataGridView dgvEndpoints;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnSave;
        private Button btnCancel;
        private Button btnRefresh;
        private List<EndpointConfig> _endpoints;
        private bool _isDirty = false;
        private string _settingsPath;

        public EndpointSettingsForm()
        {
            _settingsPath = GetSettingsPath();
            InitializeComponent();
            LoadEndpoints();
        }

        /// <summary>
        /// Gets the settings path - always uses C:\fusionclient\ERP\settings
        /// </summary>
        private string GetSettingsPath()
        {
            string settingsPath = @"C:\fusionclient\ERP\settings";

            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(settingsPath))
                {
                    Directory.CreateDirectory(settingsPath);
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Created settings path: {settingsPath}");
                }

                // Create empty endpoints.xml if it doesn't exist
                string xmlPath = Path.Combine(settingsPath, "endpoints.xml");
                if (!File.Exists(xmlPath))
                {
                    // Create empty XML file
                    string emptyXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Endpoints>\r\n</Endpoints>";
                    File.WriteAllText(xmlPath, emptyXml);
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Created empty endpoints.xml at: {xmlPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Error: {ex.Message}");
                MessageBox.Show($"Error creating settings:\n{settingsPath}\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Using settings path: {settingsPath}");
            return settingsPath;
        }

        private void InitializeComponent()
        {
            this.Text = $"Endpoint Settings ({_settingsPath})";
            this.Size = new Size(1100, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(900, 500);
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
                Text = $"Endpoint Settings ({_settingsPath})",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 18)
            };
            headerPanel.Controls.Add(lblTitle);

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

            btnRefresh = CreateButton("Refresh", Color.FromArgb(158, 158, 158), 310);
            btnRefresh.Click += BtnRefresh_Click;

            toolbarPanel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete, btnRefresh });

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

            // Alternating row colors
            dgvEndpoints.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);

            // Selection color
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

            // Content Panel (holds DataGridView with padding)
            Panel contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 10, 15, 10),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            contentPanel.Controls.Add(dgvEndpoints);

            // Footer Panel with Save/Cancel buttons
            Panel footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White
            };

            btnSave = new Button
            {
                Text = "Save Changes",
                Width = 120,
                Height = 35,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Location = new Point(this.ClientSize.Width - 270, 12);
            btnSave.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
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
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Location = new Point(this.ClientSize.Width - 130, 12);
            btnCancel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnCancel.Click += BtnCancel_Click;

            footerPanel.Controls.AddRange(new Control[] { btnSave, btnCancel });

            // Add all panels to form
            this.Controls.Add(contentPanel);
            this.Controls.Add(toolbarPanel);
            this.Controls.Add(headerPanel);
            this.Controls.Add(footerPanel);

            // Handle form closing
            this.FormClosing += EndpointSettingsForm_FormClosing;
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
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(_settingsPath))
                {
                    Directory.CreateDirectory(_settingsPath);
                }

                string xmlPath = Path.Combine(_settingsPath, "endpoints.xml");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Loading from: {xmlPath}");

                // Clear cache to ensure fresh load
                EndpointConfigReader.ClearCache();

                // Check if file exists
                if (!File.Exists(xmlPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] File not found, creating empty list");
                    _endpoints = new List<EndpointConfig>();
                }
                else
                {
                    _endpoints = EndpointConfigReader.LoadEndpoints(_settingsPath);
                }

                dgvEndpoints.Rows.Clear();
                foreach (var ep in _endpoints)
                {
                    dgvEndpoints.Rows.Add(
                        ep.Sno,
                        ep.Source,
                        ep.IntegrationCode,
                        ep.InstanceName,
                        ep.BaseUrl,
                        ep.Endpoint,
                        ep.Comments
                    );
                }

                _isDirty = false;
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Loaded {_endpoints.Count} endpoints from {xmlPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading endpoints: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGrid()
        {
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] RefreshGrid START - _endpoints.Count = {_endpoints.Count}");
            dgvEndpoints.Rows.Clear();
            int rowNum = 0;
            foreach (var ep in _endpoints)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] RefreshGrid adding row {rowNum}: Sno={ep.Sno}, Source='{ep.Source}', IntegrationCode='{ep.IntegrationCode}', Instance='{ep.InstanceName}'");
                dgvEndpoints.Rows.Add(
                    ep.Sno,
                    ep.Source,
                    ep.IntegrationCode,
                    ep.InstanceName,
                    ep.BaseUrl,
                    ep.Endpoint,
                    ep.Comments
                );
                rowNum++;
            }
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] RefreshGrid END - dgvEndpoints.Rows.Count = {dgvEndpoints.Rows.Count}");
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnAdd_Click START");
            // Get next Sno
            int nextSno = _endpoints.Count > 0 ? _endpoints.Max(ep => ep.Sno) + 1 : 1;
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnAdd_Click - nextSno = {nextSno}");

            var editForm = new EndpointEditForm(new EndpointConfig { Sno = nextSno });
            var dialogResult = editForm.ShowDialog();
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnAdd_Click - Dialog returned: {dialogResult}");

            if (dialogResult == DialogResult.OK)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnAdd_Click - Adding endpoint to list:");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Sno = {editForm.Endpoint.Sno}");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Source = '{editForm.Endpoint.Source}'");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   IntegrationCode = '{editForm.Endpoint.IntegrationCode}'");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   BaseUrl = '{editForm.Endpoint.BaseUrl}'");
                _endpoints.Add(editForm.Endpoint);
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnAdd_Click - _endpoints.Count is now {_endpoints.Count}");
                RefreshGrid();
                _isDirty = true;
            }
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnAdd_Click END");
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            EditSelectedRow();
        }

        private void DgvEndpoints_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                EditSelectedRow();
            }
        }

        private void EditSelectedRow()
        {
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] EditSelectedRow START");
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] dgvEndpoints.SelectedRows.Count = {dgvEndpoints.SelectedRows.Count}");

            if (dgvEndpoints.SelectedRows.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] No row selected, showing message");
                MessageBox.Show("Please select a row to edit.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int rowIndex = dgvEndpoints.SelectedRows[0].Index;
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Selected rowIndex = {rowIndex}, _endpoints.Count = {_endpoints.Count}");

            if (rowIndex >= 0 && rowIndex < _endpoints.Count)
            {
                var endpoint = _endpoints[rowIndex];
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Opening EndpointEditForm for endpoint:");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Sno = {endpoint.Sno}");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Source = '{endpoint.Source}'");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   IntegrationCode = '{endpoint.IntegrationCode}'");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   InstanceName = '{endpoint.InstanceName}'");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   BaseUrl = '{endpoint.BaseUrl}'");

                var editForm = new EndpointEditForm(endpoint);
                var dialogResult = editForm.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Dialog returned: {dialogResult}");

                if (dialogResult == DialogResult.OK)
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] DialogResult.OK - updating _endpoints[{rowIndex}]");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] editForm.Endpoint values:");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Sno = {editForm.Endpoint.Sno}");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Source = '{editForm.Endpoint.Source}'");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   IntegrationCode = '{editForm.Endpoint.IntegrationCode}'");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   InstanceName = '{editForm.Endpoint.InstanceName}'");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   BaseUrl = '{editForm.Endpoint.BaseUrl}'");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Endpoint = '{editForm.Endpoint.Endpoint}'");
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings]   Comments = '{editForm.Endpoint.Comments}'");

                    _endpoints[rowIndex] = editForm.Endpoint;
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] _endpoints[{rowIndex}] updated, calling RefreshGrid");
                    RefreshGrid();
                    _isDirty = true;
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] _isDirty set to true");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Dialog was cancelled or closed (DialogResult = {dialogResult})");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] rowIndex {rowIndex} is out of bounds for _endpoints.Count {_endpoints.Count}");
            }
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] EditSelectedRow END");
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
                    $"Are you sure you want to delete endpoint:\n\n" +
                    $"Source: {endpoint.Source}\n" +
                    $"Integration Code: {endpoint.IntegrationCode}\n" +
                    $"Instance: {endpoint.InstanceName}",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

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
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Refreshing will discard them. Continue?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            LoadEndpoints();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnSave_Click START (Save Changes button)");
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] _endpoints.Count = {_endpoints.Count}");
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] _isDirty = {_isDirty}");

            try
            {
                string xmlPath = Path.Combine(_settingsPath, "endpoints.xml");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Calling SaveEndpointsToXml, path = {xmlPath}");
                SaveEndpointsToXml();
                _isDirty = false;
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Save completed successfully");
                MessageBox.Show($"Endpoints saved successfully!\n\nFile: {xmlPath}\nCount: {_endpoints.Count} endpoints",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Save FAILED: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error saving endpoints:\n\n{ex.Message}\n\nPath: {_settingsPath}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] BtnSave_Click END");
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void EndpointSettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        SaveEndpointsToXml();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        e.Cancel = true;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        private void SaveEndpointsToXml()
        {
            string xmlPath = Path.Combine(_settingsPath, "endpoints.xml");
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] SaveEndpointsToXml START");
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Saving to: {xmlPath}");
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Number of endpoints to save: {_endpoints.Count}");

            // Ensure directory exists
            string directory = Path.GetDirectoryName(xmlPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Created directory: {directory}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Directory exists: {directory}");
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

                int writeCount = 0;
                foreach (var ep in _endpoints)
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Writing endpoint #{writeCount}: Sno={ep.Sno}, Source='{ep.Source}', IntegrationCode='{ep.IntegrationCode}', Instance='{ep.InstanceName}', URL='{ep.BaseUrl}'");
                    writer.WriteStartElement("Endpoint");
                    writer.WriteElementString("Sno", ep.Sno.ToString());
                    writer.WriteElementString("Source", ep.Source ?? "");
                    writer.WriteElementString("IntegrationCode", ep.IntegrationCode ?? "");
                    writer.WriteElementString("InstanceName", ep.InstanceName ?? "");
                    writer.WriteElementString("URL", ep.BaseUrl ?? "");
                    writer.WriteElementString("Path", ep.Endpoint ?? "");  // Use Path instead of Endpoint to avoid XML confusion
                    writer.WriteElementString("Comments", ep.Comments ?? "");
                    writer.WriteEndElement();
                    writeCount++;
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] XML write completed, wrote {writeCount} endpoints");
            }

            // Also save CSV version
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Calling SaveEndpointsToCsv...");
            SaveEndpointsToCsv();

            // Clear cache so next load gets fresh data
            EndpointConfigReader.ClearCache();
            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Cache cleared");

            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] SaveEndpointsToXml END - Saved {_endpoints.Count} endpoints to {xmlPath}");
        }

        private void SaveEndpointsToCsv()
        {
            string csvPath = Path.Combine(_settingsPath, "endpoints.csv");

            var lines = new List<string>
            {
                "Sno,Source,IntegrationCode,InstanceName,URL,Endpoint,Comments"
            };

            foreach (var ep in _endpoints)
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

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }
    }

    /// <summary>
    /// Form for editing a single endpoint
    /// </summary>
    public class EndpointEditForm : Form
    {
        private TextBox txtSno;
        private ComboBox cboSource;
        private TextBox txtIntegrationCode;
        private ComboBox cboInstanceName;
        private TextBox txtBaseUrl;
        private TextBox txtEndpoint;
        private TextBox txtComments;
        private Button btnOk;
        private Button btnCancel;
        private string _apexEndpointUrl;
        private List<Dictionary<string, object>> _instancesData;
        private bool _isLoadingInstances = false;

        public EndpointConfig Endpoint { get; private set; }

        public EndpointEditForm(EndpointConfig endpoint)
        {
            // Preserve exact values from the grid - do NOT set defaults for InstanceName
            Endpoint = new EndpointConfig
            {
                Sno = endpoint.Sno,
                Source = endpoint.Source ?? "APEX",
                IntegrationCode = endpoint.IntegrationCode ?? "",
                InstanceName = endpoint.InstanceName ?? "",  // Show exactly as in data grid, no default
                BaseUrl = endpoint.BaseUrl ?? "",
                Endpoint = endpoint.Endpoint ?? "",
                Comments = endpoint.Comments ?? ""
            };

            // Load APEX endpoint URL from file
            LoadApexEndpointUrl();

            InitializeComponent();
            PopulateFields();
        }

        /// <summary>
        /// Loads the APEX endpoint URL from apexendpointurl.txt file
        /// </summary>
        private void LoadApexEndpointUrl()
        {
            try
            {
                string settingsPath = EndpointConfigReader.GetSettingsPath();
                string apexUrlFilePath = Path.Combine(settingsPath, "apexendpointurl.txt");

                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Loading APEX URL from file:");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Settings path: {settingsPath}");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] File path: {apexUrlFilePath}");

                if (File.Exists(apexUrlFilePath))
                {
                    _apexEndpointUrl = File.ReadAllText(apexUrlFilePath).Trim();
                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Loaded APEX URL from file: {_apexEndpointUrl}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] WARNING: apexendpointurl.txt not found at: {apexUrlFilePath}");
                    _apexEndpointUrl = "";
                }
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ERROR loading APEX URL: {ex.Message}");
                _apexEndpointUrl = "";
            }
        }

        /// <summary>
        /// Loads instances data from the APEX API
        /// </summary>
        private async Task LoadInstancesFromApex()
        {
            if (_isLoadingInstances) return;
            _isLoadingInstances = true;

            try
            {
                string settingsPath = EndpointConfigReader.GetSettingsPath();
                string apexInstancesFilePath = Path.Combine(settingsPath, "apexinstances.txt");

                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Loading instances from APEX");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Instances URL file: {apexInstancesFilePath}");

                if (!File.Exists(apexInstancesFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] WARNING: apexinstances.txt not found");
                    _instancesData = new List<Dictionary<string, object>>();
                    return;
                }

                string apexInstancesUrl = File.ReadAllText(apexInstancesFilePath).Trim();
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Fetching from: {apexInstancesUrl}");

                if (string.IsNullOrWhiteSpace(apexInstancesUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] WARNING: apexinstances.txt is empty");
                    _instancesData = new List<Dictionary<string, object>>();
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var response = await client.GetAsync(apexInstancesUrl);

                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Response Status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Raw Response: {jsonResponse}");

                        _instancesData = ParseInstancesResponse(jsonResponse);
                        System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Parsed {_instancesData.Count} instances");

                        // Log each instance
                        foreach (var inst in _instancesData)
                        {
                            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   -> {string.Join(", ", inst.Select(kv => $"{kv.Key}={kv.Value}"))}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] HTTP Error: {response.StatusCode}");
                        _instancesData = new List<Dictionary<string, object>>();
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ERROR loading instances: {ex.Message}");
                _instancesData = new List<Dictionary<string, object>>();
            }
            finally
            {
                _isLoadingInstances = false;
            }
        }

        /// <summary>
        /// Parses the instances JSON response into a list of dictionaries
        /// </summary>
        private List<Dictionary<string, object>> ParseInstancesResponse(string jsonResponse)
        {
            var instances = new List<Dictionary<string, object>>();

            try
            {
                using (var doc = JsonDocument.Parse(jsonResponse))
                {
                    var root = doc.RootElement;

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
                        System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Unexpected JSON structure");
                        return instances;
                    }

                    foreach (var item in itemsArray.EnumerateArray())
                    {
                        var instance = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                        foreach (var prop in item.EnumerateObject())
                        {
                            string key = prop.Name;
                            object value = prop.Value.ValueKind switch
                            {
                                JsonValueKind.String => prop.Value.GetString(),
                                JsonValueKind.Number => prop.Value.TryGetInt32(out int i) ? i : prop.Value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => null,
                                _ => prop.Value.GetRawText()
                            };
                            instance[key] = value;
                        }

                        instances.Add(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Error parsing instances JSON: {ex.Message}");
            }

            return instances;
        }

        /// <summary>
        /// Gets the base URL for a given source type and instance name from the cached instances data
        /// </summary>
        private string GetBaseUrlForInstance(string sourceType, string instanceName)
        {
            if (_instancesData == null || _instancesData.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] GetBaseUrlForInstance: No instances data available");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Looking for base URL: source={sourceType}, instance={instanceName}");

            foreach (var inst in _instancesData)
            {
                // Get instance name - try multiple field names
                string instName = inst.TryGetValue("instance", out var instVal) ? instVal?.ToString() :
                                  inst.TryGetValue("instance_name", out instVal) ? instVal?.ToString() :
                                  inst.TryGetValue("name", out instVal) ? instVal?.ToString() : null;

                // Get source type - try multiple field names
                string instSource = inst.TryGetValue("source_type", out var srcVal) ? srcVal?.ToString() :
                                    inst.TryGetValue("source", out srcVal) ? srcVal?.ToString() : null;

                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   Checking: instName={instName}, instSource={instSource}");

                // Match by instance name and source type
                if (string.Equals(instName, instanceName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(instSource, sourceType, StringComparison.OrdinalIgnoreCase))
                {
                    // Get base URL - try source-specific URL first, then generic
                    string baseUrl = null;
                    if (string.Equals(sourceType, "APEX", StringComparison.OrdinalIgnoreCase))
                    {
                        baseUrl = inst.TryGetValue("apex_base_url", out var urlVal) ? urlVal?.ToString() : null;
                    }
                    else if (string.Equals(sourceType, "FUSION", StringComparison.OrdinalIgnoreCase))
                    {
                        baseUrl = inst.TryGetValue("fusion_base_url", out var urlVal) ? urlVal?.ToString() : null;
                    }

                    // Fallback to generic base_url or url field
                    if (string.IsNullOrEmpty(baseUrl))
                    {
                        baseUrl = inst.TryGetValue("base_url", out var urlVal) ? urlVal?.ToString() :
                                  inst.TryGetValue("url", out urlVal) ? urlVal?.ToString() : null;
                    }

                    if (!string.IsNullOrEmpty(baseUrl))
                    {
                        System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   FOUND base URL: {baseUrl}");
                        return baseUrl;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] GetBaseUrlForInstance: No matching instance found");
            return null;
        }

        /// <summary>
        /// Populates the Instance dropdown based on the selected source type
        /// </summary>
        private void PopulateInstanceDropdown(string sourceType, string selectedInstance = null)
        {
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] PopulateInstanceDropdown: source={sourceType}, selected={selectedInstance}");

            cboInstanceName.Items.Clear();

            if (_instancesData == null || _instancesData.Count == 0)
            {
                // Fall back to default instances if no data available
                cboInstanceName.Items.AddRange(new object[] { "PROD", "TEST", "DEV" });
                if (!string.IsNullOrEmpty(selectedInstance))
                {
                    int idx = cboInstanceName.Items.IndexOf(selectedInstance);
                    cboInstanceName.SelectedIndex = idx >= 0 ? idx : 0;
                }
                else
                {
                    cboInstanceName.SelectedIndex = 0;
                }
                return;
            }

            // Get distinct instance names for the selected source type
            var instanceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var inst in _instancesData)
            {
                // Get source type from instance
                string instSource = inst.TryGetValue("source_type", out var srcVal) ? srcVal?.ToString() :
                                    inst.TryGetValue("source", out srcVal) ? srcVal?.ToString() : null;

                // Match source type
                if (string.Equals(instSource, sourceType, StringComparison.OrdinalIgnoreCase))
                {
                    // Get instance name
                    string instName = inst.TryGetValue("instance", out var instVal) ? instVal?.ToString() :
                                      inst.TryGetValue("instance_name", out instVal) ? instVal?.ToString() :
                                      inst.TryGetValue("name", out instVal) ? instVal?.ToString() : null;

                    if (!string.IsNullOrEmpty(instName))
                    {
                        instanceNames.Add(instName);
                    }
                }
            }

            // Add found instances to dropdown
            foreach (var name in instanceNames.OrderBy(n => n))
            {
                cboInstanceName.Items.Add(name);
            }

            // If no instances found for source, add defaults
            if (cboInstanceName.Items.Count == 0)
            {
                cboInstanceName.Items.AddRange(new object[] { "PROD", "TEST", "DEV" });
            }

            // Select the appropriate instance
            if (!string.IsNullOrEmpty(selectedInstance))
            {
                int idx = cboInstanceName.Items.IndexOf(selectedInstance);
                if (idx >= 0)
                {
                    cboInstanceName.SelectedIndex = idx;
                }
                else
                {
                    cboInstanceName.SelectedIndex = 0;
                }
            }
            else if (cboInstanceName.Items.Count > 0)
            {
                cboInstanceName.SelectedIndex = 0;
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] PopulateInstanceDropdown: Added {cboInstanceName.Items.Count} instances");
        }

        /// <summary>
        /// Handles source selection change - repopulates instances and updates base URL
        /// </summary>
        private async void CboSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboSource.SelectedItem == null) return;

            string sourceType = cboSource.SelectedItem.ToString();
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Source changed to: {sourceType}");

            // Load instances if not already loaded
            if (_instancesData == null || _instancesData.Count == 0)
            {
                await LoadInstancesFromApex();
            }

            // Populate instance dropdown for selected source
            PopulateInstanceDropdown(sourceType);

            // Update base URL for new selection
            UpdateBaseUrlFromInstance();
        }

        /// <summary>
        /// Handles instance selection change - updates base URL
        /// </summary>
        private void CboInstanceName_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateBaseUrlFromInstance();
        }

        /// <summary>
        /// Updates the base URL field based on selected source and instance
        /// </summary>
        private void UpdateBaseUrlFromInstance()
        {
            if (cboSource.SelectedItem == null || cboInstanceName.SelectedItem == null) return;

            string sourceType = cboSource.SelectedItem.ToString();
            string instanceName = cboInstanceName.SelectedItem.ToString();

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] UpdateBaseUrlFromInstance: source={sourceType}, instance={instanceName}");

            string baseUrl = GetBaseUrlForInstance(sourceType, instanceName);
            if (!string.IsNullOrEmpty(baseUrl))
            {
                txtBaseUrl.Text = baseUrl;
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Base URL auto-filled: {baseUrl}");
            }
        }

        private void InitializeComponent()
        {
            this.Text = Endpoint.Sno > 0 && !string.IsNullOrEmpty(Endpoint.IntegrationCode)
                ? "Edit Endpoint"
                : "Add New Endpoint";
            this.Size = new Size(820, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int yPos = 25;
            int labelWidth = 130;
            int fieldLeft = 170;
            int fieldWidth = 600;
            int rowHeight = 50;

            // S.No
            AddLabel("S.No:", 20, yPos);
            txtSno = new TextBox
            {
                Location = new Point(fieldLeft, yPos),
                Width = 80,
                Font = new Font("Segoe UI", 10),
                ReadOnly = true,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            this.Controls.Add(txtSno);
            yPos += rowHeight;

            // Source
            AddLabel("Source:", 20, yPos);
            cboSource = new ComboBox
            {
                Location = new Point(fieldLeft, yPos),
                Width = 150,
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboSource.Items.AddRange(new object[] { "APEX", "FUSION" });
            cboSource.SelectedIndexChanged += CboSource_SelectedIndexChanged;
            this.Controls.Add(cboSource);
            yPos += rowHeight;

            // Integration Code
            AddLabel("Integration Code:", 20, yPos);
            txtIntegrationCode = new TextBox
            {
                Location = new Point(fieldLeft, yPos),
                Width = 150,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(txtIntegrationCode);

            // Add hint label
            Label lblCodeHint = new Label
            {
                Text = "(LOGIN, WMS, GL, AR, AP, INV, OM, etc.)",
                Location = new Point(fieldLeft + 160, yPos + 3),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };
            this.Controls.Add(lblCodeHint);
            yPos += rowHeight;

            // Instance Name
            AddLabel("Instance Name:", 20, yPos);
            cboInstanceName = new ComboBox
            {
                Location = new Point(fieldLeft, yPos),
                Width = 150,
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboInstanceName.Items.AddRange(new object[] { "PROD", "TEST", "DEV" });
            cboInstanceName.SelectedIndex = 0;  // Default to PROD for new endpoints
            cboInstanceName.SelectedIndexChanged += CboInstanceName_SelectedIndexChanged;
            this.Controls.Add(cboInstanceName);
            yPos += rowHeight;

            // Base URL
            AddLabel("Base URL:", 20, yPos);
            txtBaseUrl = new TextBox
            {
                Location = new Point(fieldLeft, yPos),
                Width = fieldWidth,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(txtBaseUrl);
            yPos += rowHeight;

            // Endpoint
            AddLabel("Endpoint:", 20, yPos);
            txtEndpoint = new TextBox
            {
                Location = new Point(fieldLeft, yPos),
                Width = fieldWidth,
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(txtEndpoint);
            yPos += rowHeight;

            // Comments
            AddLabel("Comments:", 20, yPos);
            txtComments = new TextBox
            {
                Location = new Point(fieldLeft, yPos),
                Width = fieldWidth,
                Height = 60,
                Font = new Font("Segoe UI", 10),
                Multiline = true
            };
            this.Controls.Add(txtComments);
            yPos += 80;

            // Buttons
            btnOk = new Button
            {
                Text = "Save",
                Width = 100,
                Height = 35,
                Location = new Point(this.ClientSize.Width - 230, yPos),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = 35,
                Location = new Point(this.ClientSize.Width - 120, yPos),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void AddLabel(string text, int x, int y)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            this.Controls.Add(label);
        }

        private async void PopulateFields()
        {
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] PopulateFields START");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint.Sno = {Endpoint.Sno}");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint.Source = '{Endpoint.Source}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint.IntegrationCode = '{Endpoint.IntegrationCode}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint.InstanceName = '{Endpoint.InstanceName}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint.BaseUrl = '{Endpoint.BaseUrl}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint.Endpoint = '{Endpoint.Endpoint}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint.Comments = '{Endpoint.Comments}'");

            // Remember if this is a new endpoint (no base URL yet)
            bool isNewEndpoint = string.IsNullOrEmpty(Endpoint.BaseUrl);
            string savedInstanceName = Endpoint.InstanceName;
            string savedBaseUrl = Endpoint.BaseUrl;

            txtSno.Text = Endpoint.Sno.ToString();
            txtIntegrationCode.Text = Endpoint.IntegrationCode ?? "";
            txtEndpoint.Text = Endpoint.Endpoint ?? "";
            txtComments.Text = Endpoint.Comments ?? "";

            // Load instances from APEX first
            await LoadInstancesFromApex();

            // Temporarily disable event handlers while populating
            cboSource.SelectedIndexChanged -= CboSource_SelectedIndexChanged;
            cboInstanceName.SelectedIndexChanged -= CboInstanceName_SelectedIndexChanged;

            try
            {
                // Set source
                cboSource.SelectedItem = Endpoint.Source ?? "APEX";

                // Populate instance dropdown based on selected source
                string sourceType = cboSource.SelectedItem?.ToString() ?? "APEX";
                PopulateInstanceDropdown(sourceType, savedInstanceName);

                // Set the base URL
                if (isNewEndpoint)
                {
                    // For new endpoints, auto-fill base URL from selected instance
                    string instanceName = cboInstanceName.SelectedItem?.ToString();
                    if (!string.IsNullOrEmpty(instanceName))
                    {
                        string baseUrl = GetBaseUrlForInstance(sourceType, instanceName);
                        if (!string.IsNullOrEmpty(baseUrl))
                        {
                            txtBaseUrl.Text = baseUrl;
                            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Auto-filled base URL for new endpoint: {baseUrl}");
                        }
                    }
                }
                else
                {
                    // For existing endpoints, keep the saved base URL
                    txtBaseUrl.Text = savedBaseUrl ?? "";
                }
            }
            finally
            {
                // Re-enable event handlers
                cboSource.SelectedIndexChanged += CboSource_SelectedIndexChanged;
                cboInstanceName.SelectedIndexChanged += CboInstanceName_SelectedIndexChanged;
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] PopulateFields END - Fields populated");
        }

        private async void BtnOk_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] BtnOk_Click START");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] APEX URL from file: {_apexEndpointUrl}");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] txtSno.Text = '{txtSno.Text}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] cboSource.SelectedItem = '{cboSource.SelectedItem}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] txtIntegrationCode.Text = '{txtIntegrationCode.Text}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] cboInstanceName.Text = '{cboInstanceName.Text}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] txtBaseUrl.Text = '{txtBaseUrl.Text}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] txtEndpoint.Text = '{txtEndpoint.Text}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] txtComments.Text = '{txtComments.Text}'");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(txtIntegrationCode.Text))
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Validation FAILED: IntegrationCode is empty");
                MessageBox.Show("Integration Code is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIntegrationCode.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Validation FAILED: BaseUrl is empty");
                MessageBox.Show("Base URL is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBaseUrl.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Validation PASSED, updating Endpoint object...");

            // Update endpoint object - preserve case as entered
            Endpoint.Sno = int.Parse(txtSno.Text);
            Endpoint.Source = cboSource.SelectedItem?.ToString() ?? "APEX";
            Endpoint.IntegrationCode = txtIntegrationCode.Text.Trim();
            Endpoint.InstanceName = cboInstanceName.SelectedItem?.ToString() ?? "PROD";
            Endpoint.BaseUrl = txtBaseUrl.Text.Trim();
            Endpoint.Endpoint = txtEndpoint.Text.Trim();
            Endpoint.Comments = txtComments.Text.Trim();

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Endpoint object updated:");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   Sno = {Endpoint.Sno}");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   Source = '{Endpoint.Source}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   IntegrationCode = '{Endpoint.IntegrationCode}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   InstanceName = '{Endpoint.InstanceName}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   BaseUrl = '{Endpoint.BaseUrl}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   Endpoint = '{Endpoint.Endpoint}'");
            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm]   Comments = '{Endpoint.Comments}'");

            // POST to APEX endpoint if URL is available
            if (!string.IsNullOrWhiteSpace(_apexEndpointUrl))
            {
                await PostToApexEndpoint();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] WARNING: No APEX URL available, skipping POST");
            }

            System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] BtnOk_Click END - DialogResult will be OK");
        }

        /// <summary>
        /// Posts the endpoint data to the APEX /save endpoint
        /// </summary>
        private async Task PostToApexEndpoint()
        {
            try
            {
                // Build the save URL by appending /save to the base APEX URL
                string saveUrl = _apexEndpointUrl.TrimEnd('/') + "/save";

                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] POSTing to APEX endpoint");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] FULL URL: {saveUrl}");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");

                // Build the JSON payload
                var payload = new
                {
                    sno = Endpoint.Sno,
                    source = Endpoint.Source,
                    integration_code = Endpoint.IntegrationCode,
                    instance_name = Endpoint.InstanceName,
                    base_url = Endpoint.BaseUrl,
                    endpoint = Endpoint.Endpoint,
                    comments = Endpoint.Comments
                };

                // Serialize with indentation for readability in debug
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                string jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] FULL JSON PAYLOAD:");
                System.Diagnostics.Debug.WriteLine(jsonPayload);
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");

                // Also serialize without indentation for the actual POST
                string jsonPayloadCompact = JsonSerializer.Serialize(payload);

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    var content = new StringContent(jsonPayloadCompact, Encoding.UTF8, "application/json");

                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Sending POST request...");
                    var response = await client.PostAsync(saveUrl, content);

                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Response Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Response Success: {response.IsSuccessStatusCode}");

                    string responseBody = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Response Body: {responseBody}");

                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] POST to APEX SUCCESS");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] POST to APEX FAILED: HTTP {response.StatusCode}");
                        MessageBox.Show($"Warning: Failed to save to APEX endpoint.\n\nHTTP {response.StatusCode}\n{responseBody}",
                            "APEX Save Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] ========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] POST to APEX EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[EndpointEditForm] Stack: {ex.StackTrace}");
                MessageBox.Show($"Warning: Error saving to APEX endpoint.\n\n{ex.Message}",
                    "APEX Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
