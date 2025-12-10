using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using dlapp.Services;
using System;
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
            await _ytDlpService.DownloadVideoAsync(Url, SavePath, IsAudioOnly, outputProgress, valueProgress);
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
