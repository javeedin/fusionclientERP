using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WMSApp
{
    /// <summary>
    /// Manages prompt history for AI interactions
    /// </summary>
    public class PromptHistoryManager
    {
        private readonly string _historyPath;
        private readonly int _maxHistoryItems = 100;

        public PromptHistoryManager()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FusionClientERP");

            Directory.CreateDirectory(appDataPath);
            _historyPath = Path.Combine(appDataPath, "prompt-history.json");
        }

        public bool SavePrompt(string prompt, string response, string category = "General")
        {
            try
            {
                var history = LoadHistory();

                history.Add(new PromptHistoryItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Prompt = prompt,
                    Response = response,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                });

                // Keep only the most recent items
                if (history.Count > _maxHistoryItems)
                {
                    history = history.GetRange(history.Count - _maxHistoryItems, _maxHistoryItems);
                }

                SaveHistory(history);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PromptHistoryManager] Save Error: {ex.Message}");
                return false;
            }
        }

        public List<PromptHistoryItem> GetHistory(int limit = 50)
        {
            var history = LoadHistory();
            if (history.Count <= limit)
                return history;

            return history.GetRange(history.Count - limit, limit);
        }

        public List<PromptHistoryItem> SearchHistory(string searchTerm)
        {
            var history = LoadHistory();
            return history.FindAll(h =>
                h.Prompt.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                h.Response.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        public bool ClearHistory()
        {
            try
            {
                SaveHistory(new List<PromptHistoryItem>());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<PromptHistoryItem> LoadHistory()
        {
            try
            {
                if (File.Exists(_historyPath))
                {
                    string json = File.ReadAllText(_historyPath);
                    return JsonSerializer.Deserialize<List<PromptHistoryItem>>(json) ?? new List<PromptHistoryItem>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PromptHistoryManager] Load Error: {ex.Message}");
            }
            return new List<PromptHistoryItem>();
        }

        private void SaveHistory(List<PromptHistoryItem> history)
        {
            string json = JsonSerializer.Serialize(history, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_historyPath, json);
        }
    }

    public class PromptHistoryItem
    {
        public string Id { get; set; }
        public string Prompt { get; set; }
        public string Response { get; set; }
        public string Category { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
