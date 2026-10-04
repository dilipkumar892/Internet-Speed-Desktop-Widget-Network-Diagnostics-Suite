using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Brush = System.Windows.Media.Brush;
using NetSpeedWidget.Helpers;
using NetSpeedWidget.Models;
using NetSpeedWidget.Services;

namespace NetSpeedWidget;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly NetworkMonitor _networkMonitor;
    private readonly SpeedTestService _speedTestService;
    private readonly HistoryService _historyService;
    private readonly NetworkDiagnosticService _diagnosticService;

    private Storyboard? _downStoryboard;
    private Storyboard? _upStoryboard;
    private bool _isDownAnimating;
    private bool _isUpAnimating;
    private bool _isLoaded;
    private NetworkDiagnosticReport? _lastDiagReport;

    public MainWindow()
    {
        InitializeComponent();

        _settingsService = App.SettingsService;
        _networkMonitor = App.NetworkMonitor;
        _speedTestService = App.SpeedTestService;
        _historyService = App.HistoryService;
        _diagnosticService = App.NetworkDiagnosticService;

        _networkMonitor.StatsUpdated += OnStatsUpdated;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _downStoryboard = (Storyboard)FindResource("DownloadPulseStoryboard");
        _upStoryboard = (Storyboard)FindResource("UploadPulseStoryboard");

        // Restore position
        if (_settingsService.Settings.WindowLeft > 0 && _settingsService.Settings.WindowTop > 0)
        {
            Left = _settingsService.Settings.WindowLeft;
            Top = _settingsService.Settings.WindowTop;
        }
        else
        {
            // Position top-right near taskbar by default
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 30;
            Top = workArea.Top + 40;
        }

        Topmost = _settingsService.Settings.AlwaysOnTop;
        UpdatePinIcon();

        // Apply scale
        ApplyScale(_settingsService.Settings.Scale);

        // Apply UI toggles
        GraphContainer.Visibility = _settingsService.Settings.ShowMiniGraph ? Visibility.Visible : Visibility.Collapsed;
        DataUsageContainer.Visibility = _settingsService.Settings.ShowDataUsage ? Visibility.Visible : Visibility.Collapsed;

        // Apply glass blur
        bool isDark = _settingsService.Settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
        WindowHelper.EnableBlur(this, isDark);

        // Apply Click-Through mode if previously enabled
        if (_settingsService.Settings.ClickThroughMode)
        {
            WindowHelper.SetClickThrough(this, true);
        }

        // Apply Mini-Bar mode if previously enabled
        if (_settingsService.Settings.IsMiniBarMode)
        {
            SetMiniBarMode(true);
        }

        // Populate Adapters
        PopulateSettingsAdapters();

        // History binding
        HistoryItemsList.ItemsSource = _historyService.History;

        _isLoaded = true;
    }

    private void OnStatsUpdated(NetworkStats stats)
    {
        Dispatcher.InvokeAsync(() =>
        {
            // 1. Full view speed indicators
            TxtDownloadSpeed.Text = stats.DownloadSpeedDisplay;
            TxtUploadSpeed.Text = stats.UploadSpeedDisplay;

            // 2. Mini-bar speed indicators
            TxtMiniDown.Text = stats.DownloadSpeedDisplay;
            TxtMiniUp.Text = stats.UploadSpeedDisplay;
            TxtMiniPing.Text = stats.PingMs >= 0 ? $"⚡ {stats.PingMs}ms" : "⚡ --";

            TxtPing.Text = stats.PingMs >= 0 ? $"{stats.PingMs} ms" : "Timeout";
            if (stats.PingMs > 150)
                TxtPing.Foreground = (Brush)FindResource("WarningBrush");
            else if (stats.PingMs < 0)
                TxtPing.Foreground = (Brush)FindResource("DangerBrush");
            else
                TxtPing.Foreground = (Brush)FindResource("TextBrush");

            // Network type & Wi-Fi
            TxtNetworkType.Text = stats.ConnectionType;
            if (stats.ConnectionType == "Wi-Fi")
            {
                TxtWifiIcon.Text = "📶";
                TxtWifiSignal.Text = $"{stats.WifiSignalStrength}%";
                TxtWifiSignal.Visibility = Visibility.Visible;
            }
            else if (stats.ConnectionType == "Ethernet")
            {
                TxtWifiIcon.Text = "🖧";
                TxtWifiSignal.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtWifiIcon.Text = "❌";
                TxtWifiSignal.Visibility = Visibility.Collapsed;
            }

            // Connection status dots
            var statusBrush = (Brush)FindResource(stats.ConnectionStatus == "Connected" ? "SuccessBrush" : "DangerBrush");
            StatusDot.Fill = statusBrush;
            MiniStatusDot.Fill = statusBrush;

            // Data usage card
            TxtSessionData.Text = stats.SessionDataDisplay;
            TxtTodayData.Text = stats.TodayDataDisplay;

            if (_settingsService.Settings.DailyDataLimitMB > 0)
            {
                DailyQuotaProgressBar.Visibility = Visibility.Visible;
                DailyQuotaProgressBar.Value = stats.DailyQuotaUsedPercent;
            }
            else
            {
                DailyQuotaProgressBar.Visibility = Visibility.Collapsed;
            }

            // Live graph
            LiveSpeedGraph.AddPoints(stats.DownloadBytesPerSec, stats.UploadBytesPerSec);

            // Animate arrows based on actual activity
            HandleArrowAnimations(stats.IsActiveDownload, stats.IsActiveUpload);
        });
    }

    private void HandleArrowAnimations(bool activeDownload, bool activeUpload)
    {
        if (activeDownload && !_isDownAnimating)
        {
            _downStoryboard?.Begin(this, true);
            _isDownAnimating = true;
        }
        else if (!activeDownload && _isDownAnimating)
        {
            _downStoryboard?.Stop(this);
            _isDownAnimating = false;
        }

        if (activeUpload && !_isUpAnimating)
        {
            _upStoryboard?.Begin(this, true);
            _isUpAnimating = true;
        }
        else if (!activeUpload && _isUpAnimating)
        {
            _upStoryboard?.Stop(this);
            _isUpAnimating = false;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
            if (_settingsService.Settings.SnapToEdges)
            {
                WindowHelper.SnapToScreenEdges(this);
            }
            SaveCurrentPosition();
        }
    }

    private void Window_LocationChanged(object sender, EventArgs e)
    {
        if (_isLoaded)
        {
            SaveCurrentPosition();
        }
    }

    private void SaveCurrentPosition()
    {
        _settingsService.Settings.WindowLeft = Left;
        _settingsService.Settings.WindowTop = Top;
        _settingsService.SaveSettings();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.AlwaysOnTop = !_settingsService.Settings.AlwaysOnTop;
        Topmost = _settingsService.Settings.AlwaysOnTop;
        UpdatePinIcon();
        _settingsService.SaveSettings();
    }

    private void UpdatePinIcon()
    {
        PinIcon.Foreground = (Brush)FindResource(Topmost ? "AccentBrush" : "TextMutedBrush");
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        App.TrayService.ShowBalloon("NetSpeedWidget Minimized", "Widget is running in system tray. Double-click tray icon to show.");
    }

    #region Mini-Bar Mode & Click-Through

    public void ToggleMiniBarMode()
    {
        SetMiniBarMode(!_settingsService.Settings.IsMiniBarMode);
    }

    private void SetMiniBarMode(bool enable)
    {
        _settingsService.Settings.IsMiniBarMode = enable;
        _settingsService.SaveSettings();

        if (enable)
        {
            CompactViewPanel.Visibility = Visibility.Collapsed;
            MiniBarPanel.Visibility = Visibility.Visible;
            DiagnosticsPanel.Visibility = Visibility.Collapsed;
            SpeedTestPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            HistoryPanel.Visibility = Visibility.Collapsed;
            SizeToContent = SizeToContent.WidthAndHeight;
        }
        else
        {
            MiniBarPanel.Visibility = Visibility.Collapsed;
            CompactViewPanel.Visibility = Visibility.Visible;
            SizeToContent = SizeToContent.Height;
            Width = 270;
        }
    }

    private void ToggleMiniBarButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMiniBarMode();
    }

    public void ToggleClickThrough()
    {
        bool enable = !_settingsService.Settings.ClickThroughMode;
        _settingsService.Settings.ClickThroughMode = enable;
        _settingsService.SaveSettings();

        WindowHelper.SetClickThrough(this, enable);
        App.TrayService.UpdateMenuStates(Topmost, enable);

        if (enable)
        {
            App.TrayService.ShowBalloon(
                "Click-Through Mode Enabled",
                "Clicks now pass through to underlying windows. To disable, right-click the System Tray icon.");
        }
        else
        {
            App.TrayService.ShowBalloon("Click-Through Disabled", "Widget is now interactive.");
        }
    }

    #endregion

    #region Panel Navigation

    public void OpenSpeedTestPanel()
    {
        SetMiniBarMode(false);
        SpeedTestPanel.Visibility = Visibility.Visible;
        DiagnosticsPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        HistoryPanel.Visibility = Visibility.Collapsed;
    }

    public void OpenSettingsPanel()
    {
        SetMiniBarMode(false);
        // Load current values to settings controls
        ChkAlwaysOnTop.IsChecked = _settingsService.Settings.AlwaysOnTop;
        ChkStartWithWindows.IsChecked = StartupService.IsStartupEnabled();
        ChkMiniGraph.IsChecked = _settingsService.Settings.ShowMiniGraph;
        ChkDataUsage.IsChecked = _settingsService.Settings.ShowDataUsage;
        ChkSnapEdges.IsChecked = _settingsService.Settings.SnapToEdges;
        ChkDisconnectAlert.IsChecked = _settingsService.Settings.EnableDisconnectAlert;
        ChkHighPingAlert.IsChecked = _settingsService.Settings.EnableHighPingAlert;
        ChkClickThrough.IsChecked = _settingsService.Settings.ClickThroughMode;
        TxtPingHost.Text = _settingsService.Settings.PingHost;
        TxtDailyQuota.Text = _settingsService.Settings.DailyDataLimitMB.ToString();

        // Quota Action
        string action = _settingsService.Settings.QuotaAction;
        CmbQuotaAction.SelectedIndex = action == "Lock PC" ? 1 : 0;

        // Refresh rate selection
        int interval = _settingsService.Settings.RefreshIntervalMs;
        CmbRefreshRate.SelectedIndex = interval switch
        {
            500 => 0,
            2000 => 2,
            _ => 1
        };

        SettingsPanel.Visibility = Visibility.Visible;
        DiagnosticsPanel.Visibility = Visibility.Collapsed;
        SpeedTestPanel.Visibility = Visibility.Collapsed;
        HistoryPanel.Visibility = Visibility.Collapsed;
    }

    public async void OpenDiagnosticsPanel()
    {
        SetMiniBarMode(false);
        DiagnosticsPanel.Visibility = Visibility.Visible;
        SpeedTestPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        HistoryPanel.Visibility = Visibility.Collapsed;

        await RunDiagnosticsAsync();
    }

    private async Task RunDiagnosticsAsync()
    {
        TxtDiagAdapter.Text = "Scanning active adapters...";
        TxtDiagLinkSpeed.Text = "Measuring link speed...";
        TxtDiagMac.Text = "Reading physical address...";
        TxtDiagLocalIp.Text = "Local IP: Reading...";
        TxtDiagGateway.Text = "Gateway: Resolving...";
        TxtDiagDns.Text = "DNS: Resolving...";
        TxtDiagPublicIp.Text = "Public IP: Querying...";
        TxtDiagDnsBenchmark.Text = "Cloudflare: Pinging... | Google: Pinging...";

        var report = await _diagnosticService.RunDiagnosticsAsync(_settingsService.Settings.SelectedAdapterId);
        _lastDiagReport = report;

        TxtDiagAdapter.Text = $"{report.AdapterName} ({report.InterfaceType})";
        TxtDiagLinkSpeed.Text = $"Speed: {report.LinkSpeed} | Status: {report.Status}";
        TxtDiagMac.Text = $"MAC: {report.MacAddress}";

        TxtDiagLocalIp.Text = $"IPv4: {report.LocalIpv4}  (Mask: {report.SubnetMask})";
        TxtDiagGateway.Text = $"Gateway: {report.DefaultGateway}";
        TxtDiagDns.Text = $"DNS: {string.Join(", ", report.DnsServers)}";
        TxtDiagPublicIp.Text = $"Public IP: {report.PublicIp ?? "Not Available"} ({report.IspLocation ?? "Anycast"})";

        if (report.PingResults.Count > 0)
        {
            var pings = report.PingResults.Select(kv => $"{kv.Key.Split(' ')[0]}: {(kv.Value >= 0 ? kv.Value + "ms" : "Timeout")}");
            TxtDiagDnsBenchmark.Text = string.Join(" | ", pings);
        }

        if (!string.IsNullOrEmpty(report.WifiSsid))
        {
            DiagWifiCard.Visibility = Visibility.Visible;
            TxtDiagWifiDetails.Text = $"SSID: {report.WifiSsid} | Signal: {report.WifiSignalPercent}% | Ch: {report.WifiChannel ?? "Auto"} ({report.WifiRadioType ?? "802.11"})";
        }
        else
        {
            DiagWifiCard.Visibility = Visibility.Collapsed;
        }
    }

    private void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenDiagnosticsPanel();
    }

    private async void RefreshDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        await RunDiagnosticsAsync();
    }

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        if (_lastDiagReport != null)
        {
            System.Windows.Clipboard.SetText(_lastDiagReport.ToFormattedReport());
            BtnCopyDiag.Content = "✓ Copied!";
            Task.Delay(1500).ContinueWith(_ => Dispatcher.Invoke(() => BtnCopyDiag.Content = "Copy Report"));
        }
    }

    private void CloseDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        DiagnosticsPanel.Visibility = Visibility.Collapsed;
    }

    private void SpeedTestButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSpeedTestPanel();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsPanel();
    }

    private void CloseSpeedTest_Click(object sender, RoutedEventArgs e)
    {
        if (_speedTestService.IsRunning)
        {
            _speedTestService.Cancel();
        }
        SpeedTestPanel.Visibility = Visibility.Collapsed;
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPanel.Visibility = Visibility.Collapsed;
    }

    private void ViewHistory_Click(object sender, RoutedEventArgs e)
    {
        HistoryPanel.Visibility = Visibility.Visible;
    }

    private void CloseHistory_Click(object sender, RoutedEventArgs e)
    {
        HistoryPanel.Visibility = Visibility.Collapsed;
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        _historyService.ClearHistory();
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string file = Path.Combine(desktop, $"NetSpeedWidget_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        if (_historyService.ExportToCsv(file))
        {
            App.TrayService.ShowBalloon("History Exported", $"Speed test records saved to Desktop:\n{Path.GetFileName(file)}");
        }
    }

    private void ResetSessionData_Click(object sender, RoutedEventArgs e)
    {
        _networkMonitor.DataTracker.ResetSession();
        TxtSessionData.Text = "↓ 0.0 MB  ↑ 0.0 MB";
    }

    #endregion

    #region Speed Test Execution

    private async void StartSpeedTest_Click(object sender, RoutedEventArgs e)
    {
        if (_speedTestService.IsRunning)
        {
            _speedTestService.Cancel();
            BtnStartSpeedTest.Content = "Start Speed Test";
            TxtTestStatus.Text = "Speed test cancelled.";
            return;
        }

        BtnStartSpeedTest.Content = "Stop Test";
        TestProgressBar.Value = 0;
        TxtTestDownload.Text = "-- Mbps";
        TxtTestUpload.Text = "-- Mbps";
        TxtTestPing.Text = "-- ms";
        TxtTestJitter.Text = "-- ms";

        var result = await _speedTestService.RunSpeedTestAsync(prog =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                TestProgressBar.Value = prog.ProgressPercentage;
                TxtTestStatus.Text = prog.Message;
                TxtTestServer.Text = prog.ServerLocation;

                if (prog.PingMs > 0) TxtTestPing.Text = $"{prog.PingMs:F0} ms";
                if (prog.JitterMs > 0) TxtTestJitter.Text = $"{prog.JitterMs:F1} ms";

                if (prog.Phase == SpeedTestPhase.MeasuringDownload)
                {
                    TxtTestDownload.Text = $"{prog.CurrentSpeedMbps:F1} Mbps";
                }
                else if (prog.Phase == SpeedTestPhase.MeasuringUpload)
                {
                    TxtTestUpload.Text = $"{prog.CurrentSpeedMbps:F1} Mbps";
                }

                if (prog.FinalDownloadMbps > 0)
                {
                    TxtTestDownload.Text = $"{prog.FinalDownloadMbps:F1} Mbps";
                }
                if (prog.FinalUploadMbps > 0)
                {
                    TxtTestUpload.Text = $"{prog.FinalUploadMbps:F1} Mbps";
                }
            });
        });

        BtnStartSpeedTest.Content = "Start Speed Test";

        if (result != null)
        {
            _historyService.AddResult(result);
        }
    }

    #endregion

    #region Settings Event Handlers

    private void PopulateSettingsAdapters()
    {
        var adapters = NetworkMonitor.GetAvailableAdapters();
        CmbAdapters.ItemsSource = adapters.Select(a => $"{a.Name} ({a.Id})").ToList();

        string current = _settingsService.Settings.SelectedAdapterId;
        int idx = adapters.FindIndex(a => a.Id == current);
        CmbAdapters.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || CmbTheme.SelectedItem is not ComboBoxItem item) return;
        string theme = item.Content.ToString() ?? "Dark";
        _settingsService.Settings.Theme = theme;
        _settingsService.SaveSettings();

        App.ApplyTheme(theme);
        WindowHelper.EnableBlur(this, theme == "Dark");
        UpdatePinIcon();
    }

    private void CmbScale_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || CmbScale.SelectedItem is not ComboBoxItem item) return;
        string text = item.Content.ToString() ?? "";
        double scale = text.Contains("85%") ? 0.85 : text.Contains("120%") ? 1.20 : 1.0;
        ApplyScale(scale);
    }

    private void ApplyScale(double scale)
    {
        WidgetScaleTransform.ScaleX = scale;
        WidgetScaleTransform.ScaleY = scale;
        _settingsService.Settings.Scale = scale;
        _settingsService.SaveSettings();
    }

    private void CmbSpeedUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || CmbSpeedUnit.SelectedItem is not ComboBoxItem item) return;
        _settingsService.Settings.SpeedUnit = item.Content.ToString() ?? "Auto";
        _settingsService.SaveSettings();
    }

    private void CmbRefreshRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || CmbRefreshRate.SelectedItem is not ComboBoxItem item) return;
        string text = item.Content.ToString() ?? "";
        int ms = text.Contains("500") ? 500 : text.Contains("2s") ? 2000 : 1000;
        _settingsService.Settings.RefreshIntervalMs = ms;
        _settingsService.SaveSettings();
    }

    private void CmbAdapters_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded) return;
        var adapters = NetworkMonitor.GetAvailableAdapters();
        if (CmbAdapters.SelectedIndex >= 0 && CmbAdapters.SelectedIndex < adapters.Count)
        {
            _settingsService.Settings.SelectedAdapterId = adapters[CmbAdapters.SelectedIndex].Id;
            _settingsService.SaveSettings();
        }
    }

    private void TxtPingHost_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        string host = TxtPingHost.Text.Trim();
        if (!string.IsNullOrEmpty(host))
        {
            _settingsService.Settings.PingHost = host;
            _settingsService.SaveSettings();
        }
    }

    private void TxtDailyQuota_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        if (long.TryParse(TxtDailyQuota.Text.Trim(), out long limit) && limit >= 0)
        {
            _settingsService.Settings.DailyDataLimitMB = limit;
            _settingsService.SaveSettings();
        }
    }

    private void CmbQuotaAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoaded || CmbQuotaAction.SelectedItem is not ComboBoxItem item) return;
        _settingsService.Settings.QuotaAction = item.Content.ToString() ?? "Alert";
        _settingsService.SaveSettings();
    }

    private void ChkAlwaysOnTop_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        _settingsService.Settings.AlwaysOnTop = ChkAlwaysOnTop.IsChecked ?? true;
        Topmost = _settingsService.Settings.AlwaysOnTop;
        UpdatePinIcon();
        _settingsService.SaveSettings();
    }

    private void ChkStartWithWindows_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        bool enable = ChkStartWithWindows.IsChecked ?? false;
        StartupService.SetStartup(enable);
        _settingsService.Settings.StartWithWindows = enable;
        _settingsService.SaveSettings();
    }

    private void ChkMiniGraph_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        bool show = ChkMiniGraph.IsChecked ?? true;
        GraphContainer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        _settingsService.Settings.ShowMiniGraph = show;
        _settingsService.SaveSettings();
    }

    private void ChkDataUsage_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        bool show = ChkDataUsage.IsChecked ?? true;
        DataUsageContainer.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        _settingsService.Settings.ShowDataUsage = show;
        _settingsService.SaveSettings();
    }

    private void ChkSnapEdges_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        _settingsService.Settings.SnapToEdges = ChkSnapEdges.IsChecked ?? true;
        _settingsService.SaveSettings();
    }

    private void ChkDisconnectAlert_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        _settingsService.Settings.EnableDisconnectAlert = ChkDisconnectAlert.IsChecked ?? true;
        _settingsService.SaveSettings();
    }

    private void ChkHighPingAlert_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        _settingsService.Settings.EnableHighPingAlert = ChkHighPingAlert.IsChecked ?? false;
        _settingsService.SaveSettings();
    }

    private void ChkClickThrough_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded) return;
        bool enable = ChkClickThrough.IsChecked ?? false;
        _settingsService.Settings.ClickThroughMode = enable;
        _settingsService.SaveSettings();
        WindowHelper.SetClickThrough(this, enable);
        App.TrayService.UpdateMenuStates(Topmost, enable);
    }

    #endregion
}
