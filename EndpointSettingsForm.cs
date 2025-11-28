using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private static readonly string DefaultSettingsPath = @"C:\fusionclient\ERP\settings";

        public EndpointSettingsForm()
        {
            InitializeComponent();
            LoadEndpoints();
        }

        private void InitializeComponent()
        {
            this.Text = "Endpoint Settings - FusionClientERP";
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
                Text = "Endpoint Configuration",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
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

            // Status label
            Label lblStatus = new Label
            {
                Text = $"Settings Path: {DefaultSettingsPath}",
                AutoSize = true,
                Location = new Point(15, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9)
            };

            footerPanel.Controls.AddRange(new Control[] { lblStatus, btnSave, btnCancel });

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
                // Clear cache to ensure fresh load
                EndpointConfigReader.ClearCache();
                _endpoints = EndpointConfigReader.LoadEndpoints(DefaultSettingsPath);

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
                System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Loaded {_endpoints.Count} endpoints");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading endpoints: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshGrid()
        {
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
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // Get next Sno
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
            {
                EditSelectedRow();
            }
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
                var endpoint = _endpoints[rowIndex];
                var editForm = new EndpointEditForm(endpoint);
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
            try
            {
                SaveEndpointsToXml();
                _isDirty = false;
                MessageBox.Show("Endpoints saved successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving endpoints: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            string xmlPath = Path.Combine(DefaultSettingsPath, "endpoints.xml");

            // Ensure directory exists
            string directory = Path.GetDirectoryName(xmlPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  "
            };

            using (var writer = XmlWriter.Create(xmlPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Endpoints");

                foreach (var ep in _endpoints)
                {
                    writer.WriteStartElement("Endpoint");
                    writer.WriteElementString("Sno", ep.Sno.ToString());
                    writer.WriteElementString("Source", ep.Source ?? "");
                    writer.WriteElementString("IntegrationCode", ep.IntegrationCode ?? "");
                    writer.WriteElementString("InstanceName", ep.InstanceName ?? "");
                    writer.WriteElementString("URL", ep.BaseUrl ?? "");
                    writer.WriteElementString("Endpoint", ep.Endpoint ?? "");
                    writer.WriteElementString("Comments", ep.Comments ?? "");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            // Also save CSV version
            SaveEndpointsToCsv();

            // Clear cache so next load gets fresh data
            EndpointConfigReader.ClearCache();

            System.Diagnostics.Debug.WriteLine($"[EndpointSettings] Saved {_endpoints.Count} endpoints to {xmlPath}");
        }

        private void SaveEndpointsToCsv()
        {
            string csvPath = Path.Combine(DefaultSettingsPath, "endpoints.csv");

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

        public EndpointConfig Endpoint { get; private set; }

        public EndpointEditForm(EndpointConfig endpoint)
        {
            Endpoint = new EndpointConfig
            {
                Sno = endpoint.Sno,
                Source = endpoint.Source ?? "APEX",
                IntegrationCode = endpoint.IntegrationCode ?? "",
                InstanceName = endpoint.InstanceName ?? "PROD",
                BaseUrl = endpoint.BaseUrl ?? "",
                Endpoint = endpoint.Endpoint ?? "",
                Comments = endpoint.Comments ?? ""
            };

            InitializeComponent();
            PopulateFields();
        }

        private void InitializeComponent()
        {
            this.Text = Endpoint.Sno > 0 && !string.IsNullOrEmpty(Endpoint.IntegrationCode)
                ? "Edit Endpoint"
                : "Add New Endpoint";
            this.Size = new Size(620, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int yPos = 25;
            int labelWidth = 130;
            int fieldLeft = 170;
            int fieldWidth = 400;
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
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cboInstanceName.Items.AddRange(new object[] { "PROD", "TEST", "DEV" });
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

        private void PopulateFields()
        {
            txtSno.Text = Endpoint.Sno.ToString();
            cboSource.SelectedItem = Endpoint.Source ?? "APEX";
            txtIntegrationCode.Text = Endpoint.IntegrationCode ?? "";
            cboInstanceName.Text = Endpoint.InstanceName ?? "PROD";
            txtBaseUrl.Text = Endpoint.BaseUrl ?? "";
            txtEndpoint.Text = Endpoint.Endpoint ?? "";
            txtComments.Text = Endpoint.Comments ?? "";
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(txtIntegrationCode.Text))
            {
                MessageBox.Show("Integration Code is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIntegrationCode.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                MessageBox.Show("Base URL is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBaseUrl.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            // Update endpoint object
            Endpoint.Sno = int.Parse(txtSno.Text);
            Endpoint.Source = cboSource.SelectedItem?.ToString() ?? "APEX";
            Endpoint.IntegrationCode = txtIntegrationCode.Text.Trim().ToUpper();
            Endpoint.InstanceName = cboInstanceName.Text.Trim().ToUpper();
            Endpoint.BaseUrl = txtBaseUrl.Text.Trim();
            Endpoint.Endpoint = txtEndpoint.Text.Trim();
            Endpoint.Comments = txtComments.Text.Trim();
        }
    }
}
