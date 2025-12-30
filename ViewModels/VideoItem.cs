using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace dlapp.ViewModels;

public partial class VideoItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCompleted))]
    [NotifyPropertyChangedFor(nameof(IsDownloading))]
    private string _status = "Pending";

    [ObservableProperty]
    private string _index = string.Empty;

    public bool IsCompleted => string.Equals(Status, "Completed", StringComparison.Ordinal);
    public bool IsDownloading => string.Equals(Status, "Downloading...", StringComparison.Ordinal);
}
