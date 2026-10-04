namespace NetSpeedWidget.Models;

public class NetworkStats
{
    public double DownloadBytesPerSec { get; set; }
    public double UploadBytesPerSec { get; set; }
    
    public string DownloadSpeedDisplay { get; set; } = "0.0 Mbps";
    public string UploadSpeedDisplay { get; set; } = "0.0 Mbps";
    
    public long PingMs { get; set; } = -1;
    public double PacketLossPercent { get; set; } = 0.0;
    
    public string ConnectionType { get; set; } = "Disconnected"; // "Wi-Fi", "Ethernet", "Cellular", "Disconnected"
    public string ConnectionStatus { get; set; } = "Disconnected"; // "Connected", "No Internet", "Disconnected"
    public string? Ssid { get; set; }
    public int WifiSignalStrength { get; set; } = 0; // 0 - 100%
    public string ActiveInterfaceName { get; set; } = "None";
    public string LocalIpAddress { get; set; } = "127.0.0.1";
    public string? PublicIpAddress { get; set; }
    
    // Session & Today cumulative data usage
    public long SessionDownloadBytes { get; set; }
    public long SessionUploadBytes { get; set; }
    public long TodayDownloadBytes { get; set; }
    public long TodayUploadBytes { get; set; }
    
    public string SessionDataDisplay { get; set; } = "↓ 0 MB  ↑ 0 MB";
    public string TodayDataDisplay { get; set; } = "Today: ↓ 0 MB  ↑ 0 MB";
    public double DailyQuotaUsedPercent { get; set; } = 0.0;

    public bool IsActiveDownload => DownloadBytesPerSec > 10240; // > 10 KB/s
    public bool IsActiveUpload => UploadBytesPerSec > 10240; // > 10 KB/s
}
