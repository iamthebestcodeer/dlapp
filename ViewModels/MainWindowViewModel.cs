using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dlapp.Services;
using System;
using System.Collections.Generic;
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
        StatusMessage = "Starting download...";

        var outputProgress = new Progress<string>(msg =>
        {
            // Keep status message relevant, ignore verbose logs if needed
            if (!string.IsNullOrWhiteSpace(msg)) StatusMessage = msg;
        });
        var valueProgress = new Progress<double>(val => ProgressValue = val);

        try
        {
            int? maxHeight = null;
            if (!IsAudioOnly && SelectedResolution != "Best")
            {
                var digits = new string(SelectedResolution.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int h)) maxHeight = h;
            }

            string container = IsAudioOnly ? SelectedAudioFormat : SelectedVideoFormat;

            await _ytDlpService.DownloadVideoAsync(Url, SavePath, IsAudioOnly, maxHeight, container, outputProgress, valueProgress);
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
