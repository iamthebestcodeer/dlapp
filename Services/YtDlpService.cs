using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace dlapp.Services
{
    public class YtDlpService
    {
        private const string YtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        // Using a reliable source for ffmpeg static builds
        private const string FfmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

        private readonly string _baseDir;
        private readonly string _ytDlpPath;
        private readonly string _ffmpegPath;

        public YtDlpService()
        {
            _baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _ytDlpPath = Path.Combine(_baseDir, "yt-dlp.exe");
            _ffmpegPath = Path.Combine(_baseDir, "ffmpeg.exe");
        }

        public bool IsReady => File.Exists(_ytDlpPath) && File.Exists(_ffmpegPath);

        public async Task InitializeAsync(IProgress<string> statusCallback)
        {
            if (!File.Exists(_ytDlpPath))
            {
                statusCallback.Report("Downloading yt-dlp...");
                await DownloadFileAsync(YtDlpUrl, _ytDlpPath);
                statusCallback.Report("yt-dlp downloaded.");
            }
            else
            {
                // Auto-update yt-dlp on start if it exists
                statusCallback.Report("Checking for updates...");
                await UpdateYtDlpAsync();
            }

            if (!File.Exists(_ffmpegPath))
            {
                statusCallback.Report("Downloading ffmpeg...");
                var zipPath = Path.Combine(_baseDir, "ffmpeg.zip");
                await DownloadFileAsync(FfmpegUrl, zipPath);

                statusCallback.Report("Extracting ffmpeg...");
                await ExtractFfmpegAsync(zipPath);

                try { File.Delete(zipPath); } catch { }
                statusCallback.Report("ffmpeg ready.");
            }

            statusCallback.Report("Ready.");
        }

        private async Task DownloadFileAsync(string url, string destination)
        {
            using var client = new HttpClient();
            // User-Agent is sometimes required
            client.DefaultRequestHeaders.Add("User-Agent", "dlapp-downloader");
            using var s = await client.GetStreamAsync(url);
            using var fs = new FileStream(destination, FileMode.Create);
            await s.CopyToAsync(fs);
        }

        private async Task ExtractFfmpegAsync(string zipPath)
        {
            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(zipPath);
                foreach (var entry in archive.Entries)
                {
                    // ffmpeg is usually in bin/ffmpeg.exe inside the zip
                    if (entry.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(_ffmpegPath, overwrite: true);
                        // We found it, can break if we assume only one
                        break;
                    }
                }
            });
        }

        public async Task UpdateYtDlpAsync()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ytDlpPath,
                    Arguments = "-U",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = Process.Start(psi);
                if (p != null) await p.WaitForExitAsync();
            }
            catch { /* Ignore update errors if offline or fails */ }
        }

        public async Task DownloadVideoAsync(string url, string savePath, bool audioOnly, int? maxVideoHeight, string containerFormat, IProgress<string> outputCallback, IProgress<double> progressCallback)
        {
            if (!IsReady) throw new InvalidOperationException("Dependencies not ready.");

            // Output template to avoid weird filenames
            string outputTemplate = "%(title)s.%(ext)s";

            // Force ffmpeg location just in case it's not in PATH (it's in current dir, but explicit is safer)
            string ffmpegArg = $"--ffmpeg-location \"{_ffmpegPath}\"";

            string args;
            if (audioOnly)
            {
                // Audio Only: Extract audio and convert to selected format
                args = $"{ffmpegArg} -x --audio-format {containerFormat} --progress-template \"%(progress._percent_str)s\" -o \"{outputTemplate}\" \"{url}\"";
            }
            else
            {
                // Video Mode
                string formatArg = "";
                if (maxVideoHeight.HasValue)
                {
                    // Limit resolution
                    formatArg = $"-f \"bestvideo[height<={maxVideoHeight.Value}]+bestaudio/best[height<={maxVideoHeight.Value}]\"";
                }

                // Merge output format to ensure container
                string mergeArg = $"--merge-output-format {containerFormat}";

                args = $"{ffmpegArg} {formatArg} {mergeArg} --progress-template \"%(progress._percent_str)s\" -o \"{outputTemplate}\" \"{url}\"";
            }

            var psi = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = savePath
            };

            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Try parse progress
                    // Default percentage looks like " 5.5%" or "100.0%"
                    var data = e.Data.Trim();
                    // Handle ANSI codes if any (yt-dlp might output them even with no-color?)
                    // With --progress-template, we just get the percentage string

                    if (data.EndsWith("%") && double.TryParse(data.TrimEnd('%'), out double p))
                    {
                        progressCallback.Report(p);
                    }
                    else
                    {
                        outputCallback.Report(e.Data);
                    }
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                // yt-dlp sends some info to stderr
                if (!string.IsNullOrEmpty(e.Data)) outputCallback.Report(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }
    }
}
