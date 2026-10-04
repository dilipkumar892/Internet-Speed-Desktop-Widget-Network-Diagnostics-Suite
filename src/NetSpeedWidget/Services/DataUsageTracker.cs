using System;
using NetSpeedWidget.Helpers;

namespace NetSpeedWidget.Services;

public class DataUsageTracker
{
    private readonly SettingsService _settingsService;
    private long _sessionRx;
    private long _sessionTx;
    private long _todayRx;
    private long _todayTx;
    private string _currentDateStr;
    private bool _quotaWarningFired;
    private int _saveCounter;

    public long SessionDownloadBytes => _sessionRx;
    public long SessionUploadBytes => _sessionTx;
    public long TodayDownloadBytes => _todayRx;
    public long TodayUploadBytes => _todayTx;

    public event Action<double, double>? QuotaExceeded; // (usedMB, limitMB)

    public DataUsageTracker(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentDateStr = DateTime.Now.ToString("yyyy-MM-dd");

        // Load today's usage from settings
        if (_settingsService.Settings.LastRecordedDate == _currentDateStr)
        {
            _todayRx = _settingsService.Settings.TodayDownloadBytes;
            _todayTx = _settingsService.Settings.TodayUploadBytes;
        }
        else
        {
            _todayRx = 0;
            _todayTx = 0;
            _settingsService.Settings.TodayDownloadBytes = 0;
            _settingsService.Settings.TodayUploadBytes = 0;
            _settingsService.Settings.LastRecordedDate = _currentDateStr;
            _settingsService.SaveSettings();
        }
    }

    public void AddDelta(long deltaRx, long deltaTx)
    {
        if (deltaRx < 0) deltaRx = 0;
        if (deltaTx < 0) deltaTx = 0;

        _sessionRx += deltaRx;
        _sessionTx += deltaTx;

        // Check day rollover
        string nowStr = DateTime.Now.ToString("yyyy-MM-dd");
        if (nowStr != _currentDateStr)
        {
            _currentDateStr = nowStr;
            _todayRx = 0;
            _todayTx = 0;
            _quotaWarningFired = false;
        }

        _todayRx += deltaRx;
        _todayTx += deltaTx;

        // Check daily quota
        long limitMB = _settingsService.Settings.DailyDataLimitMB;
        if (limitMB > 0 && !_quotaWarningFired)
        {
            double usedMB = (_todayRx + _todayTx) / (1024.0 * 1024.0);
            if (usedMB >= limitMB)
            {
                _quotaWarningFired = true;
                QuotaExceeded?.Invoke(usedMB, limitMB);
            }
        }

        // Persist today's stats periodically (every ~15 ticks)
        _saveCounter++;
        if (_saveCounter >= 15)
        {
            _saveCounter = 0;
            SaveStats();
        }
    }

    public void ResetSession()
    {
        _sessionRx = 0;
        _sessionTx = 0;
    }

    public void ResetToday()
    {
        _todayRx = 0;
        _todayTx = 0;
        _quotaWarningFired = false;
        SaveStats();
    }

    public void SaveStats()
    {
        _settingsService.Settings.TodayDownloadBytes = _todayRx;
        _settingsService.Settings.TodayUploadBytes = _todayTx;
        _settingsService.Settings.LastRecordedDate = _currentDateStr;
        _settingsService.SaveSettings();
    }

    public static string FormatDataSize(long bytes)
    {
        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }
        if (bytes < 1024L * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    public string GetSessionDisplay()
    {
        return $"↓ {FormatDataSize(_sessionRx)}  ↑ {FormatDataSize(_sessionTx)}";
    }

    public string GetTodayDisplay()
    {
        return $"Today: ↓ {FormatDataSize(_todayRx)}  ↑ {FormatDataSize(_todayTx)}";
    }

    public double GetDailyQuotaPercentage()
    {
        long limitMB = _settingsService.Settings.DailyDataLimitMB;
        if (limitMB <= 0) return 0.0;
        double usedMB = (_todayRx + _todayTx) / (1024.0 * 1024.0);
        return Math.Clamp((usedMB / limitMB) * 100.0, 0.0, 100.0);
    }
}
