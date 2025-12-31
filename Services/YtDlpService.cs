using System;
using System.Collections.Generic;
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

        private static string AppDataRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "dlapp"
        );

        private readonly string _ytDlpPath = Path.Combine(AppDataRoot, "yt-dlp.exe");
        private readonly string _ffmpegPath = Path.Combine(AppDataRoot, "ffmpeg.exe");

        public YtDlpService()
        {
        }

        public bool IsReady => File.Exists(_ytDlpPath) && File.Exists(_ffmpegPath);

        public async Task InitializeAsync(IProgress<string> statusCallback)
        {
            Directory.CreateDirectory(AppDataRoot);

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
                var zipPath = Path.Combine(AppDataRoot, "ffmpeg.zip");
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
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                var p = Process.Start(psi);
                if (p != null)
                {
                    await p.StandardError.ReadToEndAsync();
                    await p.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"yt-dlp update failed: {ex.Message}");
            }
        }

        public async Task<List<(string Index, string Title)>> GetVideoInfoAsync(string url, bool isPlaylist, System.Threading.CancellationToken cancellationToken = default)
        {
            if (!IsReady) throw new InvalidOperationException("Dependencies not ready.");

            var items = new List<(string Index, string Title)>();

            var baseInfoArgs = "--skip-download --quiet --no-warnings";
            var psi = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = isPlaylist
                    ? $"{baseInfoArgs} --flat-playlist --print \"%(playlist_index)s|%(title)s\" \"{url}\""
                    : $"{baseInfoArgs} --no-playlist --playlist-items 1 --max-downloads 1 --print \"%(title)s\" \"{url}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            var processResult = await Task.Run(() =>
            {
                using var process = new Process { StartInfo = psi };
                process.Start();
                return process;
            }, cancellationToken).ConfigureAwait(false);

            using var process = processResult;

            var errorTask = process.StandardError.ReadToEndAsync();

            int counter = 1;
            var addedSingle = false;
            while (!process.StandardOutput.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("[", StringComparison.Ordinal)) continue;

                if (isPlaylist)
                {
                    var parts = line.Split('|', 2);
                    if (parts.Length == 2)
                    {
                        var index = parts[0].Trim();
                        if (string.IsNullOrEmpty(index)) index = counter.ToString();
                        items.Add((index, parts[1].Trim()));
                    }
                    else
                    {
                        items.Add((counter.ToString(), line.Trim()));
                    }
                    counter++;
                }
                else
                {
                    if (!addedSingle)
                    {
                        items.Add(("1", line.Trim()));
                        addedSingle = true;
                        break;
                    }
                }
            }

            await process.WaitForExitAsync(cancellationToken);
            await errorTask;

            if (!isPlaylist && items.Count > 1)
            {
                return new List<(string Index, string Title)> { items[0] };
            }

            return items;
        }

        public async Task DownloadVideoAsync(string url, string savePath, bool audioOnly, bool isPlaylist, int? maxVideoHeight, string containerFormat, IProgress<string> outputCallback, IProgress<double> progressCallback, System.Threading.CancellationToken cancellationToken = default)
        {
            if (!IsReady) throw new InvalidOperationException("Dependencies not ready.");

            // Output template to avoid weird filenames
            string outputTemplate = isPlaylist
                ? "%(playlist_title)s/%(playlist_index)s - %(title)s.%(ext)s"
                : "%(title)s.%(ext)s";

            // Force ffmpeg location just in case it's not in PATH (it's in current dir, but explicit is safer)
            string ffmpegArg = $"--ffmpeg-location \"{_ffmpegPath}\"";

            string playlistArg = isPlaylist ? "--yes-playlist" : "--no-playlist";

            string args;
            if (audioOnly)
            {
                // Audio Only: Extract audio and convert to selected format
                args = $"{ffmpegArg} {playlistArg} -x --audio-format {containerFormat} --progress-template \"%(progress._percent_str)s\" -o \"{outputTemplate}\" \"{url}\"";
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

                args = $"{ffmpegArg} {playlistArg} {formatArg} {mergeArg} --progress-template \"%(progress._percent_str)s\" -o \"{outputTemplate}\" \"{url}\"";
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

            await Task.Run(() =>
            {
                using var process = new Process { StartInfo = psi };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        var data = e.Data.Trim();
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
                    if (!string.IsNullOrEmpty(e.Data)) outputCallback.Report(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
