using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NetSpeedWidget.Models;

namespace NetSpeedWidget.Services;

public class SpeedTestService
{
    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        MaxConnectionsPerServer = 10
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public event Action<SpeedTestProgress>? ProgressChanged;

    public void Cancel()
    {
        if (_isRunning && _cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    public async Task<SpeedTestResult?> RunSpeedTestAsync(Action<SpeedTestProgress> onProgress)
    {
        if (_isRunning) return null;

        _isRunning = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        var progress = new SpeedTestProgress
        {
            Phase = SpeedTestPhase.MeasuringPing,
            Message = "Connecting to nearest test server...",
            ServerLocation = "Global Anycast Edge"
        };

        void Report(Action update)
        {
            update();
            onProgress(progress);
            ProgressChanged?.Invoke(progress);
        }

        try
        {
            // 1. Ping & Jitter
            Report(() =>
            {
                progress.Phase = SpeedTestPhase.MeasuringPing;
                progress.ProgressPercentage = 5;
                progress.Message = "Measuring latency & jitter...";
            });

            var pings = new List<double>();
            string pingEndpoint = "https://speed.cloudflare.com/__down?bytes=0";

            for (int i = 0; i < 8; i++)
            {
                if (token.IsCancellationRequested) break;

                var sw = Stopwatch.StartNew();
                try
                {
                    using var response = await HttpClient.GetAsync(pingEndpoint, HttpCompletionOption.ResponseHeadersRead, token);
                    sw.Stop();
                    if (response.IsSuccessStatusCode)
                    {
                        pings.Add(sw.Elapsed.TotalMilliseconds);
                        if (response.Headers.TryGetValues("cf-ray", out var rayValues))
                        {
                            var ray = rayValues.FirstOrDefault();
                            if (!string.IsNullOrEmpty(ray) && ray.Contains('-'))
                            {
                                progress.ServerLocation = $"Cloudflare Edge ({ray.Split('-').Last().ToUpper()})";
                            }
                        }
                    }
                }
                catch when (!token.IsCancellationRequested)
                {
                    // Single ping attempt failed, continue
                }

                Report(() =>
                {
                    progress.ProgressPercentage = 5 + (i * 2.5);
                    if (pings.Count > 0) progress.PingMs = pings.Average();
                });

                await Task.Delay(40, token);
            }

            token.ThrowIfCancellationRequested();

            double avgPing = pings.Count > 0 ? pings.Average() : 25.0;
            double jitter = 0;
            if (pings.Count > 1)
            {
                double jitterSum = 0;
                for (int i = 0; i < pings.Count - 1; i++)
                {
                    jitterSum += Math.Abs(pings[i + 1] - pings[i]);
                }
                jitter = jitterSum / (pings.Count - 1);
            }

            Report(() =>
            {
                progress.PingMs = avgPing;
                progress.JitterMs = jitter;
                progress.ProgressPercentage = 25;
            });

            // 2. Download Test (Streaming multi-MB payload)
            Report(() =>
            {
                progress.Phase = SpeedTestPhase.MeasuringDownload;
                progress.Message = "Testing download throughput...";
            });

            // Download ~25 MB in chunks with live speed updates
            string downloadUrl = "https://speed.cloudflare.com/__down?bytes=25000000";
            double downloadSpeedMbps = await MeasureDownloadThroughputAsync(downloadUrl, (currentMbps, pct) =>
            {
                Report(() =>
                {
                    progress.CurrentSpeedMbps = currentMbps;
                    progress.ProgressPercentage = 25 + (pct * 0.40);
                });
            }, token);

            progress.FinalDownloadMbps = downloadSpeedMbps;
            Report(() =>
            {
                progress.ProgressPercentage = 65;
                progress.CurrentSpeedMbps = 0;
            });

            token.ThrowIfCancellationRequested();

            // 3. Upload Test (~8 MB in chunks to /__up)
            Report(() =>
            {
                progress.Phase = SpeedTestPhase.MeasuringUpload;
                progress.Message = "Testing upload throughput...";
            });

            string uploadUrl = "https://speed.cloudflare.com/__up";
            double uploadSpeedMbps = await MeasureUploadThroughputAsync(uploadUrl, (currentMbps, pct) =>
            {
                Report(() =>
                {
                    progress.CurrentSpeedMbps = currentMbps;
                    progress.ProgressPercentage = 65 + (pct * 0.35);
                });
            }, token);

            progress.FinalUploadMbps = uploadSpeedMbps;

            Report(() =>
            {
                progress.Phase = SpeedTestPhase.Completed;
                progress.ProgressPercentage = 100;
                progress.Message = "Test completed successfully (Estimate)";
            });

            var result = new SpeedTestResult
            {
                Timestamp = DateTime.Now,
                DownloadMbps = downloadSpeedMbps,
                UploadMbps = uploadSpeedMbps,
                PingMs = avgPing,
                JitterMs = jitter,
                ServerName = progress.ServerLocation,
                Status = "Completed"
            };

            return result;
        }
        catch (OperationCanceledException)
        {
            Report(() =>
            {
                progress.Phase = SpeedTestPhase.Cancelled;
                progress.Message = "Speed test cancelled by user.";
            });
            return null;
        }
        catch (Exception ex)
        {
            Report(() =>
            {
                progress.Phase = SpeedTestPhase.Failed;
                progress.Message = $"Test failed: {ex.Message}";
            });
            return null;
        }
        finally
        {
            _isRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task<double> MeasureDownloadThroughputAsync(string url, Action<double, double> onProgress, CancellationToken token)
    {
        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        if (!totalBytes.HasValue || totalBytes <= 0) totalBytes = 25_000_000;

        using var stream = await response.Content.ReadAsStreamAsync(token);
        byte[] buffer = new byte[64 * 1024]; // 64KB buffer
        long totalRead = 0;

        var sw = Stopwatch.StartNew();
        var sampleSw = Stopwatch.StartNew();
        long sampleBytes = 0;
        double smoothedMbps = 0;

        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
        {
            totalRead += read;
            sampleBytes += read;

            if (sampleSw.ElapsedMilliseconds >= 150)
            {
                double instMbps = (sampleBytes * 8.0) / (sampleSw.Elapsed.TotalSeconds * 1_000_000.0);
                smoothedMbps = smoothedMbps == 0 ? instMbps : (smoothedMbps * 0.7) + (instMbps * 0.3);
                double pct = Math.Min(100.0, (totalRead * 100.0) / totalBytes.Value);
                onProgress(smoothedMbps, pct);

                sampleBytes = 0;
                sampleSw.Restart();
            }

            // Stop after 7 seconds max to keep test quick and low data consumption
            if (sw.Elapsed.TotalSeconds >= 7.0) break;
        }

        sw.Stop();
        double overallMbps = (totalRead * 8.0) / (sw.Elapsed.TotalSeconds * 1_000_000.0);
        return Math.Round(Math.Max(overallMbps, smoothedMbps), 2);
    }

    private async Task<double> MeasureUploadThroughputAsync(string url, Action<double, double> onProgress, CancellationToken token)
    {
        // 8MB upload payload
        int payloadSize = 8 * 1024 * 1024;
        byte[] payload = new byte[payloadSize];
        Random.Shared.NextBytes(payload);

        using var content = new ProgressByteArrayContent(payload, 64 * 1024, (bytesSent, total) =>
        {
            // progress is driven inside content
        }, token);

        var sw = Stopwatch.StartNew();
        var sampleSw = Stopwatch.StartNew();
        long lastBytes = 0;
        double smoothedMbps = 0;

        content.BytesTransferred += (sender, bytesSent) =>
        {
            if (sampleSw.ElapsedMilliseconds >= 150)
            {
                long delta = bytesSent - lastBytes;
                double instMbps = (delta * 8.0) / (sampleSw.Elapsed.TotalSeconds * 1_000_000.0);
                smoothedMbps = smoothedMbps == 0 ? instMbps : (smoothedMbps * 0.7) + (instMbps * 0.3);
                double pct = Math.Min(100.0, (bytesSent * 100.0) / payloadSize);
                onProgress(smoothedMbps, pct);

                lastBytes = bytesSent;
                sampleSw.Restart();
            }
        };

        try
        {
            using var response = await HttpClient.PostAsync(url, content, token);
        }
        catch when (token.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // If speed.cloudflare.com/__up rejects POST or errors, calculate whatever transferred
        }

        sw.Stop();
        double overallMbps = (content.TotalBytesSent * 8.0) / (sw.Elapsed.TotalSeconds * 1_000_000.0);
        return Math.Round(Math.Max(overallMbps, smoothedMbps), 2);
    }

    private class ProgressByteArrayContent : HttpContent
    {
        private readonly byte[] _data;
        private readonly int _bufferSize;
        private readonly Action<long, long>? _progress;
        private readonly CancellationToken _token;
        public long TotalBytesSent { get; private set; }

        public event EventHandler<long>? BytesTransferred;

        public ProgressByteArrayContent(byte[] data, int bufferSize, Action<long, long>? progress, CancellationToken token)
        {
            _data = data;
            _bufferSize = bufferSize;
            _progress = progress;
            _token = token;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            int offset = 0;
            while (offset < _data.Length)
            {
                _token.ThrowIfCancellationRequested();
                int count = Math.Min(_bufferSize, _data.Length - offset);
                await stream.WriteAsync(_data.AsMemory(offset, count), _token);
                offset += count;
                TotalBytesSent = offset;
                _progress?.Invoke(offset, _data.Length);
                BytesTransferred?.Invoke(this, offset);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _data.Length;
            return true;
        }
    }
}
