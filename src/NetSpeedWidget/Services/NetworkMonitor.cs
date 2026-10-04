using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NetSpeedWidget.Helpers;
using NetSpeedWidget.Models;

namespace NetSpeedWidget.Services;

public class NetworkMonitor : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly DataUsageTracker _dataTracker;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    private long _prevBytesReceived;
    private long _prevBytesSent;
    private long _prevTimestampTicks;

    // Rolling ping window for packet loss and average latency
    private readonly Queue<long?> _pingHistory = new();
    private const int PingHistoryCapacity = 10;
    private int _pingCounter = 0;

    private bool? _wasConnected;
    private DateTime _lastPingAlertTime = DateTime.MinValue;
    private string? _cachedPublicIp;
    private DateTime _lastPublicIpCheck = DateTime.MinValue;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

    public NetworkStats CurrentStats { get; private set; } = new();
    public DataUsageTracker DataTracker => _dataTracker;

    public event Action<NetworkStats>? StatsUpdated;
    public event Action<bool>? ConnectionStatusChanged;
    public event Action<long>? HighLatencyDetected;
    public event Action<double, double>? DailyQuotaExceeded;

    public NetworkMonitor(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _dataTracker = new DataUsageTracker(settingsService);
        _dataTracker.QuotaExceeded += (used, limit) => DailyQuotaExceeded?.Invoke(used, limit);
    }

    public void Start()
    {
        if (_monitorTask != null) return;

        _cts = new CancellationTokenSource();
        InitializeBaseline();
        _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _monitorTask = null;
    }

    public void Dispose()
    {
        Stop();
        _dataTracker.SaveStats();
    }

    private void InitializeBaseline()
    {
        GetTotalNetworkBytes(out _prevBytesReceived, out _prevBytesSent);
        _prevTimestampTicks = Stopwatch.GetTimestamp();
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                int delayMs = _settingsService.Settings.RefreshIntervalMs;
                if (delayMs < 250) delayMs = 1000;
                await Task.Delay(delayMs, token);

                // 1. Throughput calculation
                GetTotalNetworkBytes(out long currentBytesRecv, out long currentBytesSent);
                long currentTimestamp = Stopwatch.GetTimestamp();

                double elapsedSeconds = (double)(currentTimestamp - _prevTimestampTicks) / Stopwatch.Frequency;
                if (elapsedSeconds <= 0.001) elapsedSeconds = delayMs / 1000.0;

                long deltaRecv = Math.Max(0, currentBytesRecv - _prevBytesReceived);
                long deltaSent = Math.Max(0, currentBytesSent - _prevBytesSent);

                _prevBytesReceived = currentBytesRecv;
                _prevBytesSent = currentBytesSent;
                _prevTimestampTicks = currentTimestamp;

                double downBytesPerSec = deltaRecv / elapsedSeconds;
                double upBytesPerSec = deltaSent / elapsedSeconds;

                // Track data usage
                _dataTracker.AddDelta(deltaRecv, deltaSent);

                // 2. Ping & Packet Loss (every ~2 seconds)
                _pingCounter++;
                int pingIntervalTicks = Math.Max(1, 2000 / delayMs);
                if (_pingCounter % pingIntervalTicks == 0)
                {
                    _ = PingAsync(_settingsService.Settings.PingHost, token);
                }

                // 3. Periodic Public IP check (every 5 minutes)
                if (DateTime.UtcNow - _lastPublicIpCheck > TimeSpan.FromMinutes(5))
                {
                    _lastPublicIpCheck = DateTime.UtcNow;
                    _ = RefreshPublicIpAsync(token);
                }

                // 4. Connection & Wi-Fi Details
                var (connType, ifName) = NetworkInfoService.GetActiveConnectionInfo(_settingsService.Settings.SelectedAdapterId);
                var wifi = connType == "Wi-Fi" ? NetworkInfoService.GetWifiDetails() : new NetworkInfoService.WifiDetails(null, 0);

                // 5. Update Stats object
                var stats = new NetworkStats
                {
                    DownloadBytesPerSec = downBytesPerSec,
                    UploadBytesPerSec = upBytesPerSec,
                    DownloadSpeedDisplay = Formatter.FormatSpeed(downBytesPerSec, _settingsService.Settings.SpeedUnit),
                    UploadSpeedDisplay = Formatter.FormatSpeed(upBytesPerSec, _settingsService.Settings.SpeedUnit),
                    ConnectionType = connType,
                    ActiveInterfaceName = ifName,
                    Ssid = wifi.Ssid,
                    WifiSignalStrength = wifi.SignalQuality,
                    LocalIpAddress = NetworkInfoService.GetActiveLocalIp(),
                    PublicIpAddress = _cachedPublicIp,

                    SessionDownloadBytes = _dataTracker.SessionDownloadBytes,
                    SessionUploadBytes = _dataTracker.SessionUploadBytes,
                    TodayDownloadBytes = _dataTracker.TodayDownloadBytes,
                    TodayUploadBytes = _dataTracker.TodayUploadBytes,
                    SessionDataDisplay = _dataTracker.GetSessionDisplay(),
                    TodayDataDisplay = _dataTracker.GetTodayDisplay(),
                    DailyQuotaUsedPercent = _dataTracker.GetDailyQuotaPercentage()
                };

                lock (_pingHistory)
                {
                    var validPings = _pingHistory.Where(p => p.HasValue).Select(p => p!.Value).ToList();
                    stats.PingMs = validPings.Any() ? (long)validPings.Average() : (connType == "Disconnected" ? -1 : 0);
                    int lostCount = _pingHistory.Count(p => !p.HasValue);
                    stats.PacketLossPercent = _pingHistory.Count > 0 ? (lostCount * 100.0 / _pingHistory.Count) : 0;
                    stats.ConnectionStatus = (stats.PingMs >= 0 || validPings.Any()) ? "Connected" : "No Internet";
                }

                // Disconnect / Reconnect alert detection
                bool isConnected = stats.ConnectionStatus == "Connected";
                if (_wasConnected.HasValue && _wasConnected.Value != isConnected)
                {
                    ConnectionStatusChanged?.Invoke(isConnected);
                }
                _wasConnected = isConnected;

                // High latency alert detection
                if (stats.PingMs > _settingsService.Settings.PingAlertThresholdMs &&
                    _settingsService.Settings.EnableHighPingAlert &&
                    DateTime.UtcNow - _lastPingAlertTime > TimeSpan.FromMinutes(2))
                {
                    _lastPingAlertTime = DateTime.UtcNow;
                    HighLatencyDetected?.Invoke(stats.PingMs);
                }

                CurrentStats = stats;
                StatsUpdated?.Invoke(stats);
            }
            catch when (!token.IsCancellationRequested)
            {
                // Non-fatal exception in background polling loop
            }
        }
    }

    private async Task PingAsync(string host, CancellationToken token)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 1200);

            lock (_pingHistory)
            {
                if (reply.Status == IPStatus.Success)
                {
                    _pingHistory.Enqueue(reply.RoundtripTime);
                }
                else
                {
                    _pingHistory.Enqueue(null); // packet loss
                }

                while (_pingHistory.Count > PingHistoryCapacity)
                {
                    _pingHistory.Dequeue();
                }
            }
        }
        catch
        {
            lock (_pingHistory)
            {
                _pingHistory.Enqueue(null);
                while (_pingHistory.Count > PingHistoryCapacity)
                {
                    _pingHistory.Dequeue();
                }
            }
        }
    }

    private async Task RefreshPublicIpAsync(CancellationToken token)
    {
        try
        {
            string ip = (await HttpClient.GetStringAsync("https://api.ipify.org", token)).Trim();
            if (!string.IsNullOrEmpty(ip))
            {
                _cachedPublicIp = ip;
            }
        }
        catch { }
    }

    private void GetTotalNetworkBytes(out long received, out long sent)
    {
        received = 0;
        sent = 0;

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            string targetId = _settingsService.Settings.SelectedAdapterId;
            if (!string.IsNullOrEmpty(targetId) && targetId != "auto")
            {
                var target = interfaces.FirstOrDefault(ni => ni.Id == targetId);
                if (target != null)
                {
                    var stats = target.GetIPStatistics();
                    received = stats.BytesReceived;
                    sent = stats.BytesSent;
                    return;
                }
            }

            // Aggregate all active interfaces
            foreach (var ni in interfaces)
            {
                var stats = ni.GetIPStatistics();
                received += stats.BytesReceived;
                sent += stats.BytesSent;
            }
        }
        catch
        {
            // Ignore error
        }
    }

    public static List<(string Id, string Name, string Description)> GetAvailableAdapters()
    {
        var list = new List<(string Id, string Name, string Description)>
        {
            ("auto", "Auto (Default Gateway)", "Automatically track primary active interface")
        };

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                list.Add((ni.Id, ni.Name, $"{ni.Description} ({ni.OperationalStatus})"));
            }
        }
        catch { }

        return list;
    }
}
