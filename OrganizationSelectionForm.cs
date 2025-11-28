using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WMSApp
{
    /// <summary>
    /// Form for selecting an Inventory Organization
    /// </summary>
    public class OrganizationSelectionForm : Form
    {
        private ListBox lstOrganizations;
        private Button btnSelect;
        private Button btnCancel;
        private Label lblTitle;
        private Label lblInfo;

        public InventoryOrganization SelectedOrganization { get; private set; }

        public OrganizationSelectionForm(List<InventoryOrganization> organizations, InventoryOrganization currentSelection = null)
        {
            InitializeComponent();
            LoadOrganizations(organizations, currentSelection);
        }

        private void InitializeComponent()
        {
            this.Text = "Select Organization";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            // Header Panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(102, 126, 234)
            };

            lblTitle = new Label
            {
                Text = "Select Inventory Organization",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 15)
            };
            headerPanel.Controls.Add(lblTitle);

            lblInfo = new Label
            {
                Text = "Choose the organization you want to work with",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(220, 220, 255),
                AutoSize = true,
                Location = new Point(20, 45)
            };
            headerPanel.Controls.Add(lblInfo);

            // Organizations ListBox
            lstOrganizations = new ListBox
            {
                Location = new Point(20, 90),
                Size = new Size(395, 200),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                ItemHeight = 30
            };
            lstOrganizations.DoubleClick += LstOrganizations_DoubleClick;
            lstOrganizations.DrawMode = DrawMode.OwnerDrawFixed;
            lstOrganizations.DrawItem += LstOrganizations_DrawItem;

            // Buttons Panel
            Panel buttonPanel = new Panel
            {
                Location = new Point(20, 305),
                Size = new Size(395, 45),
                BackColor = Color.Transparent
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = 38,
                Location = new Point(170, 0),
                BackColor = Color.FromArgb(180, 180, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += BtnCancel_Click;

            btnSelect = new Button
            {
                Text = "Select",
                Width = 100,
                Height = 38,
                Location = new Point(280, 0),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.Click += BtnSelect_Click;

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnSelect);

            // Add controls
            this.Controls.Add(headerPanel);
            this.Controls.Add(lstOrganizations);
            this.Controls.Add(buttonPanel);

            this.AcceptButton = btnSelect;
            this.CancelButton = btnCancel;
        }

        private void LstOrganizations_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            var org = lstOrganizations.Items[e.Index] as InventoryOrganization;
            if (org != null)
            {
                // Draw selection highlight
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(200, 220, 255)), e.Bounds);
                }

                // Draw organization name
                using (var font = new Font("Segoe UI", 10, FontStyle.Regular))
                using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                {
                    string displayText = $"{org.OrganizationName}";
                    e.Graphics.DrawString(displayText, font, brush, e.Bounds.X + 10, e.Bounds.Y + 5);
                }

                // Draw organization code
                using (var smallFont = new Font("Segoe UI", 8))
                using (var grayBrush = new SolidBrush(Color.Gray))
                {
                    string codeText = $"({org.OrganizationCode})";
                    var codeSize = e.Graphics.MeasureString(codeText, smallFont);
                    e.Graphics.DrawString(codeText, smallFont, grayBrush,
                        e.Bounds.Right - codeSize.Width - 15, e.Bounds.Y + 8);
                }
            }

            e.DrawFocusRectangle();
        }

        private void LoadOrganizations(List<InventoryOrganization> organizations, InventoryOrganization currentSelection)
        {
            lstOrganizations.Items.Clear();

            foreach (var org in organizations)
            {
                lstOrganizations.Items.Add(org);

                // Select current organization if provided
                if (currentSelection != null && org.OrganizationId == currentSelection.OrganizationId)
                {
                    lstOrganizations.SelectedItem = org;
                }
            }

            // Select first item if nothing selected
            if (lstOrganizations.SelectedIndex < 0 && lstOrganizations.Items.Count > 0)
            {
                lstOrganizations.SelectedIndex = 0;
            }
        }

        private void BtnSelect_Click(object sender, EventArgs e)
        {
            SelectOrganization();
        }

        private void LstOrganizations_DoubleClick(object sender, EventArgs e)
        {
            SelectOrganization();
        }

        private void SelectOrganization()
        {
            if (lstOrganizations.SelectedItem is InventoryOrganization org)
            {
                SelectedOrganization = org;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select an organization.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
