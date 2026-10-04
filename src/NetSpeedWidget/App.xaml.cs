using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Runtime.InteropServices;
using NetSpeedWidget.Services;

namespace NetSpeedWidget;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    public static SettingsService SettingsService { get; private set; } = null!;
    public static NetworkMonitor NetworkMonitor { get; private set; } = null!;
    public static SpeedTestService SpeedTestService { get; private set; } = null!;
    public static HistoryService HistoryService { get; private set; } = null!;
    public static NetworkDiagnosticService NetworkDiagnosticService { get; private set; } = null!;
    public static TrayService TrayService { get; private set; } = null!;

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool LockWorkStation();

    public static MainWindow? MainWidgetWindow { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "NetSpeedWidget_SingleInstance_Mutex";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            // Already running
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Initialize Services
        SettingsService = new SettingsService();
        NetworkMonitor = new NetworkMonitor(SettingsService);
        SpeedTestService = new SpeedTestService();
        HistoryService = new HistoryService();
        NetworkDiagnosticService = new NetworkDiagnosticService();
        TrayService = new TrayService(SettingsService);

        ApplyTheme(SettingsService.Settings.Theme);

        // Start background network monitoring
        NetworkMonitor.Start();

        // Wire alert notifications
        NetworkMonitor.ConnectionStatusChanged += isConnected =>
        {
            if (SettingsService.Settings.EnableDisconnectAlert)
            {
                TrayService.ShowBalloon(
                    isConnected ? "Internet Restored" : "Internet Disconnected",
                    isConnected ? "Network connectivity is active." : "Network connection was interrupted.",
                    isConnected ? System.Windows.Forms.ToolTipIcon.Info : System.Windows.Forms.ToolTipIcon.Warning);
            }
        };

        NetworkMonitor.HighLatencyDetected += pingMs =>
        {
            TrayService.ShowBalloon(
                "High Latency Alert",
                $"Ping spiked to {pingMs} ms on {SettingsService.Settings.PingHost}.",
                System.Windows.Forms.ToolTipIcon.Warning);
        };

        NetworkMonitor.DailyQuotaExceeded += (used, limit) =>
        {
            TrayService.ShowBalloon(
                "Daily Data Quota Exceeded",
                $"You have consumed {used:F0} MB of your {limit:F0} MB daily limit.",
                System.Windows.Forms.ToolTipIcon.Warning);

            string action = SettingsService.Settings.QuotaAction;
            if (action == "Lock PC")
            {
                LockWorkStation();
            }
        };

        // Wire tray service events
        TrayService.ToggleWidgetRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWidgetWindow == null)
                {
                    MainWidgetWindow = new MainWindow();
                }

                if (MainWidgetWindow.IsVisible)
                {
                    MainWidgetWindow.Hide();
                }
                else
                {
                    MainWidgetWindow.Show();
                    MainWidgetWindow.WindowState = WindowState.Normal;
                    MainWidgetWindow.Activate();
                }
            });
        };

        TrayService.ToggleMiniBarRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWidgetWindow == null)
                {
                    MainWidgetWindow = new MainWindow();
                    MainWidgetWindow.Show();
                }
                MainWidgetWindow.ToggleMiniBarMode();
            });
        };

        TrayService.ToggleAlwaysOnTopRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                SettingsService.Settings.AlwaysOnTop = !SettingsService.Settings.AlwaysOnTop;
                SettingsService.SaveSettings();
                if (MainWidgetWindow != null)
                {
                    MainWidgetWindow.Topmost = SettingsService.Settings.AlwaysOnTop;
                }
            });
        };

        TrayService.ToggleClickThroughRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                MainWidgetWindow?.ToggleClickThrough();
            });
        };

        TrayService.OpenDiagnosticsRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWidgetWindow == null)
                {
                    MainWidgetWindow = new MainWindow();
                    MainWidgetWindow.Show();
                }
                MainWidgetWindow.OpenDiagnosticsPanel();
            });
        };

        TrayService.OpenSpeedTestRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWidgetWindow == null)
                {
                    MainWidgetWindow = new MainWindow();
                    MainWidgetWindow.Show();
                }
                MainWidgetWindow.OpenSpeedTestPanel();
            });
        };

        TrayService.ExportHistoryRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string file = Path.Combine(desktop, $"NetSpeedWidget_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                if (HistoryService.ExportToCsv(file))
                {
                    TrayService.ShowBalloon("History Exported", $"Speed test records saved to:\n{file}");
                }
            });
        };

        TrayService.OpenSettingsRequested += () =>
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWidgetWindow == null)
                {
                    MainWidgetWindow = new MainWindow();
                    MainWidgetWindow.Show();
                }
                MainWidgetWindow.OpenSettingsPanel();
            });
        };

        TrayService.ExitRequested += () =>
        {
            Dispatcher.Invoke(ExitApplication);
        };

        // Update Tray tooltip with current throughput
        NetworkMonitor.StatsUpdated += stats =>
        {
            TrayService.UpdateTooltip(stats);
        };

        bool startMinimized = e.Args.Contains("--minimized");

        MainWidgetWindow = new MainWindow();
        if (!startMinimized)
        {
            MainWidgetWindow.Show();
        }
    }

    public static void ApplyTheme(string themeName)
    {
        string themeUri = themeName.Equals("Light", StringComparison.OrdinalIgnoreCase)
            ? "Resources/LightTheme.xaml"
            : "Resources/DarkTheme.xaml";

        var dictionaries = Current.Resources.MergedDictionaries;
        var existingTheme = dictionaries.FirstOrDefault(d =>
            d.Source != null && (d.Source.OriginalString.Contains("DarkTheme") || d.Source.OriginalString.Contains("LightTheme")));

        if (existingTheme != null)
        {
            dictionaries.Remove(existingTheme);
        }

        dictionaries.Insert(0, new ResourceDictionary { Source = new Uri(themeUri, UriKind.Relative) });
    }

    public static void ExitApplication()
    {
        NetworkMonitor.Dispose();
        TrayService.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        Current.Shutdown();
    }
}
