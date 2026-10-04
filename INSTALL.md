# 📥 NetSpeedWidget — Installation Guide

This document provides detailed, step-by-step installation instructions for every supported method. No administrator privileges are required for any of these methods.

---

## Prerequisites

### For End Users (Pre-Built Release)
- **Windows 10 or 11** (64-bit)
- No other software is required (the self-contained release includes everything)

### For Framework-Dependent Release
- **Windows 10 or 11** (64-bit)
- **.NET 8.0 Desktop Runtime** (x64) — [Download here](https://dotnet.microsoft.com/download/dotnet/8.0/runtime)
  ```powershell
  # Verify .NET Desktop Runtime is installed:
  dotnet --list-runtimes
  # Should show: Microsoft.WindowsDesktop.App 8.0.x [...]
  ```

### For Building from Source
- **Windows 10 or 11** (64-bit)
- **.NET 8.0 SDK** — [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
  ```powershell
  # Verify SDK is installed:
  dotnet --version
  # Output: 8.0.xxx
  ```
- **Git** (optional, for cloning) — [Download here](https://git-scm.com/downloads)

---

## Method 1: Portable ZIP (Recommended)

**Best for:** Users who want a quick, clean installation with no system changes.

### Step 1: Obtain the portable ZIP

Download or locate:
```
NetSpeedWidget-1.0.0-win-x64-portable.zip
```
If building from source, this is generated in `artifacts\dist\` by the packaging script.

### Step 2: Verify integrity (optional but recommended)

Open **PowerShell** and run:
```powershell
$hash = (Get-FileHash "NetSpeedWidget-1.0.0-win-x64-portable.zip" -Algorithm SHA256).Hash
Write-Host "SHA256: $hash"
```
Compare the output with the corresponding hash in `SHA256SUMS.txt`.

### Step 3: Extract the archive

**Using Windows File Explorer:**
1. Right-click the `.zip` file.
2. Click **"Extract All..."**.
3. Enter a destination path. Suggested locations:
   ```
   C:\Users\<YourName>\Programs\NetSpeedWidget\
   D:\Tools\NetSpeedWidget\
   C:\PortableApps\NetSpeedWidget\
   ```
4. Click **"Extract"**.

**Using PowerShell:**
```powershell
Expand-Archive -Path "NetSpeedWidget-1.0.0-win-x64-portable.zip" -DestinationPath "$env:USERPROFILE\Programs\NetSpeedWidget"
```

### Step 4: Launch the application

1. Open the folder where you extracted the files.
2. Double-click **`NetSpeedWidget.exe`**.
3. The widget appears on your desktop with a glassmorphic overlay.
4. A system tray icon ( 🌐 ) appears in the Windows notification area.

> **First-run note:** Windows SmartScreen may show a warning for unsigned executables. Click **"More info"** → **"Run anyway"** if you trust the source.

### Step 5: Configure auto-start (optional)

**Via the Widget UI:**
1. Click **⚙** (Settings gear icon) on the widget.
2. Scroll down and check **"Start with Windows"**.
3. Done — the app will launch minimized on every login.

**Via Registry (manual):**
```powershell
$exePath = "C:\Users\<YourName>\Programs\NetSpeedWidget\NetSpeedWidget.exe"
Set-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "NetSpeedWidget" -Value "`"$exePath`" --minimized"
```

### Step 6: Create a desktop shortcut (optional)

```powershell
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\NetSpeedWidget.lnk")
$Shortcut.TargetPath = "$env:USERPROFILE\Programs\NetSpeedWidget\NetSpeedWidget.exe"
$Shortcut.WorkingDirectory = "$env:USERPROFILE\Programs\NetSpeedWidget"
$Shortcut.Description = "Internet Speed Desktop Widget"
$Shortcut.Save()
```

---

## Method 2: PowerShell Installer Bundle

**Best for:** Users who want an automated installation with shortcuts and proper app placement.

### Step 1: Obtain the installer bundle

Download or locate:
```
NetSpeedWidget-Setup-1.0.0.zip
```

### Step 2: Extract the installer bundle

```powershell
Expand-Archive -Path "NetSpeedWidget-Setup-1.0.0.zip" -DestinationPath "$env:TEMP\NetSpeedWidget-Setup"
```
Or right-click → "Extract All..." in File Explorer.

### Step 3: Review the contents

The extracted folder contains:
```
NetSpeedWidget-Setup-1.0.0/
├── Install.bat        ← Double-click this to install
├── Install.ps1        ← PowerShell installer script
└── app/               ← Application binaries
    ├── NetSpeedWidget.exe
    └── ... (supporting files)
```

### Step 4: Run the installer

**Option A — Double-click `Install.bat`:**
1. Double-click `Install.bat`.
2. A PowerShell window opens and displays installation progress.
3. Wait for the "✓ NetSpeedWidget installed successfully!" message.

**Option B — Run `Install.ps1` directly:**
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
& "$env:TEMP\NetSpeedWidget-Setup\NetSpeedWidget-Setup-1.0.0\Install.ps1"
```

### Step 5: What the installer does (detailed)

| Step | Action | Detail |
| :--- | :--- | :--- |
| 1 | Terminates existing instances | Stops any running `NetSpeedWidget.exe` process |
| 2 | Creates install directory | `%LOCALAPPDATA%\NetSpeedWidget\` |
| 3 | Copies application files | All files from `app/` → install directory |
| 4 | Creates Start Menu shortcut | `%APPDATA%\Microsoft\Windows\Start Menu\Programs\NetSpeedWidget\NetSpeedWidget.lnk` |
| 5 | Creates Desktop shortcut | `%USERPROFILE%\Desktop\NetSpeedWidget.lnk` |
| 6 | Launches NetSpeedWidget | Starts the widget immediately |

### Step 6: Verify installation

- ✅ Widget appears on your desktop.
- ✅ "NetSpeedWidget" shortcut exists on your Desktop.
- ✅ "NetSpeedWidget" appears in Start Menu under Programs.
- ✅ System tray icon is visible.

### Step 7: Updating

To update, simply re-run the installer with the new version. It will:
1. Stop the running instance.
2. Overwrite the files in `%LOCALAPPDATA%\NetSpeedWidget\`.
3. Re-launch the updated version.

Your settings in `%LOCALAPPDATA%\NetSpeedWidget\settings.json` are preserved.

---

## Method 3: Inno Setup Installer (.exe) (Recommended)

**Best for:** Most users who want a traditional Windows installer with wizard UI and built-in uninstaller.

### Prerequisites
This installer is only available when built on a machine with [Inno Setup 6](https://jrsoftware.org/isdl.php) installed. It produces a professional `.exe` setup file.

### Step 1: Run the installer
1. Double-click **`NetSpeedWidget-Setup-1.0.0.exe`**.
2. If Windows SmartScreen appears, click **"More info"** → **"Run anyway"**.

### Step 2: Follow the setup wizard

| Wizard Screen | Action |
| :--- | :--- |
| **Welcome** | Click **"Next"** |
| **License Agreement** | Read and accept → Click **"Next"** |
| **Destination Location** | Default: `%LOCALAPPDATA%\Programs\NetSpeedWidget\`. Change if desired → Click **"Next"** |
| **Additional Tasks** | ☐ Create a Desktop shortcut (optional) |
| | ☐ Start NetSpeedWidget automatically when Windows starts (optional) |
| **Ready to Install** | Review settings → Click **"Install"** |
| **Completing** | ☐ Launch NetSpeedWidget (checked by default) → Click **"Finish"** |

### Step 3: Post-installation

The installer configures:
- ✅ Application files in the chosen directory.
- ✅ Start Menu folder with "NetSpeedWidget" and "Uninstall NetSpeedWidget" shortcuts.
- ✅ (Optional) Desktop shortcut.
- ✅ (Optional) Auto-start via `HKCU\...\Run` registry entry.
- ✅ Full uninstaller registered in Windows "Apps & Features".

### Uninstalling

Three ways to uninstall:
1. **Settings → Apps → Apps & Features** → Search "NetSpeedWidget" → Click "Uninstall".
2. **Start Menu** → NetSpeedWidget → "Uninstall NetSpeedWidget".
3. **Direct**: Run `unins000.exe` from the installation directory.

---

## Method 4: Windows Installer (.msi)

**Best for:** Enterprise deployments, active directory rollouts, or users who prefer the native Windows Installer engine.

### Prerequisites
None. The MSI package contains the self-contained application and installs without needing the .NET runtime.

### Step 1: Run the installer
1. Double-click **`NetSpeedWidget-Setup-1.0.0.msi`**.
2. Follow the standard Windows Installer prompts.

### Step 2: Post-installation
The MSI configures:
- ✅ Application files in `%LOCALAPPDATA%\Programs\NetSpeedWidget\`.
- ✅ Start Menu shortcuts.
- ✅ Full uninstaller registered in Windows "Apps & Features".

### Uninstalling
1. **Settings → Apps → Apps & Features** → Search "NetSpeedWidget" → Click "Uninstall".
2. Or run: `msiexec /x NetSpeedWidget-Setup-1.0.0.msi`

---

## Method 4: Build from Source

**Best for:** Developers who want to customize, contribute, or audit the code.

### Step 1: Clone the repository

```powershell
git clone <repository-url>
cd internet
```
Or download and extract the source ZIP.

### Step 2: Restore NuGet dependencies

```powershell
dotnet restore src\NetSpeedWidget\NetSpeedWidget.csproj
```

### Step 3: Generate the application icon

The icon is generated programmatically via a PowerShell script:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\generate-icon.ps1
```
This creates `src\NetSpeedWidget\Resources\app.ico` and `app.png`.

### Step 4: Build (Debug mode)

```powershell
dotnet build src\NetSpeedWidget\NetSpeedWidget.csproj -c Debug
```

### Step 5: Run in development mode

```powershell
dotnet run --project src\NetSpeedWidget\NetSpeedWidget.csproj
```

### Step 6: Run unit tests

```powershell
dotnet test tests\NetSpeedWidget.Tests\NetSpeedWidget.Tests.csproj
```
All 9 tests should pass:
```
Passed!  - Failed: 0, Passed: 9, Skipped: 0, Total: 9
```

### Step 7: Build a Release binary

**Framework-Dependent** (~400 KB, requires .NET 8.0 Desktop Runtime on target):
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1
```

**Self-Contained** (~72 MB, runs on any Windows 10/11 x64 without .NET):
```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1 -SelfContained
```

Output: `artifacts\publish\NetSpeedWidget.exe`

### Step 8: Package for distribution

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package.ps1 -Version 1.0.0 -SelfContained
```

This produces the following in `artifacts\dist\`:
```
NetSpeedWidget-1.0.0-win-x64-portable.zip    ← Portable ZIP
NetSpeedWidget-Setup-1.0.0.zip                ← Installer bundle
SHA256SUMS.txt                                ← Checksum verification
```

---

## Post-Installation Configuration

After installing via any method, here's how to get the most out of NetSpeedWidget:

### Recommended First Steps

1. **Position the widget**: Drag it to your preferred screen location (it snaps to edges).
2. **Pin it**: Click 📌 to keep it always on top.
3. **Choose your theme**: ⚙ Settings → Theme → Dark or Light.
4. **Set daily quota** (if needed): ⚙ Settings → Daily Quota → Enter MB limit.
5. **Enable auto-start**: ⚙ Settings → Check "Start with Windows".
6. **Run a speed test**: Click ⚡ SPEED TEST to benchmark your connection.

### Data & Configuration Files

All user data is stored in a single folder that is safe to back up or migrate:

```
%LOCALAPPDATA%\NetSpeedWidget\
├── settings.json     ← All user preferences, window position, and daily counters
└── history.json      ← Speed test history records
```

To **back up** your configuration:
```powershell
Copy-Item -Recurse "$env:LOCALAPPDATA\NetSpeedWidget" "$env:USERPROFILE\Desktop\NetSpeedWidget-Backup"
```

To **restore** a backup:
```powershell
Copy-Item -Recurse "$env:USERPROFILE\Desktop\NetSpeedWidget-Backup\*" "$env:LOCALAPPDATA\NetSpeedWidget" -Force
```

To **reset all settings** to defaults:
```powershell
Remove-Item -Recurse -Force "$env:LOCALAPPDATA\NetSpeedWidget"
```

---

## Troubleshooting Installation Issues

| Problem | Cause | Solution |
| :--- | :--- | :--- |
| "This app requires .NET 8.0" dialog | Framework-dependent build on a machine without the runtime | Install [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0/runtime) or use the self-contained build |
| Windows SmartScreen blocks the app | Unsigned executable from an unknown publisher | Click "More info" → "Run anyway" |
| `Install.ps1` shows "script is not digitally signed" | PowerShell execution policy is Restricted | Run: `powershell -ExecutionPolicy Bypass -File Install.ps1` |
| App runs but shows 0.0 Mbps | Network adapter not detected or wrong adapter selected | Open ⚙ Settings → Network Adapter → Select the correct adapter |
| "Another instance is already running" | Mutex-enforced single instance | Close the existing instance from the System Tray → Exit |
| Widget is invisible / cannot click it | Ghost / Click-Through mode is enabled | Right-click the System Tray icon → Uncheck "Ghost / Click-Through Mode" |
| `dotnet` command not recognized | .NET SDK not in PATH | Restart your terminal, or add the .NET SDK directory to your PATH environment variable |
| `build.ps1` fails with "PublishSingleFile" error | .NET SDK version < 8.0 | Update to .NET 8.0 SDK |

---

## Summary Table

| Method | Difficulty | Admin Required | Uninstaller | Best For |
| :--- | :--- | :--- | :--- | :--- |
| **Portable ZIP** | ⭐ Easy | ❌ No | Manual delete | Quick use, USB drives, testing |
| **PowerShell Installer** | ⭐⭐ Easy | ❌ No | Manual (PowerShell) | Standard users, shortcuts |
| **Inno Setup .exe** | ⭐ Easy | ❌ No | ✅ Built-in | Professional deployment |
| **Windows Installer .msi** | ⭐ Easy | ❌ No | ✅ Built-in | Enterprise deployment |
| **Build from Source** | ⭐⭐⭐ Moderate | ❌ No | Manual delete | Developers, contributors |
