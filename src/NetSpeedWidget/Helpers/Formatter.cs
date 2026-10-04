namespace NetSpeedWidget.Helpers;

public static class Formatter
{
    public static string FormatSpeed(double bytesPerSec, string unitPreference = "Auto")
    {
        if (bytesPerSec <= 0) return "0.0 Mbps";

        // Bit rate calculation: 1 Byte = 8 bits
        double bitsPerSec = bytesPerSec * 8.0;

        if (unitPreference == "MB/s")
        {
            double mbPerSec = bytesPerSec / (1024.0 * 1024.0);
            return mbPerSec >= 100 ? $"{mbPerSec:F0} MB/s" : $"{mbPerSec:F1} MB/s";
        }
        else if (unitPreference == "Kbps")
        {
            double kbps = bitsPerSec / 1000.0;
            return $"{kbps:F0} Kbps";
        }
        else if (unitPreference == "Mbps")
        {
            double mbps = bitsPerSec / 1_000_000.0;
            return mbps >= 100 ? $"{mbps:F0} Mbps" : $"{mbps:F1} Mbps";
        }

        // Auto mode
        if (bitsPerSec >= 1_000_000_000.0)
        {
            double gbps = bitsPerSec / 1_000_000_000.0;
            return $"{gbps:F2} Gbps";
        }
        else if (bitsPerSec >= 1_000_000.0)
        {
            double mbps = bitsPerSec / 1_000_000.0;
            return mbps >= 100 ? $"{mbps:F0} Mbps" : $"{mbps:F1} Mbps";
        }
        else if (bitsPerSec >= 1000.0)
        {
            double kbps = bitsPerSec / 1000.0;
            return $"{kbps:F0} Kbps";
        }
        else
        {
            return $"{bitsPerSec:F0} bps";
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:F1} KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:F1} MB";
        double gb = mb / 1024.0;
        return $"{gb:F2} GB";
    }
}
