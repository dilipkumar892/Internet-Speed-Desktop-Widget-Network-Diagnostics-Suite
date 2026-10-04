using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetSpeedWidget.Services;

public class NetworkInfoService
{
    public record WifiDetails(string? Ssid, int SignalQuality);

    public static string GetActiveLocalIp()
    {
        try
        {
            // Connect dummy socket to determine route
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint)
            {
                return endPoint.Address.ToString();
            }
        }
        catch
        {
            // Fallback: search interfaces
            try
            {
                var ip = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                    .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)?
                    .Address.ToString();

                if (!string.IsNullOrEmpty(ip)) return ip;
            }
            catch { }
        }

        return "127.0.0.1";
    }

    public static (string ConnectionType, string InterfaceName) GetActiveConnectionInfo(string? preferredInterfaceId = null)
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            if (!interfaces.Any())
            {
                return ("Disconnected", "No Interface");
            }

            NetworkInterface? target = null;
            if (!string.IsNullOrEmpty(preferredInterfaceId) && preferredInterfaceId != "auto")
            {
                target = interfaces.FirstOrDefault(ni => ni.Id == preferredInterfaceId);
            }

            if (target == null)
            {
                // Find interface with default gateway
                target = interfaces.FirstOrDefault(ni =>
                    ni.GetIPProperties().GatewayAddresses.Any(g => g.Address != null && !IPAddress.Any.Equals(g.Address)))
                    ?? interfaces.First();
            }

            string type = target.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                NetworkInterfaceType.Ethernet => "Ethernet",
                NetworkInterfaceType.GigabitEthernet => "Ethernet",
                NetworkInterfaceType.Wman or NetworkInterfaceType.Wwanpp => "Cellular",
                _ => target.Name.Contains("Wi-Fi", StringComparison.OrdinalIgnoreCase) ||
                     target.Description.Contains("Wireless", StringComparison.OrdinalIgnoreCase)
                    ? "Wi-Fi"
                    : "Ethernet"
            };

            return (type, target.Name);
        }
        catch
        {
            return ("Disconnected", "Error");
        }
    }

    public static WifiDetails GetWifiDetails()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = "wlan show interfaces",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return new WifiDetails(null, 0);

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000);

            string? ssid = null;
            int signal = 0;

            foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) && !line.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        ssid = parts[1].Trim();
                    }
                }
                else if (line.StartsWith("Signal", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var rawSignal = parts[1].Trim().Replace("%", "");
                        int.TryParse(rawSignal, out signal);
                    }
                }
            }

            return new WifiDetails(ssid, signal);
        }
        catch
        {
            return new WifiDetails(null, 0);
        }
    }
}
