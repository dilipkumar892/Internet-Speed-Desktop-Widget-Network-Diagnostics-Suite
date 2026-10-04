# 🌐 NetSpeedWidget for Windows 10 & 11

A lightweight, modern, glassmorphic **Internet Speed Desktop Widget & Network Diagnostics Suite** for Windows 10 & 11 (64-bit), built with **C# .NET 8 and WPF**.

NetSpeedWidget continuously measures and displays real-time network throughput directly on your desktop — **no administrator rights required**, negligible CPU footprint (< 0.2%), and zero background services.

```
┌─────────────────────────────────────┐
│ 🌐 Internet     🗕 🔍 📌 ⚙ ─       │
│                                     │
│  ↓ 12.5 Mbps        ↑ 3.8 Mbps     │
│                                     │
│  Ping 24 ms         Wi-Fi 📶 85%   │
│                                     │
│  📊 DATA USAGE                   ↺  │
│  Session: ↓ 120 MB   ↑ 15 MB       │
│  Today:   ↓ 1.45 GB  ↑ 210 MB      │
│  ▓▓▓▓▓▓▓▓▓░░░░░░░  58% of 5 GB    │
│                                     │
│  ╱╲╱╲__╱╲╱╲  (Live Speed Graph)    │
│                                     │
│         [ ⚡ SPEED TEST ]           │
└─────────────────────────────────────┘

Mini-Bar Mode (Floating Pill):
┌──────────────────────────────────────┐
│ 🌐 🟢  ↓ 12.5 M  ↑ 3.8 M  ⚡ 24ms ⛶│
└──────────────────────────────────────┘
```

---

## 📋 Table of Contents

1. [Features](#-features)
2. [System Requirements](#-system-requirements)
3. [Installation (Step-by-Step)](#-installation-step-by-step)
4. [Running the Application](#-running-the-application)
5. [User Guide](#-user-guide)
6. [Settings Reference](#-settings-reference)
7. [Project Architecture](#-project-architecture)
8. [Building from Source](#-building-from-source)
9. [Running Unit Tests](#-running-unit-tests)
10. [Packaging & Distribution](#-packaging--distribution)
11. [Uninstallation](#-uninstallation)
12. [Troubleshooting](#-troubleshooting)
13. [License](#-license)

---

## ✨ Features

### 🚀 Real-Time Network Monitoring
- **Live Throughput**: Real-time download (↓) and upload (↑) speeds calculated from Windows network adapter byte counters (`System.Net.NetworkInformation`).
- **Configurable Sampling Rate**: Choose **Fast (500ms)** for gaming/streaming, **Normal (1000ms)**, or **Battery Saver (2000ms)**.
- **Active Indicators**: Animated pulsing directional arrows when network activity exceeds 10 KB/s.
- **DrawingContext Sparkline Graph**: Anti-aliased rolling history curves for both download and upload throughput.
- **Unit Preferences**: Display speed in **Auto** (adaptive), **Mbps**, **MB/s**, or **Kbps**.

### 📊 Data Usage Tracking & Daily Quota Alerts
- **Session Data Counter**: Live accumulated download and upload bytes with 1-click reset (↺).
- **Daily Cumulative Usage**: Persistent daily tracker stored in `%LOCALAPPDATA%\NetSpeedWidget` with automatic midnight rollover.
- **Daily Quota Limit**: Set a cap (in MB). A progress bar appears and a Windows notification fires when the limit is exceeded.

### 🔍 Deep Network Diagnostics
- **Adapter & Hardware**: Active interface name, hardware description, MAC address, Link Speed (e.g. 1.0 Gbps), operational status.
- **IP Routing & DNS**: Local IPv4, Subnet Mask, IPv6, Default Gateway, primary & secondary DNS servers.
- **Public IP & ISP Location**: Resolved via Cloudflare Trace API (with Anycast PoP code) or ipify fallback.
- **Multi-DNS Benchmark**: Simultaneous ping to Cloudflare (1.1.1.1), Google (8.8.8.8), and Quad9 (9.9.9.9).
- **Wi-Fi Intelligence**: SSID, BSSID, Signal Quality %, Channel, and Radio Type (802.11ax/ac/n).
- **1-Click Copy**: Formats the entire diagnostics report as text and copies to clipboard.

### ⚡ Independent ISP Speed Test Engine
- **Cloudflare Edge CDN Benchmarks**: Multi-threaded HTTP/HTTPS throughput measurement using Cloudflare Anycast nodes.
- **Latency & RFC 3550 Jitter**: Round-trip ping and inter-packet jitter calculation.
- **Live Progress**: Real-time progress bar, instantaneous speed readings, and cancel/stop at any time.
- **History & Export**: Up to 50 historical records stored locally. Export to **CSV** or **JSON** with one click.
- **Transparently Labeled**: Results clearly marked as CDN-based estimates.

### 🪟 Modern Windows Glass UI
- **Fluent Acrylic Glass**: Win32 Composition blur-behind with subtle gradient tint and drop shadow.
- **Mini-Bar / Floating Pill Mode**: Ultra-slim horizontal bar showing `🌐 ↓ 12.5M  ↑ 3.8M  ⚡ 24ms`. Toggle via header button (🗕), pill expand button (⛶), or System Tray menu.
- **Ghost / Click-Through Mode**: Win32 `WS_EX_TRANSPARENT` mouse pass-through — the widget becomes a HUD overlay that does not intercept clicks. Perfect for gaming, coding, or presentations.
- **Dark & Light Themes**: Toggle between Fluent Dark and Light palettes with dynamic switching.
- **3 Scale Sizes**: Compact (85%), Normal (100%), Large (120%).
- **Drag & Snap**: Left-click drag to reposition; magnetic snapping to screen edges.
- **Always-on-Top Pin**: Toggle with 📌 button.

### 🔔 Smart Alerts & Notifications
- **Disconnect / Reconnect Alert**: Desktop notification when internet connection drops or is restored.
- **High Latency Spike Alert**: Optional notification when ping exceeds a configurable threshold (default > 150ms).
- **Daily Quota Exceeded**: Notification when daily data consumption crosses the set limit.

### 🖥️ System Tray & Windows Integration
- **System Tray NotifyIcon**: Minimize to tray with live speed and daily usage tooltips.
- **Rich Context Menu**: Toggle widget, Mini-Bar mode, Click-Through, Diagnostics, Speed Test, Export History, Settings, and Exit.
- **Start with Windows**: Non-admin auto-start via per-user registry key (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).
- **Single Instance**: Mutex-based single-instance enforcement. Re-launching brings the existing window to focus.
- **Settings Persistence**: All preferences stored as JSON in `%LOCALAPPDATA%\NetSpeedWidget\settings.json`.

---

## 💻 System Requirements

| Requirement | Minimum | Recommended |
| :--- | :--- | :--- |
| **Operating System** | Windows 10 64-bit (Build 1809+) | Windows 10/11 64-bit (latest) |
| **RAM** | 50 MB free | 100 MB free |
| **Disk Space** | 5 MB (framework-dependent) | 75 MB (self-contained) |
| **Runtime** | .NET 8.0 Desktop Runtime (if not self-contained) | Included in self-contained build |
| **CPU** | Any x64 processor | Any x64 processor |
| **Network** | Active network adapter | Active network adapter |
| **Privileges** | Standard User (no admin) | Standard User (no admin) |

---

## 📥 Installation (Step-by-Step)

NetSpeedWidget offers **four installation methods**. Choose the one that best fits your needs:

---

### Method 1: Portable ZIP (Recommended — Zero Install)

This is the simplest method. No installer, no admin rights, no system changes. Just extract and run.

**Step 1 — Download the portable ZIP**
```
File: NetSpeedWidget-1.0.0-win-x64-portable.zip
Location: artifacts\dist\NetSpeedWidget-1.0.0-win-x64-portable.zip
```

**Step 2 — Extract the ZIP**
1. Right-click the `.zip` file in File Explorer.
2. Select **"Extract All..."**.
3. Choose a destination folder, for example:
   - `C:\Users\<YourName>\Programs\NetSpeedWidget\`
   - or `D:\Tools\NetSpeedWidget\`
4. Click **"Extract"**.

**Step 3 — (Optional) Verify the SHA256 checksum**
```powershell
# Open PowerShell and run:
Get-FileHash "C:\path\to\NetSpeedWidget-1.0.0-win-x64-portable.zip" -Algorithm SHA256
# Compare with the hash in SHA256SUMS.txt
```

**Step 4 — Run the application**
1. Open the extracted folder.
2. Double-click **`NetSpeedWidget.exe`**.
3. The widget appears on your desktop. A system tray icon (🌐) also appears in the notification area.

> **Note (Framework-Dependent Build):** If the build was compiled *without* `-SelfContained`, Windows may prompt you to install the **.NET 8.0 Desktop Runtime**. Click the link in the dialog to download it from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0), install it, then re-launch.

> **Note (Self-Contained Build):** The self-contained build (~72 MB) embeds the entire .NET runtime. No additional downloads are needed. It works on any 64-bit Windows 10/11 machine immediately.

**Step 5 — (Optional) Start with Windows**
1. Click **⚙** (Settings) on the widget.
2. Check **"Start with Windows"**.
3. This adds `NetSpeedWidget.exe --minimized` to your user-level Windows Startup registry (`HKCU\...\Run`) — no admin required.

**Step 6 — (Optional) Pin to Desktop**
1. Right-click the widget header and drag to your preferred position.
2. Click **📌** to toggle Always-on-Top.
3. The widget will remember its position across restarts.

---

### Method 2: PowerShell Installer Bundle

This method installs to `%LOCALAPPDATA%\NetSpeedWidget`, creates Start Menu and Desktop shortcuts, and launches automatically.

**Step 1 — Download the installer bundle**
```
File: NetSpeedWidget-Setup-1.0.0.zip
Location: artifacts\dist\NetSpeedWidget-Setup-1.0.0.zip
```

**Step 2 — Extract the installer ZIP**
1. Right-click → **"Extract All..."** → choose any temporary folder.

**Step 3 — Run the installer**

*Option A — Double-click the batch file:*
1. Open the extracted folder.
2. Double-click **`Install.bat`**.
3. A PowerShell window opens showing the installation progress.

*Option B — Run the PowerShell script directly:*
```powershell
powershell -ExecutionPolicy Bypass -File "C:\path\to\extracted\Install.ps1"
```

**Step 4 — What the installer does:**
1. Terminates any running instance of NetSpeedWidget.
2. Copies all application files to `%LOCALAPPDATA%\NetSpeedWidget\`.
3. Creates a **Start Menu** shortcut under `Programs\NetSpeedWidget\`.
4. Creates a **Desktop** shortcut.
5. Launches NetSpeedWidget automatically.

**Step 5 — Verify installation**
- The widget should appear on your desktop.
- A shortcut named "NetSpeedWidget" should be on your Desktop and in the Start Menu.

---

### Method 3: Inno Setup Installer (.exe) — If Available

If the project was built on a machine with [Inno Setup 6](https://jrsoftware.org/isdl.php) installed, the packaging script produces a professional Windows installer EXE.

**Step 1 — Run the installer**
```
File: NetSpeedWidget-Setup-1.0.0.exe
```
1. Double-click `NetSpeedWidget-Setup-1.0.0.exe`.
2. Follow the setup wizard:
   - Accept the license agreement.
   - Choose install location (default: `%LOCALAPPDATA%\Programs\NetSpeedWidget\`).
   - (Optional) Check "Create a Desktop shortcut".
   - (Optional) Check "Start NetSpeedWidget automatically when Windows starts".
3. Click **Install**, then **Finish**.

**Step 2 — The installer configures:**
- Application files in user-local programs directory (no admin elevation).
- Start Menu folder with launch and uninstall shortcuts.
- (If selected) Desktop shortcut and Windows Startup registry entry.
- Full uninstaller accessible from Settings → Apps or the Start Menu.

---

### Method 4: Windows Installer (.msi)

Ideal for enterprise deployments or users who prefer the native Windows Installer engine.

**Step 1 — Run the installer**
```
File: NetSpeedWidget-Setup-1.0.0.msi
```
1. Double-click `NetSpeedWidget-Setup-1.0.0.msi`.
2. Follow the standard Windows Installer wizard.

**Step 2 — The installer configures:**
- Application files in `%LOCALAPPDATA%\Programs\NetSpeedWidget\`
- Start Menu shortcuts
- Full uninstaller registered with Windows

---

### Method 5: Windows Package Manager (Winget)

Pre-configured Winget manifests are included for future submission to the [winget-pkgs](https://github.com/microsoft/winget-pkgs) repository.

```powershell
# After the manifest is published:
winget install NetSpeedWidget.NetSpeedWidget
```

---

## 🚀 Running the Application

### Launching
```powershell
# Direct launch:
.\NetSpeedWidget.exe

# Launch minimized to system tray:
.\NetSpeedWidget.exe --minimized
```

### First Launch Behavior
1. The widget window appears at the **top-right corner** of your primary monitor.
2. A **system tray icon** (🌐) is added to the notification area.
3. Background network monitoring starts immediately.
4. Default settings: Dark theme, Normal scale (100%), 1-second refresh, Always-on-Top enabled.

### Single Instance
Only one instance of NetSpeedWidget can run at a time. If you try to launch it again while it's already running, the second instance will silently close. To access a minimized widget, **double-click the system tray icon**.

---

## 📖 User Guide

### Main Widget View

| UI Element | Description |
| :--- | :--- |
| **🌐 Internet** | Header with application title |
| **🟢 Status Dot** | Green = Connected, Red = No Internet / Disconnected |
| **🗕** | Toggle Mini-Bar / Full mode |
| **🔍** | Open Network Diagnostics panel |
| **📌** | Toggle Always-on-Top (highlighted when pinned) |
| **⚙** | Open Settings panel |
| **─** | Minimize to System Tray |
| **↓ / ↑ Speed** | Real-time download and upload speeds with animated activity arrows |
| **Ping** | Averaged ICMP round-trip latency to the configured ping host |
| **Wi-Fi / Ethernet** | Connection type with signal strength (Wi-Fi) or icon (Ethernet) |
| **📊 DATA USAGE** | Session and daily cumulative data with optional quota progress bar |
| **Speed Graph** | Rolling sparkline chart of recent throughput history |
| **⚡ SPEED TEST** | Opens the ISP Speed Test panel |

### Mini-Bar Mode
- An ultra-compact horizontal pill showing: `🌐 🟢  ↓ 12.5M  ↑ 3.8M  ⚡ 24ms  ⛶`
- Click **⛶** to expand back to full mode.
- Supports drag, screen snapping, and always-on-top.
- Toggle from: header 🗕 button, pill ⛶ button, or System Tray menu.

### Network Diagnostics Panel
Click **🔍** to open. Displays:
- **Adapter & Speed**: Name, description, link speed, MAC address, status.
- **IP Routing & DNS**: IPv4, subnet mask, gateway, DNS servers, public IP + ISP location.
- **Multi-DNS Benchmark**: Latency to Cloudflare, Google, and Quad9 DNS servers.
- **Wi-Fi Signal & Channel**: SSID, BSSID, signal %, channel, radio type (when on Wi-Fi).
- **Copy Report**: Copies the full text diagnostic report to clipboard.
- **Refresh**: Re-runs all diagnostics.

### Speed Test Panel
Click **⚡ SPEED TEST** to open. Features:
- **Server info**: Cloudflare Anycast Edge CDN node and PoP code.
- **Results grid**: Download (Mbps), Upload (Mbps), Ping Latency (ms), Jitter (ms).
- **Live progress bar** with instantaneous speed readings.
- **Start / Stop** button for cancellation.
- **📜 History** button to view past test results.
- **Export CSV** and **Clear History** buttons in the history view.

### Ghost / Click-Through Mode
When enabled, mouse clicks pass through the widget to the window underneath. The widget becomes a transparent HUD overlay.
- **Enable**: Settings → "Ghost / Click-Through Mode" checkbox, or System Tray → "Ghost / Click-Through Mode".
- **Disable**: Right-click the System Tray icon → uncheck "Ghost / Click-Through Mode" (since clicks pass through the widget itself).

### System Tray Menu
Right-click the 🌐 tray icon:

| Menu Item | Action |
| :--- | :--- |
| Show / Hide Widget | Toggle widget window visibility |
| Mini-Bar / Full Mode | Switch between Mini-Bar and full view |
| Always on Top | Toggle topmost window state |
| Ghost / Click-Through Mode | Toggle mouse pass-through |
| Network Diagnostics... | Open the diagnostics panel |
| Run Speed Test... | Open the speed test panel |
| Export Test History... | Save speed test records as CSV to Desktop |
| Settings... | Open the settings panel |
| Exit | Shut down the application completely |

---

## ⚙ Settings Reference

Open Settings by clicking **⚙** on the widget header or from the System Tray context menu.

| Setting | Options | Default | Description |
| :--- | :--- | :--- | :--- |
| **Theme** | Dark, Light | Dark | Visual theme. Changes glass tint and all UI colors. |
| **Size** | Compact (85%), Normal (100%), Large (120%) | Normal | Widget scale factor. |
| **Speed Unit** | Auto, Mbps, MB/s, Kbps | Auto | How throughput is displayed. Auto adapts to magnitude. |
| **Refresh Rate** | Fast (500ms), Normal (1s), Battery (2s) | Normal (1s) | How often the throughput reading updates. |
| **Network Adapter** | Auto (Default Gateway), or specific adapter | Auto | Which adapter's byte counters to track. |
| **Ping Host** | Any hostname or IP | 1.1.1.1 | Target host for continuous ICMP latency monitoring. |
| **Daily Quota (MB)** | 0 (disabled), or any number | 0 | Daily data limit. Shows progress bar and alert when exceeded. |
| **Always on Top** | Checkbox | ✅ On | Keep widget above all other windows. |
| **Start with Windows** | Checkbox | ❌ Off | Add `HKCU\...\Run` registry entry for auto-start on login. |
| **Show Speed Graph** | Checkbox | ✅ On | Display or hide the rolling sparkline chart. |
| **Show Data Usage Counter** | Checkbox | ✅ On | Display or hide the session/daily data usage card. |
| **Snap to Screen Edges** | Checkbox | ✅ On | Magnetic snapping when dragging near screen borders. |
| **Alert on Disconnect / Reconnect** | Checkbox | ✅ On | Desktop notification on connectivity changes. |
| **Alert on Latency Spike (>150ms)** | Checkbox | ❌ Off | Desktop notification when ping exceeds threshold. |
| **Ghost / Click-Through Mode** | Checkbox | ❌ Off | Mouse clicks pass through to windows behind. |

### Data Storage Locations

| Data | File Path |
| :--- | :--- |
| Settings | `%LOCALAPPDATA%\NetSpeedWidget\settings.json` |
| Speed Test History | `%LOCALAPPDATA%\NetSpeedWidget\history.json` |
| Daily Data Usage | Stored within `settings.json` (`TodayDownloadBytes`, `TodayUploadBytes`, `LastRecordedDate`) |

---

## 🏗️ Project Architecture

```
NetSpeedWidget/
├── src/
│   └── NetSpeedWidget/
│       ├── App.xaml                        # WPF Application definition & merged resource dictionaries
│       ├── App.xaml.cs                     # Single-instance mutex, service initialization, alert wiring, Tray orchestration
│       ├── MainWindow.xaml                 # Full widget XAML: Mini-Bar, Full view, Diagnostics, Speed Test, Settings, History panels
│       ├── MainWindow.xaml.cs              # UI event handlers for all panels, Mini-Bar toggle, Click-Through, Diagnostics
│       ├── NetSpeedWidget.csproj           # .NET 8.0 WPF project (WinExe, win-x64, single-file publish)
│       ├── Controls/
│       │   └── SpeedGraph.cs              # High-performance DrawingContext sparkline graph (anti-aliased, area-filled)
│       ├── Models/
│       │   ├── NetworkStats.cs            # Real-time throughput, data usage, IP, and connection stats
│       │   ├── SpeedTestProgress.cs       # Phase enum (Idle/Ping/Download/Upload/Completed/Failed/Cancelled) and progress state
│       │   ├── SpeedTestResult.cs         # Result schema with GUID, timestamp, speeds, ping, jitter, server
│       │   └── WidgetSettings.cs          # All user preferences: theme, scale, unit, refresh rate, quota, alerts, mini-bar, click-through
│       ├── Services/
│       │   ├── NetworkMonitor.cs          # Delta throughput calculation, ICMP ping polling, data usage orchestration, alerts
│       │   ├── DataUsageTracker.cs        # Session and daily data accumulation, midnight rollover, quota alerts, persistence
│       │   ├── NetworkDiagnosticService.cs # Deep diagnostics: adapter info, IP/gateway/DNS, public IP, multi-DNS benchmark, Wi-Fi details
│       │   ├── SpeedTestService.cs        # Multi-threaded HTTP download/upload benchmark via Cloudflare Edge, latency & jitter
│       │   ├── NetworkInfoService.cs      # Active connection type detection, Wi-Fi SSID & signal via netsh, local IP resolution
│       │   ├── SettingsService.cs         # JSON read/write to %LOCALAPPDATA%\NetSpeedWidget\settings.json
│       │   ├── HistoryService.cs          # ObservableCollection persistence, CSV and JSON export
│       │   ├── StartupService.cs          # HKCU\...\Run registry management for Start with Windows
│       │   └── TrayService.cs             # Windows NotifyIcon, tooltip, context menu, balloon notifications
│       ├── Helpers/
│       │   ├── Formatter.cs               # Speed formatting: Auto/Mbps/MB/s/Kbps with magnitude adaptation
│       │   ├── RelayCommand.cs            # MVVM ICommand implementation for WPF bindings
│       │   └── WindowHelper.cs            # Win32 composition blur-behind, WS_EX_TRANSPARENT click-through, screen edge snapping
│       └── Resources/
│           ├── app.ico                    # Multi-resolution application icon (256x256, generated by scripts/generate-icon.ps1)
│           ├── app.png                    # PNG version of the application icon
│           ├── DarkTheme.xaml             # Fluent Dark color palette (12 named colors + brushes)
│           ├── LightTheme.xaml            # Fluent Light color palette (12 named colors + brushes)
│           └── Styles.xaml                # GlassBorderStyle, CardBorderStyle, ModernButtonStyle, IconButton templates
├── tests/
│   └── NetSpeedWidget.Tests/
│       ├── NetSpeedWidget.Tests.csproj    # xUnit test project referencing the main project
│       └── ServiceTests.cs               # 9 unit tests covering Formatter, Settings, History, DataUsageTracker, Diagnostics, SpeedTest
├── installer/
│   ├── installer.iss                      # Inno Setup script: per-user install, Start Menu, Desktop shortcut, auto-start
│   └── build-installer.ps1               # Fallback: generates Install.ps1 + Install.bat + app/ bundle
├── winget/
│   └── manifests/n/NetSpeedWidget/1.0.0/
│       ├── NetSpeedWidget.NetSpeedWidget.yaml
│       ├── NetSpeedWidget.NetSpeedWidget.installer.yaml
│       └── NetSpeedWidget.NetSpeedWidget.locale.en-US.yaml
├── scripts/
│   ├── build.ps1                          # Builds Release single-file publish (supports -SelfContained switch)
│   ├── package.ps1                        # Full packaging: build + ZIP + installer + SHA256 + Winget manifest update
│   ├── run.ps1                            # Quick local development launcher (dotnet run)
│   └── generate-icon.ps1                  # Programmatic 256x256 icon generation via System.Drawing
├── artifacts/                             # (Generated) Build output and distribution packages
│   ├── publish/                           # Published single-file binaries
│   └── dist/                              # Portable ZIP, installer bundle, SHA256SUMS.txt
├── .gitignore
├── LICENSE
└── README.md
```

---

## 🔨 Building from Source

### Prerequisites

1. **Windows 10 or 11** (64-bit).
2. **.NET 8.0 SDK** — Download from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0).
3. Verify installation:
   ```powershell
   dotnet --version
   # Expected output: 8.0.xxx
   ```

### Step 1 — Clone or download the repository

```powershell
git clone <repository-url>
cd internet
```

### Step 2 — Restore dependencies

```powershell
dotnet restore src/NetSpeedWidget/NetSpeedWidget.csproj
```

### Step 3 — Build (Debug)

```powershell
dotnet build src/NetSpeedWidget/NetSpeedWidget.csproj
```

### Step 4 — Run locally

```powershell
# Option A — Using the launcher script:
powershell -ExecutionPolicy Bypass -File scripts\run.ps1

# Option B — Using dotnet CLI:
dotnet run --project src/NetSpeedWidget/NetSpeedWidget.csproj
```

### Step 5 — Build Release (Framework-Dependent, ~400 KB)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1
# Output: artifacts\publish\NetSpeedWidget.exe
```

### Step 6 — Build Release (Self-Contained, ~72 MB, no .NET required on target)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1 -SelfContained
# Output: artifacts\publish\NetSpeedWidget.exe (includes .NET runtime)
```

---

## 🧪 Running Unit Tests

```powershell
dotnet test tests/NetSpeedWidget.Tests/NetSpeedWidget.Tests.csproj
```

**Expected output:**
```
Passed!  - Failed: 0, Passed: 9, Skipped: 0, Total: 9, Duration: ~1s

Test suites:
  ✔ Formatter_FormatsCorrectly           — Validates Auto/Mbps/MB/s/Kbps formatting
  ✔ SettingsService_SavesAndLoads        — JSON persistence round-trip including new fields
  ✔ HistoryService_AddsAndClears         — ObservableCollection add/clear operations
  ✔ HistoryService_ExportsCsvAndJson     — CSV and JSON file export with content validation
  ✔ DataUsageTracker_TracksAndResets      — Session/daily accumulation, quota alert firing, reset
  ✔ DataUsageTracker_FormatsDataSizeCorrectly — KB/MB/GB formatting thresholds
  ✔ NetworkDiagnosticService_GeneratesReport  — Full diagnostic scan with formatted text report
  ✔ NetworkInfoService_ResolvesAdapters  — Adapter enumeration and active connection detection
  ✔ SpeedTestService_ConnectivityAndPing — Cloudflare CDN connectivity and progress callbacks
```

---

## 📦 Packaging & Distribution

The `package.ps1` script automates the full release pipeline:

```powershell
# Framework-dependent package:
powershell -ExecutionPolicy Bypass -File scripts\package.ps1 -Version 1.0.0

# Self-contained package (recommended for distribution):
powershell -ExecutionPolicy Bypass -File scripts\package.ps1 -Version 1.0.0 -SelfContained
```

### What `package.ps1` does:
1. **Builds** the Release single-file binary via `build.ps1`.
2. **Creates** `NetSpeedWidget-1.0.0-win-x64-portable.zip` (portable ZIP).
3. **Creates** an installer bundle:
   - If **Inno Setup 6** is installed: compiles `NetSpeedWidget-Setup-1.0.0.exe`.
   - Otherwise: generates a `NetSpeedWidget-Setup-1.0.0.zip` with `Install.bat` and `Install.ps1`.
4. **Computes** SHA256 checksums → `SHA256SUMS.txt`.
5. **Updates** the Winget manifest with the installer's SHA256 hash.

### Output artifacts in `artifacts\dist\`:
```
NetSpeedWidget-1.0.0-win-x64-portable.zip    — Portable ZIP (extract and run)
NetSpeedWidget-Setup-1.0.0.zip                — Installer bundle (with Install.bat)
  or NetSpeedWidget-Setup-1.0.0.exe           — Inno Setup installer (when available)
SHA256SUMS.txt                                — Cryptographic checksums for verification
```

---

## 🗑️ Uninstallation

### If installed via Portable ZIP
1. Close NetSpeedWidget (right-click tray icon → **Exit**).
2. If "Start with Windows" was enabled, open Settings first and uncheck it, or manually remove the registry entry:
   ```powershell
   Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "NetSpeedWidget" -ErrorAction SilentlyContinue
   ```
3. Delete the application folder.
4. (Optional) Delete stored settings and history:
   ```powershell
   Remove-Item -Recurse -Force "$env:LOCALAPPDATA\NetSpeedWidget"
   ```

### If installed via PowerShell Installer
1. Close NetSpeedWidget (right-click tray icon → **Exit**).
2. Delete the application folder and shortcuts:
   ```powershell
   # Remove app files:
   Remove-Item -Recurse -Force "$env:LOCALAPPDATA\NetSpeedWidget"

   # Remove Desktop shortcut:
   Remove-Item -Force "$env:USERPROFILE\Desktop\NetSpeedWidget.lnk" -ErrorAction SilentlyContinue

   # Remove Start Menu shortcut:
   Remove-Item -Recurse -Force "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\NetSpeedWidget" -ErrorAction SilentlyContinue

   # Remove Startup registry entry:
   Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "NetSpeedWidget" -ErrorAction SilentlyContinue
   ```

### If installed via Inno Setup Installer
1. Open **Windows Settings** → **Apps** → **Apps & features**.
2. Search for **"NetSpeedWidget"**.
3. Click **Uninstall** and follow the wizard.
4. Or use the uninstaller shortcut in Start Menu → NetSpeedWidget → Uninstall.

---

## 🔧 Troubleshooting

### Widget does not appear on startup
- Ensure the executable path in the registry is correct:
  ```powershell
  Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "NetSpeedWidget"
  ```
- If the path points to a deleted location, uncheck and re-check "Start with Windows" in Settings.

### "This app requires .NET 8.0 Desktop Runtime" error
- You are using a **framework-dependent** build. Either:
  - Download and install the [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime) (x64).
  - Or rebuild with `-SelfContained` to embed the runtime.

### Speed readings show 0.0 Mbps constantly
- Check the **Network Adapter** dropdown in Settings. Try switching from "Auto" to a specific adapter.
- Ensure the adapter is operational: open **Network Diagnostics** (🔍) to verify the adapter status.

### Ping shows "Timeout" continuously
- The default ping host is `1.1.1.1` (Cloudflare DNS). If your network blocks ICMP, try changing the Ping Host to `8.8.8.8` or `google.com` in Settings.

### Widget interferes with clicking underlying windows
- You may have accidentally enabled **Ghost / Click-Through Mode**. Right-click the **System Tray icon** → uncheck "Ghost / Click-Through Mode".

### High CPU usage
- Switch the refresh rate to **Battery (2s)** in Settings.
- Disable the **Speed Graph** if not needed.

### Speed Test results seem too low or too high
- Speed Test results are **CDN-based estimates** using Cloudflare Edge nodes. Factors like routing, congestion, and server load affect results.
- For accurate ISP benchmarks, use a dedicated service like [speedtest.net](https://www.speedtest.net/) in a browser.

### Daily data counter did not reset at midnight
- The counter resets on the first network tick after midnight. If the app was closed at midnight, it resets the next time it starts.
- To manually reset: click the **↺** button on the Data Usage card, or open Settings → set **Daily Quota** to `0` and back.

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for full details.
