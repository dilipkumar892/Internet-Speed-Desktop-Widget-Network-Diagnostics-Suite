using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NetSpeedWidget.Models;

namespace NetSpeedWidget.Services;

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsService _settingsService;
    private readonly ToolStripMenuItem _alwaysOnTopMenuItem;
    private readonly ToolStripMenuItem _clickThroughMenuItem;
    private readonly ToolStripMenuItem _miniBarMenuItem;
    private readonly ToolStripMenuItem _showHideMenuItem;

    private readonly Queue<float> _downHistory = new Queue<float>();
    private readonly Queue<float> _upHistory = new Queue<float>();
    private const int MaxHistory = 16;
    private Icon? _defaultIcon;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private extern static bool DestroyIcon(IntPtr handle);

    public event Action? ToggleWidgetRequested;
    public event Action? ToggleAlwaysOnTopRequested;
    public event Action? ToggleClickThroughRequested;
    public event Action? ToggleMiniBarRequested;
    public event Action? OpenDiagnosticsRequested;
    public event Action? OpenSpeedTestRequested;
    public event Action? OpenSettingsRequested;
    public event Action? ExportHistoryRequested;
    public event Action? ExitRequested;

    public TrayService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        _notifyIcon = new NotifyIcon
        {
            Text = "NetSpeedWidget - Active",
            Visible = true
        };

        // Try load icon from resource or fallback to system icon
        try
        {
            var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/app.ico"))?.Stream;
            if (stream != null)
            {
                _defaultIcon = new Icon(stream);
            }
            else
            {
                _defaultIcon = SystemIcons.Application;
            }
        }
        catch
        {
            _defaultIcon = SystemIcons.Application;
        }

        _notifyIcon.Icon = _defaultIcon;

        var contextMenu = new ContextMenuStrip();

        _showHideMenuItem = new ToolStripMenuItem("Show / Hide Widget", null, (s, e) => ToggleWidgetRequested?.Invoke());
        _miniBarMenuItem = new ToolStripMenuItem("Mini-Bar / Full Mode", null, (s, e) => ToggleMiniBarRequested?.Invoke());

        _alwaysOnTopMenuItem = new ToolStripMenuItem("Always on Top")
        {
            Checked = _settingsService.Settings.AlwaysOnTop
        };
        _alwaysOnTopMenuItem.Click += (s, e) =>
        {
            ToggleAlwaysOnTopRequested?.Invoke();
            _alwaysOnTopMenuItem.Checked = _settingsService.Settings.AlwaysOnTop;
        };

        _clickThroughMenuItem = new ToolStripMenuItem("Ghost / Click-Through Mode")
        {
            Checked = _settingsService.Settings.ClickThroughMode
        };
        _clickThroughMenuItem.Click += (s, e) =>
        {
            ToggleClickThroughRequested?.Invoke();
            _clickThroughMenuItem.Checked = _settingsService.Settings.ClickThroughMode;
        };

        var diagItem = new ToolStripMenuItem("Network Diagnostics...", null, (s, e) => OpenDiagnosticsRequested?.Invoke());
        var speedTestItem = new ToolStripMenuItem("Run Speed Test...", null, (s, e) => OpenSpeedTestRequested?.Invoke());
        var exportItem = new ToolStripMenuItem("Export Test History...", null, (s, e) => ExportHistoryRequested?.Invoke());
        var settingsItem = new ToolStripMenuItem("Settings...", null, (s, e) => OpenSettingsRequested?.Invoke());
        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitRequested?.Invoke());

        contextMenu.Items.Add(_showHideMenuItem);
        contextMenu.Items.Add(_miniBarMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_alwaysOnTopMenuItem);
        contextMenu.Items.Add(_clickThroughMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(diagItem);
        contextMenu.Items.Add(speedTestItem);
        contextMenu.Items.Add(exportItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ToggleWidgetRequested?.Invoke();
    }

    public void UpdateTooltip(NetworkStats stats)
    {
        try
        {
            string text = $"NetSpeed: ↓ {stats.DownloadSpeedDisplay}  ↑ {stats.UploadSpeedDisplay}\n{stats.TodayDataDisplay}";
            if (text.Length >= 64)
            {
                text = $"↓{stats.DownloadSpeedDisplay} ↑{stats.UploadSpeedDisplay} | Ping:{stats.PingMs}ms";
                if (text.Length >= 64) text = text.Substring(0, 63);
            }
            _notifyIcon.Text = text;
            UpdateDynamicIcon(stats);
        }
        catch { }
    }

    private void UpdateDynamicIcon(NetworkStats stats)
    {
        try
        {
            _downHistory.Enqueue((float)stats.DownloadBytesPerSec);
            _upHistory.Enqueue((float)stats.UploadBytesPerSec);

            if (_downHistory.Count > MaxHistory) _downHistory.Dequeue();
            if (_upHistory.Count > MaxHistory) _upHistory.Dequeue();

            float maxSpeed = Math.Max(1024 * 100, Math.Max(_downHistory.Max(), _upHistory.Max())); // Min 100KB/s scale

            using var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);

            var downPoints = _downHistory.ToArray();
            var upPoints = _upHistory.ToArray();

            using var downPen = new Pen(Color.FromArgb(50, 205, 50), 1); // LimeGreen
            using var upPen = new Pen(Color.FromArgb(30, 144, 255), 1); // DodgerBlue

            for (int i = 0; i < downPoints.Length - 1; i++)
            {
                int x1 = i;
                int x2 = i + 1;
                
                int y1Down = 15 - (int)((downPoints[i] / maxSpeed) * 15);
                int y2Down = 15 - (int)((downPoints[i + 1] / maxSpeed) * 15);
                g.DrawLine(downPen, x1, y1Down, x2, y2Down);

                int y1Up = 15 - (int)((upPoints[i] / maxSpeed) * 15);
                int y2Up = 15 - (int)((upPoints[i + 1] / maxSpeed) * 15);
                g.DrawLine(upPen, x1, y1Up, x2, y2Up);
            }

            IntPtr hIcon = bmp.GetHicon();
            Icon newIcon = Icon.FromHandle(hIcon);
            
            var oldIcon = _notifyIcon.Icon;
            _notifyIcon.Icon = newIcon;

            if (oldIcon != null && oldIcon != _defaultIcon)
            {
                DestroyIcon(oldIcon.Handle);
                oldIcon.Dispose();
            }
        }
        catch
        {
            if (_defaultIcon != null) _notifyIcon.Icon = _defaultIcon;
        }
    }

    public void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        try
        {
            _notifyIcon.ShowBalloonTip(3000, title, text, icon);
        }
        catch { }
    }

    public void UpdateMenuStates(bool alwaysOnTop, bool clickThrough)
    {
        _alwaysOnTopMenuItem.Checked = alwaysOnTop;
        _clickThroughMenuItem.Checked = clickThrough;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
