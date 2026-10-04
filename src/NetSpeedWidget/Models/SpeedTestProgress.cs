namespace NetSpeedWidget.Models;

public enum SpeedTestPhase
{
    Idle,
    MeasuringPing,
    MeasuringDownload,
    MeasuringUpload,
    Completed,
    Failed,
    Cancelled
}

public class SpeedTestProgress
{
    public SpeedTestPhase Phase { get; set; } = SpeedTestPhase.Idle;
    public double ProgressPercentage { get; set; } // 0 to 100
    public double CurrentSpeedMbps { get; set; }
    public double PingMs { get; set; }
    public double JitterMs { get; set; }
    public double FinalDownloadMbps { get; set; }
    public double FinalUploadMbps { get; set; }
    public string Message { get; set; } = "Ready";
    public string ServerLocation { get; set; } = "Edge CDN";
}
