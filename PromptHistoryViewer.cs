using System;
using System.Drawing;
using System.Windows.Forms;

namespace WMSApp
{
    /// <summary>
    /// Form for viewing and managing prompt history
    /// </summary>
    public class PromptHistoryViewer : Form
    {
        private readonly PromptHistoryManager _historyManager;
        private ListBox _historyListBox;
        private TextBox _promptTextBox;
        private TextBox _responseTextBox;
        private Button _clearButton;
        private Button _closeButton;

        public PromptHistoryViewer(PromptHistoryManager historyManager)
        {
            _historyManager = historyManager;
            InitializeComponents();
            LoadHistory();
        }

        private void InitializeComponents()
        {
            this.Text = "Prompt History";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // Split container for list and details
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 250
            };

            // Left panel - history list
            var listPanel = new Panel { Dock = DockStyle.Fill };

            var listLabel = new Label
            {
                Text = "History:",
                Dock = DockStyle.Top,
                Height = 25,
                Padding = new Padding(5)
            };

            _historyListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };
            _historyListBox.SelectedIndexChanged += HistoryListBox_SelectedIndexChanged;

            listPanel.Controls.Add(_historyListBox);
            listPanel.Controls.Add(listLabel);

            // Right panel - details
            var detailsPanel = new Panel { Dock = DockStyle.Fill };

            var promptLabel = new Label
            {
                Text = "Prompt:",
                Dock = DockStyle.Top,
                Height = 25,
                Padding = new Padding(5)
            };

            _promptTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 100,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9)
            };

            var responseLabel = new Label
            {
                Text = "Response:",
                Dock = DockStyle.Top,
                Height = 25,
                Padding = new Padding(5)
            };

            _responseTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9)
            };

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5)
            };

            _closeButton = new Button
            {
                Text = "Close",
                Width = 80
            };
            _closeButton.Click += (s, e) => this.Close();

            _clearButton = new Button
            {
                Text = "Clear History",
                Width = 100
            };
            _clearButton.Click += ClearButton_Click;

            buttonPanel.Controls.Add(_closeButton);
            buttonPanel.Controls.Add(_clearButton);

            detailsPanel.Controls.Add(_responseTextBox);
            detailsPanel.Controls.Add(responseLabel);
            detailsPanel.Controls.Add(_promptTextBox);
            detailsPanel.Controls.Add(promptLabel);

            splitContainer.Panel1.Controls.Add(listPanel);
            splitContainer.Panel2.Controls.Add(detailsPanel);

            this.Controls.Add(splitContainer);
            this.Controls.Add(buttonPanel);
        }

        private void LoadHistory()
        {
            _historyListBox.Items.Clear();
            var history = _historyManager.GetHistory(100);

            foreach (var item in history)
            {
                string displayText = $"[{item.Timestamp:MM/dd HH:mm}] {TruncateString(item.Prompt, 50)}";
                _historyListBox.Items.Add(new HistoryListItem(item, displayText));
            }
        }

        private void HistoryListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_historyListBox.SelectedItem is HistoryListItem item)
            {
                _promptTextBox.Text = item.HistoryItem.Prompt;
                _responseTextBox.Text = item.HistoryItem.Response;
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all prompt history?",
                "Confirm Clear",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _historyManager.ClearHistory();
                LoadHistory();
                _promptTextBox.Clear();
                _responseTextBox.Clear();
            }
        }

        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            text = text.Replace("\n", " ").Replace("\r", "");
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "...";
        }

        private class HistoryListItem
        {
            public PromptHistoryItem HistoryItem { get; }
            private readonly string _displayText;

            public HistoryListItem(PromptHistoryItem item, string displayText)
            {
                HistoryItem = item;
                _displayText = displayText;
            }

            public override string ToString() => _displayText;
        }
    }
}
