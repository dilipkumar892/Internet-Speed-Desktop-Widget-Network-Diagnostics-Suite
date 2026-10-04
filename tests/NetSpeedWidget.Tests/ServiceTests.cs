using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetSpeedWidget.Helpers;
using NetSpeedWidget.Models;
using NetSpeedWidget.Services;
using Xunit;

namespace NetSpeedWidget.Tests;

public class ServiceTests
{
    [Fact]
    public void Formatter_FormatsCorrectly()
    {
        // 12.5 MB/s = 100 Mbps
        double bytesPerSec = 12.5 * 1024 * 1024;
        string resultMbps = Formatter.FormatSpeed(bytesPerSec, "Mbps");
        Assert.Contains("Mbps", resultMbps);

        string resultMBs = Formatter.FormatSpeed(bytesPerSec, "MB/s");
        Assert.Contains("MB/s", resultMBs);
        Assert.Equal("12.5 MB/s", resultMBs);

        string resultKbps = Formatter.FormatSpeed(1000, "Kbps");
        Assert.Contains("Kbps", resultKbps);
    }

    [Fact]
    public void SettingsService_SavesAndLoads()
    {
        var service = new SettingsService();
        service.Settings.Theme = "Light";
        service.Settings.Scale = 1.2;
        service.Settings.AlwaysOnTop = true;
        service.Settings.RefreshIntervalMs = 500;
        service.Settings.DailyDataLimitMB = 5000;
        service.Settings.IsMiniBarMode = true;
        service.SaveSettings();

        var reloaded = service.LoadSettings();
        Assert.Equal("Light", reloaded.Theme);
        Assert.Equal(1.2, reloaded.Scale);
        Assert.True(reloaded.AlwaysOnTop);
        Assert.Equal(500, reloaded.RefreshIntervalMs);
        Assert.Equal(5000, reloaded.DailyDataLimitMB);
        Assert.True(reloaded.IsMiniBarMode);
    }

    [Fact]
    public void HistoryService_AddsAndClears()
    {
        var history = new HistoryService();
        history.ClearHistory();
        Assert.Empty(history.History);

        var item = new SpeedTestResult
        {
            DownloadMbps = 55.4,
            UploadMbps = 18.2,
            PingMs = 15,
            JitterMs = 2.1,
            ServerName = "Test Server"
        };
        history.AddResult(item);

        Assert.Single(history.History);
        Assert.Equal(55.4, history.History[0].DownloadMbps);
    }

    [Fact]
    public void HistoryService_ExportsCsvAndJson()
    {
        var history = new HistoryService();
        history.ClearHistory();
        history.AddResult(new SpeedTestResult
        {
            DownloadMbps = 120.5,
            UploadMbps = 35.0,
            PingMs = 12,
            JitterMs = 1.5,
            ServerName = "Cloudflare Anycast"
        });

        string tempCsv = Path.Combine(Path.GetTempPath(), $"test_history_{Guid.NewGuid()}.csv");
        string tempJson = Path.Combine(Path.GetTempPath(), $"test_history_{Guid.NewGuid()}.json");

        try
        {
            bool csvOk = history.ExportToCsv(tempCsv);
            Assert.True(csvOk);
            Assert.True(File.Exists(tempCsv));
            string csvContent = File.ReadAllText(tempCsv);
            Assert.Contains("Timestamp,Download_Mbps", csvContent);
            Assert.Contains("120.50", csvContent);

            bool jsonOk = history.ExportToJson(tempJson);
            Assert.True(jsonOk);
            Assert.True(File.Exists(tempJson));
            string jsonContent = File.ReadAllText(tempJson);
            Assert.Contains("Cloudflare Anycast", jsonContent);
        }
        finally
        {
            if (File.Exists(tempCsv)) File.Delete(tempCsv);
            if (File.Exists(tempJson)) File.Delete(tempJson);
        }
    }

    [Fact]
    public void DataUsageTracker_TracksAndResets()
    {
        var settings = new SettingsService();
        settings.Settings.DailyDataLimitMB = 100; // 100 MB limit
        var tracker = new DataUsageTracker(settings);
        tracker.ResetSession();
        tracker.ResetToday();

        Assert.Equal(0, tracker.SessionDownloadBytes);
        Assert.Equal(0, tracker.SessionUploadBytes);

        bool quotaFired = false;
        tracker.QuotaExceeded += (used, limit) =>
        {
            quotaFired = true;
        };

        // Add 50 MB
        long fiftyMb = 50 * 1024 * 1024;
        tracker.AddDelta(fiftyMb, 0);

        Assert.Equal(fiftyMb, tracker.SessionDownloadBytes);
        Assert.False(quotaFired);

        // Add 60 MB (total 110 MB > 100 MB limit)
        long sixtyMb = 60 * 1024 * 1024;
        tracker.AddDelta(sixtyMb, 0);

        Assert.True(quotaFired);
        Assert.Contains("110.0 MB", tracker.GetSessionDisplay());

        // Reset session
        tracker.ResetSession();
        Assert.Equal(0, tracker.SessionDownloadBytes);
    }

    [Fact]
    public void DataUsageTracker_FormatsDataSizeCorrectly()
    {
        Assert.Equal("500.0 KB", DataUsageTracker.FormatDataSize(500 * 1024));
        Assert.Equal("25.0 MB", DataUsageTracker.FormatDataSize(25 * 1024 * 1024));
        Assert.Equal("1.50 GB", DataUsageTracker.FormatDataSize((long)(1.5 * 1024 * 1024 * 1024)));
    }

    [Fact]
    public async Task NetworkDiagnosticService_GeneratesReport()
    {
        var diagService = new NetworkDiagnosticService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));

        var report = await diagService.RunDiagnosticsAsync("auto", cts.Token);
        Assert.NotNull(report);
        Assert.NotNull(report.AdapterName);
        Assert.NotNull(report.MacAddress);

        string formatted = report.ToFormattedReport();
        Assert.Contains("NetSpeedWidget Network Diagnostic Report", formatted);
        Assert.Contains("Network Adapter", formatted);
    }

    [Fact]
    public void NetworkInfoService_ResolvesAdapters()
    {
        var adapters = NetworkMonitor.GetAvailableAdapters();
        Assert.NotEmpty(adapters);
        Assert.Contains(adapters, a => a.Id == "auto");

        var (connType, ifName) = NetworkInfoService.GetActiveConnectionInfo();
        Assert.NotNull(connType);
        Assert.NotNull(ifName);
    }

    [Fact]
    public async Task SpeedTestService_ConnectivityAndPing()
    {
        var service = new SpeedTestService();
        bool receivedProgress = false;

        var task = service.RunSpeedTestAsync(prog =>
        {
            receivedProgress = true;
        });

        // Allow ping phase to run briefly
        await Task.Delay(1200);
        service.Cancel();

        await task;
        Assert.True(receivedProgress);
    }
}
