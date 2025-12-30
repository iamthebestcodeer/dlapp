using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dlapp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace dlapp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly YtDlpService _ytDlpService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _savePath;

    [ObservableProperty]
    private bool _isAudioOnly;

    [ObservableProperty]
    private bool _isPlaylist;

    public List<string> Resolutions { get; } = new()
    {
        "Best",
        "2160p (4K)",
        "1440p (2K)",
        "1080p",
        "720p",
        "480p",
        "360p"
    };

    public List<string> VideoFormats { get; } = new() { "mp4", "mkv", "webm" };
    public List<string> AudioFormats { get; } = new() { "mp3", "m4a", "wav" };

    [ObservableProperty]
    private string _selectedResolution = "Best";

    [ObservableProperty]
    private string _selectedVideoFormat = "mp4";

    [ObservableProperty]
    private string _selectedAudioFormat = "mp3";

    [ObservableProperty]
    private string _statusMessage = "Initializing...";

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    private bool _isBusy;

    // Tracks last reported progress to detect when yt-dlp restarts progress for the next item
    private double _lastItemProgress;

    public ObservableCollection<VideoItem> Items { get; } = new();

    // Track seen destination lines to avoid double-advancing when yt-dlp emits multiple destination logs
    private readonly HashSet<string> _seenDestinations = new(StringComparer.OrdinalIgnoreCase);

    public Func<Task<string?>>? ShowOpenFolderDialog { get; set; }

    public MainWindowViewModel()
    {
        _ytDlpService = new YtDlpService();
        _savePath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        if (string.IsNullOrEmpty(_savePath))
        {
            _savePath = AppDomain.CurrentDomain.BaseDirectory;
        }
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        StatusMessage = "Checking dependencies...";

        var progress = new Progress<string>(msg => StatusMessage = msg);
        try
        {
            await _ytDlpService.InitializeAsync(progress);
            StatusMessage = "Ready to download.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error initializing: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectSavePathAsync()
    {
        if (ShowOpenFolderDialog != null)
        {
            var result = await ShowOpenFolderDialog();
            if (!string.IsNullOrEmpty(result))
            {
                SavePath = result;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(Url)) return;

        IsBusy = true;
        ProgressValue = 0;
        StatusMessage = "Fetching info...";
        Items.Clear();
        _lastItemProgress = 0;
        _seenDestinations.Clear();

        try
        {
            var videos = await _ytDlpService.GetVideoInfoAsync(Url, IsPlaylist);
            foreach (var v in videos)
            {
                Items.Add(new VideoItem
                {
                    Title = v.Title,
                    Index = v.Index,
                    Status = "Pending"
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error fetching info: {ex.Message}";
            IsBusy = false;
            return;
        }

        StatusMessage = "Starting download...";

        // Prime the list by marking the first item as downloading so the UI updates immediately
        var firstPending = Items.FirstOrDefault();
        if (firstPending != null)
        {
            firstPending.Status = "Downloading...";
        }

        var outputProgress = new Progress<string>(msg =>
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                // Clean up status message
                var displayMsg = msg;
                if (displayMsg.StartsWith("[download] ", StringComparison.Ordinal))
                    displayMsg = displayMsg.Substring(11);

                StatusMessage = displayMsg;

                // 1. Explicit Index: "[download] Downloading video 1 of 5" or "Downloading item 1 of 5"
                if ((displayMsg.Contains("Downloading video", StringComparison.Ordinal)
                    || displayMsg.Contains("Downloading item", StringComparison.Ordinal))
                    && displayMsg.Contains(" of ", StringComparison.Ordinal))
                {
                    try
                    {
                        var marker = displayMsg.Contains("Downloading video", StringComparison.Ordinal)
                            ? "Downloading video"
                            : "Downloading item";
                        var parts = displayMsg.Split(new[] { marker }, StringSplitOptions.None);
                        if (parts.Length < 2) return;
                        var part = parts[1].Trim();
                        if (string.IsNullOrEmpty(part)) return;
                        var indexPart = part.Split(' ')[0];
                        if (string.IsNullOrEmpty(indexPart)) return;
                        // Normalize leading zeros to match playlist_index formatting
                        var normalizedIndex = indexPart.TrimStart('0');
                        if (string.IsNullOrEmpty(normalizedIndex)) normalizedIndex = indexPart;

                        var currentItem = Items.FirstOrDefault(i => i.Index == normalizedIndex || i.Index == indexPart);
                        if (currentItem != null)
                        {
                            // Mark all previous as completed
                            foreach (var i in Items)
                            {
                                if (i != currentItem && string.Equals(i.Status, "Downloading...", StringComparison.Ordinal))
                                    i.Status = "Completed";
                            }
                            currentItem.Status = "Downloading...";
                        }
                    }
                    catch when (displayMsg.Contains(" of ", StringComparison.Ordinal))
                    {
                        // Silently ignore parsing errors for "Downloading video X of Y" messages.
                        // These failures only affect UI progress indicators and won't crash the download.
                    }
                }
                // 2. Fallback: "[download] Destination: ..." implies a download started
                else if (displayMsg.Contains("[download] Destination:", StringComparison.Ordinal))
                {
                    // Treat as start of a (next) download, even if the explicit "Downloading video X of Y"
                    // line didn't arrive (some yt-dlp outputs omit it when using custom progress templates).
                    // Mark any currently-downloading item as completed, then advance to the next pending item.
                    var destination = displayMsg.Split(new[] { "[download] Destination:" }, StringSplitOptions.None)
                        .LastOrDefault()?.Trim();

                    if (!string.IsNullOrWhiteSpace(destination))
                    {
                        // Skip duplicate destination logs for the same file
                        if (!_seenDestinations.Add(destination))
                        {
                            return;
                        }
                    }

                    var hasPending = Items.Any(i => string.Equals(i.Status, "Pending", StringComparison.Ordinal));
                    var downloading = Items.FirstOrDefault(i => i.IsDownloading);
                    if (downloading != null && hasPending) downloading.Status = "Completed";

                    if (hasPending)
                    {
                        var next = Items.FirstOrDefault(i => string.Equals(i.Status, "Pending", StringComparison.Ordinal));
                        if (next != null)
                        {
                            next.Status = "Downloading...";
                        }
                    }
                    // If nothing is pending, ignore to avoid double-counting completions.
                }
                // 3. Completion of an item
                else if (displayMsg.Contains("100%"))
                {
                    // Wait for next start to mark completed? 
                    // Or mark now? If we mark now, we might blink if chunks are downloaded.
                    // Let's leave it as Downloading... until next one starts or end of process.
                }
                // 4. "Already downloaded"
                else if (displayMsg.Contains("has already been downloaded"))
                {
                    // Try to advance
                    var downloading = Items.FirstOrDefault(i => i.IsDownloading);
                    if (downloading != null)
                    {
                        downloading.Status = "Completed";
                    }
                    else
                    {
                        // Find first Pending and mark Completed
                        var next = Items.FirstOrDefault(i => string.Equals(i.Status, "Pending", StringComparison.Ordinal));
                        if (next != null) next.Status = "Completed";
                    }
                }
            }
        });
        var valueProgress = new Progress<double>(val =>
        {
            // Detect when yt-dlp resets progress to 0% for the next item (common in playlists).
            // Only consider this a reset if we've seen destination logs for this playlist
            // (small fast videos could legitimately show similar progress patterns).
            if (_lastItemProgress >= 99 && val <= 5 && _seenDestinations.Count > 0 && Items.Any(i => i.Status == "Pending"))
            {
                var downloading = Items.FirstOrDefault(i => i.IsDownloading);
                if (downloading != null) downloading.Status = "Completed";

                var next = Items.FirstOrDefault(i => i.Status == "Pending");
                if (next != null) next.Status = "Downloading...";
            }

            _lastItemProgress = val;
            ProgressValue = val;
        });

        try
        {
            int? maxHeight = null;
            if (!IsAudioOnly && SelectedResolution != "Best")
            {
                var digits = new string(SelectedResolution.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int h)) maxHeight = h;
            }

            string container = IsAudioOnly ? SelectedAudioFormat : SelectedVideoFormat;

            await _ytDlpService.DownloadVideoAsync(Url, SavePath, IsAudioOnly, IsPlaylist, maxHeight, container, outputProgress, valueProgress, System.Threading.CancellationToken.None);

            // Mark last one completed
            foreach (var i in Items)
            {
                if (i.IsDownloading) i.Status = "Completed";
            }
            // Fallback: single video download may not emit "Downloading video 1 of 1" message
            if (Items.Count == 1 && string.Equals(Items[0].Status, "Pending", StringComparison.Ordinal)) Items[0].Status = "Completed";

            StatusMessage = "Download complete!";
            ProgressValue = 100;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDownload() => !IsBusy && !string.IsNullOrWhiteSpace(Url);
}
