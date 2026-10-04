Add-Type -AssemblyName System.Drawing

$resDir = "c:\Users\Dilip\Documents\Projects\internet\src\NetSpeedWidget\Resources"
if (-not (Test-Path $resDir)) {
    New-Item -ItemType Directory -Force -Path $resDir | Out-Null
}

$bmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.Clear([System.Drawing.Color]::Transparent)

# Background circle with gradient
$rect = New-Object System.Drawing.Rectangle 12, 12, 232, 232
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 15, 23, 42)), ([System.Drawing.Color]::FromArgb(255, 30, 41, 59)), 45.0
$g.FillEllipse($brush, $rect)

# Glow border
$borderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(220, 56, 189, 248)), 8
$borderRect = New-Object System.Drawing.Rectangle 16, 16, 224, 224
$g.DrawEllipse($borderPen, $borderRect)

# Speed curve arc
$arcPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 168, 85, 247)), 12
$arcPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$arcPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawArc($arcPen, 40, 40, 176, 176, 140, 260)

# Inner circle
$innerBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 10, 15, 30))
$g.FillEllipse($innerBrush, 68, 68, 120, 120)

# Arrow Down (Cyan)
$downBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 56, 189, 248))
$downPts = [System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 105, 95),
    (New-Object System.Drawing.Point 105, 130),
    (New-Object System.Drawing.Point 93, 130),
    (New-Object System.Drawing.Point 112, 156),
    (New-Object System.Drawing.Point 131, 130),
    (New-Object System.Drawing.Point 119, 130),
    (New-Object System.Drawing.Point 119, 95)
)
$g.FillPolygon($downBrush, $downPts)

# Arrow Up (Purple)
$upBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 168, 85, 247))
$upPts = [System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 145, 155),
    (New-Object System.Drawing.Point 145, 120),
    (New-Object System.Drawing.Point 133, 120),
    (New-Object System.Drawing.Point 152, 94),
    (New-Object System.Drawing.Point 171, 120),
    (New-Object System.Drawing.Point 159, 120),
    (New-Object System.Drawing.Point 159, 155)
)
$g.FillPolygon($upBrush, $upPts)

$pngPath = Join-Path $resDir "app.png"
$bmp.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

$icoPath = Join-Path $resDir "app.ico"
$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = New-Object System.IO.FileStream $icoPath, ([System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()

$g.Dispose()
$bmp.Dispose()
Write-Host "Icons generated successfully at $icoPath and $pngPath"
