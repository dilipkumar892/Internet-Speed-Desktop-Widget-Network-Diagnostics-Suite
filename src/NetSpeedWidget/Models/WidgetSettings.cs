namespace NetSpeedWidget.Models;

public class WidgetSettings
{
    public string Theme { get; set; } = "Dark";
    public double Scale { get; set; } = 1.0;
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public string SelectedAdapterId { get; set; } = "auto";
    public string PingHost { get; set; } = "1.1.1.1";
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public bool SnapToEdges { get; set; } = true;
    public bool ShowPing { get; set; } = true;
    public bool ShowWifiInfo { get; set; } = true;
    public bool ShowMiniGraph { get; set; } = true;
    public string SpeedUnit { get; set; } = "Auto"; // "Auto", "Mbps", "MB/s", "Kbps"
    public double WindowOpacity { get; set; } = 0.95;

    // Advanced features
    public int RefreshIntervalMs { get; set; } = 1000; // 500, 1000, 2000
    public bool IsMiniBarMode { get; set; } = false;
    public bool ClickThroughMode { get; set; } = false;
    public bool ShowDataUsage { get; set; } = true;
    public bool EnableHighPingAlert { get; set; } = false;
    public int PingAlertThresholdMs { get; set; } = 150;
    public bool EnableDisconnectAlert { get; set; } = true;
    public long DailyDataLimitMB { get; set; } = 0; // 0 = disabled
    public string QuotaAction { get; set; } = "Alert"; // "Alert", "Mute Audio", "Lock PC"
    public string SpeedTestServerEndpoint { get; set; } = "auto";
    public long TodayDownloadBytes { get; set; } = 0;
    public long TodayUploadBytes { get; set; } = 0;
    public string LastRecordedDate { get; set; } = "";
}
