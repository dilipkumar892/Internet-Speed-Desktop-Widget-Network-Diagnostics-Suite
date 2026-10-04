using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSpeedWidget.Services;

public class NetworkDiagnosticReport
{
    public string AdapterName { get; set; } = "Unknown";
    public string AdapterDescription { get; set; } = "Unknown";
    public string InterfaceType { get; set; } = "Unknown";
    public string MacAddress { get; set; } = "00:00:00:00:00:00";
    public string LinkSpeed { get; set; } = "Unknown";
    public string Status { get; set; } = "Down";

    public string LocalIpv4 { get; set; } = "127.0.0.1";
    public string SubnetMask { get; set; } = "255.255.255.0";
    public string LocalIpv6 { get; set; } = "None";
    public string DefaultGateway { get; set; } = "None";
    public List<string> DnsServers { get; set; } = new();

    public string? PublicIp { get; set; }
    public string? IspLocation { get; set; }

    public string? WifiSsid { get; set; }
    public string? WifiBssid { get; set; }
    public int WifiSignalPercent { get; set; }
    public string? WifiRadioType { get; set; }
    public string? WifiChannel { get; set; }

    public Dictionary<string, long> PingResults { get; set; } = new();

    public string ToFormattedReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== NetSpeedWidget Network Diagnostic Report ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("[ Network Adapter ]");
        sb.AppendLine($"Name:            {AdapterName}");
        sb.AppendLine($"Description:     {AdapterDescription}");
        sb.AppendLine($"Interface Type:  {InterfaceType}");
        sb.AppendLine($"Link Speed:      {LinkSpeed}");
        sb.AppendLine($"MAC Address:     {MacAddress}");
        sb.AppendLine($"Status:          {Status}");
        sb.AppendLine();
        sb.AppendLine("[ IP & Routing ]");
        sb.AppendLine($"IPv4 Address:    {LocalIpv4}");
        sb.AppendLine($"Subnet Mask:     {SubnetMask}");
        sb.AppendLine($"IPv6 Address:    {LocalIpv6}");
        sb.AppendLine($"Default Gateway: {DefaultGateway}");
        sb.AppendLine($"DNS Servers:     {string.Join(", ", DnsServers)}");
        sb.AppendLine($"Public IP:       {PublicIp ?? "Detecting..."}");
        if (!string.IsNullOrEmpty(IspLocation))
            sb.AppendLine($"Location/Edge:   {IspLocation}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(WifiSsid))
        {
            sb.AppendLine("[ Wi-Fi Parameters ]");
            sb.AppendLine($"SSID:            {WifiSsid}");
            sb.AppendLine($"BSSID:           {WifiBssid ?? "N/A"}");
            sb.AppendLine($"Signal Strength: {WifiSignalPercent}%");
            sb.AppendLine($"Radio Type:      {WifiRadioType ?? "N/A"}");
            sb.AppendLine($"Channel:         {WifiChannel ?? "N/A"}");
            sb.AppendLine();
        }

        if (PingResults.Count > 0)
        {
            sb.AppendLine("[ Multi-DNS Latency ]");
            foreach (var kvp in PingResults)
            {
                sb.AppendLine($"{kvp.Key,-20}: {(kvp.Value >= 0 ? kvp.Value + " ms" : "Timeout")}");
            }
        }

        return sb.ToString();
    }
}

public class NetworkDiagnosticService
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<NetworkDiagnosticReport> RunDiagnosticsAsync(string? preferredAdapterId = null, CancellationToken token = default)
    {
        var report = new NetworkDiagnosticReport();

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            NetworkInterface? active = null;
            if (!string.IsNullOrEmpty(preferredAdapterId) && preferredAdapterId != "auto")
            {
                active = interfaces.FirstOrDefault(ni => ni.Id == preferredAdapterId);
            }

            if (active == null)
            {
                active = interfaces.FirstOrDefault(ni =>
                    ni.OperationalStatus == OperationalStatus.Up &&
                    ni.GetIPProperties().GatewayAddresses.Any(g => g.Address != null && !IPAddress.Any.Equals(g.Address)))
                    ?? interfaces.FirstOrDefault(ni => ni.OperationalStatus == OperationalStatus.Up)
                    ?? interfaces.FirstOrDefault();
            }

            if (active != null)
            {
                report.AdapterName = active.Name;
                report.AdapterDescription = active.Description;
                report.InterfaceType = active.NetworkInterfaceType.ToString();
                report.Status = active.OperationalStatus.ToString();

                // Format MAC address
                var bytes = active.GetPhysicalAddress().GetAddressBytes();
                report.MacAddress = bytes.Length > 0
                    ? string.Join(":", bytes.Select(b => b.ToString("X2")))
                    : "Not Available";

                // Format Speed
                long speedBps = active.Speed;
                if (speedBps > 0)
                {
                    if (speedBps >= 1_000_000_000)
                        report.LinkSpeed = $"{speedBps / 1_000_000_000.0:F1} Gbps";
                    else if (speedBps >= 1_000_000)
                        report.LinkSpeed = $"{speedBps / 1_000_000} Mbps";
                    else
                        report.LinkSpeed = $"{speedBps / 1000} Kbps";
                }

                var ipProps = active.GetIPProperties();

                // IPv4 & Subnet Mask
                var ipv4Info = ipProps.UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4Info != null)
                {
                    report.LocalIpv4 = ipv4Info.Address.ToString();
                    report.SubnetMask = ipv4Info.IPv4Mask?.ToString() ?? "255.255.255.0";
                }

                // IPv6
                var ipv6Info = ipProps.UnicastAddresses
                    .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetworkV6 && !u.Address.IsIPv6LinkLocal);
                if (ipv6Info != null)
                {
                    report.LocalIpv6 = ipv6Info.Address.ToString();
                }

                // Gateway
                var gateway = ipProps.GatewayAddresses
                    .FirstOrDefault(g => g.Address != null && !IPAddress.Any.Equals(g.Address));
                if (gateway != null)
                {
                    report.DefaultGateway = gateway.Address.ToString();
                }

                // DNS
                foreach (var dns in ipProps.DnsAddresses)
                {
                    if (dns.AddressFamily == AddressFamily.InterNetwork)
                        report.DnsServers.Add(dns.ToString());
                }
            }

            // Wi-Fi details via netsh
            PopulateWifiDetails(report);

            // Fetch Public IP
            try
            {
                var (pubIp, loc) = await FetchPublicIpAsync(token);
                report.PublicIp = pubIp;
                report.IspLocation = loc;
            }
            catch { }

            // Multi-DNS ping
            report.PingResults = await BenchmarkDnsServersAsync(token);
        }
        catch (Exception ex)
        {
            report.AdapterDescription = $"Diagnostic error: {ex.Message}";
        }

        return report;
    }

    private static async Task<(string? Ip, string? Location)> FetchPublicIpAsync(CancellationToken token)
    {
        try
        {
            // Use Cloudflare trace for fast IP and colo code
            string response = await HttpClient.GetStringAsync("https://1.1.1.1/cdn-cgi/trace", token);
            string? ip = null;
            string? colo = null;
            string? loc = null;

            foreach (var line in response.Split('\n'))
            {
                if (line.StartsWith("ip=")) ip = line.Substring(3).Trim();
                else if (line.StartsWith("colo=")) colo = line.Substring(5).Trim();
                else if (line.StartsWith("loc=")) loc = line.Substring(4).Trim();
            }

            string location = $"{loc ?? "Global"} ({colo ?? "Anycast"})";
            return (ip, location);
        }
        catch
        {
            try
            {
                // Fallback to ipify
                string ip = (await HttpClient.GetStringAsync("https://api.ipify.org", token)).Trim();
                return (ip, null);
            }
            catch
            {
                return (null, null);
            }
        }
    }

    private static void PopulateWifiDetails(NetworkDiagnosticReport report)
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
            if (process == null) return;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000);

            foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                var parts = line.Split(':', 2);
                if (parts.Length != 2) continue;
                string key = parts[0].Trim();
                string val = parts[1].Trim();

                if (key.Equals("SSID", StringComparison.OrdinalIgnoreCase) && !key.Equals("BSSID", StringComparison.OrdinalIgnoreCase))
                    report.WifiSsid = val;
                else if (key.Equals("BSSID", StringComparison.OrdinalIgnoreCase))
                    report.WifiBssid = val;
                else if (key.Equals("Signal", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(val.Replace("%", "").Trim(), out int sig))
                        report.WifiSignalPercent = sig;
                }
                else if (key.Equals("Radio type", StringComparison.OrdinalIgnoreCase))
                    report.WifiRadioType = val;
                else if (key.Equals("Channel", StringComparison.OrdinalIgnoreCase))
                    report.WifiChannel = val;
            }
        }
        catch { }
    }

    private static async Task<Dictionary<string, long>> BenchmarkDnsServersAsync(CancellationToken token)
    {
        var targets = new Dictionary<string, string>
        {
            ["Cloudflare (1.1.1.1)"] = "1.1.1.1",
            ["Google (8.8.8.8)"] = "8.8.8.8",
            ["Quad9 (9.9.9.9)"] = "9.9.9.9"
        };

        var results = new Dictionary<string, long>();

        foreach (var kvp in targets)
        {
            if (token.IsCancellationRequested) break;
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(kvp.Value, 1000);
                results[kvp.Key] = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
            }
            catch
            {
                results[kvp.Key] = -1;
            }
        }

        return results;
    }
}
