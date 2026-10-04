using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NetSpeedWidget.Models;

namespace NetSpeedWidget.Services;

public class HistoryService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NetSpeedWidget");

    private static readonly string HistoryFile = Path.Combine(AppDataFolder, "history.json");

    private const int MaxHistoryItems = 50;

    public ObservableCollection<SpeedTestResult> History { get; } = new();

    public HistoryService()
    {
        LoadHistory();
    }

    public void AddResult(SpeedTestResult result)
    {
        History.Insert(0, result);
        while (History.Count > MaxHistoryItems)
        {
            History.RemoveAt(History.Count - 1);
        }
        SaveHistory();
    }

    public void ClearHistory()
    {
        History.Clear();
        SaveHistory();
    }

    public bool ExportToCsv(string filePath)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Download_Mbps,Upload_Mbps,Ping_ms,Jitter_ms,Server_Name");
            foreach (var item in History)
            {
                sb.AppendLine($"{item.Timestamp:yyyy-MM-dd HH:mm:ss},{item.DownloadMbps:F2},{item.UploadMbps:F2},{item.PingMs:F1},{item.JitterMs:F1},\"{item.ServerName}\"");
            }
            File.WriteAllText(filePath, sb.ToString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ExportToJson(string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(History.ToList(), options);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryFile))
            {
                string json = File.ReadAllText(HistoryFile);
                var items = JsonSerializer.Deserialize<List<SpeedTestResult>>(json);
                if (items != null)
                {
                    History.Clear();
                    foreach (var item in items)
                    {
                        History.Add(item);
                    }
                }
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    private void SaveHistory()
    {
        try
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(History.ToList(), options);
            File.WriteAllText(HistoryFile, json);
        }
        catch
        {
            // Ignore write errors
        }
    }
}
