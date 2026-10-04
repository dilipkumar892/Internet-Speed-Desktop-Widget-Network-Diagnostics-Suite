using System;
using System.IO;
using System.Text.Json;
using NetSpeedWidget.Models;

namespace NetSpeedWidget.Services;

public class SettingsService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NetSpeedWidget");

    private static readonly string SettingsFile = Path.Combine(AppDataFolder, "settings.json");

    public WidgetSettings Settings { get; private set; }

    public event Action? SettingsChanged;

    public SettingsService()
    {
        Settings = LoadSettings();
    }

    public WidgetSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                var loaded = JsonSerializer.Deserialize<WidgetSettings>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Ignore error and fallback to default
        }

        return new WidgetSettings();
    }

    public void SaveSettings()
    {
        try
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Settings, options);
            File.WriteAllText(SettingsFile, json);

            SettingsChanged?.Invoke();
        }
        catch
        {
            // Logging or non-fatal fallback
        }
    }
}
