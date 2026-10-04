using System;

namespace NetSpeedWidget.Models;

public class SpeedTestResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public double DownloadMbps { get; set; }
    public double UploadMbps { get; set; }
    public double PingMs { get; set; }
    public double JitterMs { get; set; }
    public string ServerName { get; set; } = "Default Edge Node";
    public string Status { get; set; } = "Completed";

    public string FormattedDate => Timestamp.ToString("g");
    public string DownloadDisplay => $"{DownloadMbps:F1} Mbps";
    public string UploadDisplay => $"{UploadMbps:F1} Mbps";
    public string PingDisplay => $"{PingMs:F0} ms";
    public string JitterDisplay => $"{JitterMs:F1} ms";
}
