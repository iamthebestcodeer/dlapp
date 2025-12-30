# Comprehensive Testing Plan for dlapp

## Executive Summary

This document outlines a complete testing strategy for the dlapp project, a .NET 9.0 Avalonia desktop application for downloading videos using yt-dlp and ffmpeg. The testing plan includes unit tests, integration tests, and optional UI tests, targeting 80%+ code coverage for critical paths.

---

## 1. Testing Strategy Overview

### 1.1 Testing Framework Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Test Framework | xUnit 2.6.x | Primary testing framework |
| Mocking | Moq 4.20.x | Dependency mocking |
| Assertions | FluentAssertions 6.12.x | Readable assertions |
| Coverage | coverlet 6.0.x | Code coverage collection |
| UI Testing | Avalonia 11.3.x | Optional UI automation |

### 1.2 Testing Pyramid

```
                    /\
                   /UI \          ~10% (Optional)
                  /------\
                 /Integration\    ~30% (Medium Priority)
                /--------------\
               /    Unit         \  ~60% (High Priority)
              /------------------\
```

---

## 2. Test Project Structure

```
dlapp/
├── dlapp.csproj                    # Main application
├── dlapp.Tests/                    # NEW: Test project
│   ├── dlapp.Tests.csproj
│   ├── Tests/
│   │   ├── Unit/
│   │   │   ├── ViewModels/
│   │   │   │   ├── MainWindowViewModelTests.cs
│   │   │   │   └── VideoItemTests.cs
│   │   │   └── Services/
│   │   │       └── YtDlpServiceTests.cs
│   │   ├── Integration/
│   │   │   └── Services/
│   │   │       └── YtDlpServiceIntegrationTests.cs
│   │   └── UI/
│   │       └── MainWindowTests.cs          # Optional
│   ├── Helpers/
│   │   ├── MockProcessHelper.cs
│   │   └── TestHelpers.cs
│   └── TestData/
│       ├── YtDlpPlaylistOutput.txt
│       ├── YtDlpSingleVideoOutput.txt
│       └── YtDlpProgressOutputs.txt
```

---

## 3. Dependencies for dlapp.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\dlapp.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

---

## 4. Unit Test Plans

### 4.1 VideoItemTests.cs

**Purpose:** Test the VideoItem model class which represents individual download items.

```csharp
namespace dlapp.Tests.Unit.ViewModels;

public class VideoItemTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var item = new VideoItem();

        // Assert
        item.Title.Should().BeEmpty();
        item.Status.Should().Be("Pending");
        item.Index.Should().BeEmpty();
        item.IsCompleted.Should().BeFalse();
        item.IsDownloading.Should().BeFalse();
    }

    [Theory]
    [InlineData("Pending", false, false)]
    [InlineData("Downloading...", false, true)]
    [InlineData("Completed", true, false)]
    [InlineData("Error", false, false)]
    public void Status_PropertyChanged_UpdatesComputedProperties(
        string status, bool expectedIsCompleted, bool expectedIsDownloading)
    {
        // Arrange
        var item = new VideoItem();

        // Act
        item.Status = status;

        // Assert
        item.IsCompleted.Should().Be(expectedIsCompleted);
        item.IsDownloading.Should().Be(expectedIsDownloading);
    }

    [Fact]
    public void Title_Set_TriggersPropertyChanged()
    {
        // Arrange
        var item = new VideoItem();
        var changedProperties = new List<string>();
        item.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        item.Title = "Test Video";

        // Assert
        changedProperties.Should().Contain(nameof(VideoItem.Title));
    }

    [Fact]
    public void Index_Set_TriggersPropertyChanged()
    {
        // Arrange
        var item = new VideoItem();
        var changedProperties = new List<string>();
        item.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName);

        // Act
        item.Index = "1";

        // Assert
        changedProperties.Should().Contain(nameof(VideoItem.Index));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("01", true)]
    [InlineData("001", true)]
    public void Index_VariousFormats_StoredCorrectly(string index, bool expected)
    {
        // Arrange & Act
        var item = new VideoItem { Index = index };

        // Assert
        item.Index.Should().Be(index);
    }
}
```

### 4.2 MainWindowViewModelTests.cs

**Purpose:** Test the main ViewModel containing all download logic and state management.

#### 4.2.1 Initialization Tests

```csharp
namespace dlapp.Tests.Unit.ViewModels;

public partial class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var vm = new MainWindowViewModel();

        // Assert
        vm.Url.Should().BeEmpty();
        vm.SavePath.Should().NotBeNullOrEmpty();
        vm.IsAudioOnly.Should().BeFalse();
        vm.IsPlaylist.Should().BeFalse();
        vm.SelectedResolution.Should().Be("Best");
        vm.SelectedVideoFormat.Should().Be("mp4");
        vm.SelectedAudioFormat.Should().Be("mp3");
        vm.Items.Should().BeEmpty();
        vm.Resolutions.Should().HaveCount(7);
        vm.VideoFormats.Should().HaveCount(3);
        vm.AudioFormats.Should().HaveCount(3);
    }

    [Fact]
    public void Constructor_SetsSavePathToMyVideos_IfAvailable()
    {
        // Arrange
        var myVideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        // Act
        var vm = new MainWindowViewModel();

        // Assert
        if (!string.IsNullOrEmpty(myVideosPath))
            vm.SavePath.Should().Be(myVideosPath);
    }

    [Fact]
    public void Constructor_SetsSavePathToBaseDirectory_IfMyVideosUnavailable()
    {
        // Arrange
        var expectedPath = AppDomain.CurrentDomain.BaseDirectory;

        // Act
        var vm = new MainWindowViewModel();

        // Assert
        vm.SavePath.Should().Be(expectedPath);
    }
}
```

#### 4.2.2 CanExecute Tests

```csharp
public partial class MainWindowViewModelTests
{
    [Theory]
    [InlineData("", false)]
    ["   ", false)]
    [InlineData("http://example.com", true)]
    public void CanDownload_ReturnsCorrectly(string url, bool expected)
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.Url = url;

        // Act
        var result = vm.CanDownload();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void CanDownload_ReturnsFalse_WhenIsBusy()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.Url = "http://valid.com";
        vm.IsBusy = true;

        // Act
        var result = vm.CanDownload();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanDownload_ReturnsTrue_WhenReady()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.Url = "http://valid.com";
        vm.IsBusy = false;

        // Act
        var result = vm.CanDownload();

        // Assert
        result.Should().BeTrue();
    }
}
```

#### 4.2.3 SelectSavePathCommand Tests

```csharp
public partial class MainWindowViewModelTests
{
    [Fact]
    public async Task SelectSavePathAsync_UpdatesSavePath_WhenDialogReturnsPath()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var expectedPath = @"C:\Downloads";
        string? capturedPath = null;
        vm.ShowOpenFolderDialog = async () =>
        {
            capturedPath = expectedPath;
            return expectedPath;
        };

        // Act
        await vm.SelectSavePathCommand.ExecuteAsync(null);

        // Assert
        vm.SavePath.Should().Be(expectedPath);
    }

    [Fact]
    public async Task SelectSavePathAsync_DoesNothing_WhenDialogReturnsNull()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var originalPath = vm.SavePath;
        vm.ShowOpenFolderDialog = async () => null;

        // Act
        await vm.SelectSavePathCommand.ExecuteAsync(null);

        // Assert
        vm.SavePath.Should().Be(originalPath);
    }

    [Fact]
    public async Task SelectSavePathAsync_DoesNothing_WhenDialogReturnsEmpty()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var originalPath = vm.SavePath;
        vm.ShowOpenFolderDialog = async () => string.Empty;

        // Act
        await vm.SelectSavePathCommand.ExecuteAsync(null);

        // Assert
        vm.SavePath.Should().Be(originalPath);
    }

    [Fact]
    public async Task SelectSavePathAsync_DoesNothing_WhenShowOpenFolderDialogIsNull()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        var originalPath = vm.SavePath;
        vm.ShowOpenFolderDialog = null;

        // Act
        await vm.SelectSavePathCommand.ExecuteAsync(null);

        // Assert
        vm.SavePath.Should().Be(originalPath);
    }
}
```

#### 4.2.4 DownloadCommand - Info Fetching Tests

```csharp
public partial class MainWindowViewModelTests
{
    [Fact]
    public async Task DownloadAsync_ClearsItems_WhenStarting()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.Items.Add(new VideoItem { Title = "Old Item" });
        vm.Url = "http://test.com";
        
        // Mock service to avoid actual execution
        var mockService = new Mock<YtDlpService>(MockBehavior.Strict);
        mockService.Setup(s => s.InitializeAsync(It.IsAny<IProgress<string>>()))
            .Returns(Task.CompletedTask);
        mockService.Setup(s => s.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<(string Index, string Title)> { ("1", "New Video") });
        mockService.Setup(s => s.DownloadVideoAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<string>(),
            It.IsAny<IProgress<string>>(), It.IsAny<IProgress<double>>()))
            .Returns(Task.CompletedTask);

        // Act
        await vm.DownloadAsync();

        // Assert
        vm.Items.Should().NotContain(i => i.Title == "Old Item");
    }

    [Fact]
    public async Task DownloadAsync_AddsVideoItems_WhenInfoFetched()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.Url = "http://test.com";
        var videoInfos = new List<(string Index, string Title)>
        {
            ("1", "Video 1"),
            ("2", "Video 2"),
            ("3", "Video 3")
        };
        
        var mockService = CreateMockService(videoInfos);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        vm.Items.Should().HaveCount(3);
        vm.Items[0].Title.Should().Be("Video 1");
        vm.Items[1].Title.Should().Be("Video 2");
        vm.Items[2].Title.Should().Be("Video 3");
        vm.Items.Should().AllSatisfy(i => i.Status.Should().Be("Pending"));
    }

    [Fact]
    public async Task DownloadAsync_SingleVideo_WhenIsPlaylistFalse()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.Url = "http://test.com";
        vm.IsPlaylist = false;
        var videoInfos = new List<(string Index, string Title)>
        {
            ("1", "Video 1"),
            ("2", "Video 2"),
            ("3", "Video 3")
        };
        
        var mockService = CreateMockService(videoInfos);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        vm.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task DownloadAsync_ShowsErrorMessage_WhenInfoFetchFails()
    {
        // Arrange
        var vm = new MainWindowViewModel();
        vm.Url = "http://test.com";
        
        var mockService = new Mock<YtDlpService>(MockBehavior.Strict);
        mockService.Setup(s => s.InitializeAsync(It.IsAny<IProgress<string>>()))
            .Returns(Task.CompletedTask);
        mockService.Setup(s => s.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Throws(new Exception("Network error"));
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        vm.StatusMessage.Should().Contain("Error");
        vm.IsBusy.Should().BeFalse();
    }
}
```

#### 4.2.5 Progress Parsing Tests

```csharp
public partial class MainWindowViewModelTests
{
    [Fact]
    public async Task DownloadAsync_ParsesProgress_DownloadingVideoXofY()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Url = "http://test.com";
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });
        vm.Items.Add(new VideoItem { Index = "2", Title = "Video 2", Status = "Pending" });
        vm.Items.Add(new VideoItem { Index = "3", Title = "Video 3", Status = "Pending" });

        var progressMessages = new List<string>();
        var outputProgress = new Progress<string>(msg => progressMessages.Add(msg));

        // Act
        await ProcessDownloadProgress(vm, outputProgress, "[download] Downloading video 2 of 3");

        // Assert
        vm.Items[0].Status.Should().Be("Completed");
        vm.Items[1].Status.Should().Be("Downloading...");
        vm.Items[2].Status.Should().Be("Pending");
    }

    [Fact]
    public async Task DownloadAsync_ParsesProgress_DownloadingItemXofY()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });
        vm.Items.Add(new VideoItem { Index = "2", Title = "Video 2", Status = "Pending" });

        var progressMessages = new List<string>();
        var outputProgress = new Progress<string>(msg => progressMessages.Add(msg));

        // Act
        await ProcessDownloadProgress(vm, outputProgress, "[download] Downloading item 2 of 2");

        // Assert
        vm.Items[0].Status.Should().Be("Completed");
        vm.Items[1].Status.Should().Be("Downloading...");
    }

    [Theory]
    [InlineData("01", "1")]
    [InlineData("001", "1")]
    [InlineData("1", "1")]
    public async Task DownloadAsync_NormalizesLeadingZeros(string inputIndex, string expectedIndex)
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = inputIndex, Title = "Video", Status = "Pending" });
        vm.Items.Add(new VideoItem { Index = "2", Title = "Video 2", Status = "Pending" });

        var outputProgress = new Progress<string>(_ => { });

        // Act
        await ProcessDownloadProgress(vm, outputProgress, $"[download] Downloading video {inputIndex} of 2");

        // Assert
        vm.Items.Should().Contain(i => i.Index == expectedIndex && i.Status == "Downloading...");
    }

    [Fact]
    public async Task DownloadAsync_AdvancesOnDestinationLog()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });
        vm.Items.Add(new VideoItem { Index = "2", Title = "Video 2", Status = "Pending" });

        var outputProgress = new Progress<string>(_ => { });

        // Act - First destination
        await ProcessDownloadProgress(vm, outputProgress, "[download] Destination: C:\\video1.mp4");

        // Assert
        vm.Items[0].Status.Should().Be("Completed");
        vm.Items[1].Status.Should().Be("Downloading...");
    }

    [Fact]
    public async Task DownloadAsync_IgnoresDuplicateDestinations()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });
        vm.Items.Add(new VideoItem { Index = "2", Title = "Video 2", Status = "Pending" });

        var statusChanges = new List<string>();
        vm.Items.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<VideoItem>())
                    statusChanges.Add($"{item.Index}:{item.Status}");
        };

        var outputProgress = new Progress<string>(_ => { });

        // Act - Same destination twice
        await ProcessDownloadProgress(vm, outputProgress, "[download] Destination: C:\\video1.mp4");
        await ProcessDownloadProgress(vm, outputProgress, "[download] Destination: C:\\video1.mp4");

        // Assert - Should only advance once
        statusChanges.Should().HaveCount(2); // One for Video 1 completed, one for Video 2 downloading
    }

    [Fact]
    public async Task DownloadAsync_MarksCompleted_WhenAlreadyDownloaded()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });

        var outputProgress = new Progress<string>(_ => { });

        // Act
        await ProcessDownloadProgress(vm, outputProgress, "Video 1 has already been downloaded");

        // Assert
        vm.Items[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task DownloadAsync_DetectsProgressReset()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        var progressUpdates = new List<double>();
        var valueProgress = new Progress<double>(p => progressUpdates.Add(p));
        
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });
        vm.Items.Add(new VideoItem { Index = "2", Title = "Video 2", Status = "Pending" });

        // Act - Simulate progress from 99% to 2%
        valueProgress.Report(99);
        valueProgress.Report(2); // Reset detected

        // Assert
        vm.Items[0].Status.Should().Be("Completed");
        vm.Items[1].Status.Should().Be("Downloading...");
    }

    [Fact]
    public async Task DownloadAsync_UpdatesStatusMessage()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        var outputProgress = new Progress<string>(_ => { });

        // Act
        await ProcessDownloadProgress(vm, outputProgress, "[download] Downloading video 1 of 3");

        // Assert
        vm.StatusMessage.Should().Contain("Downloading video 1 of 3");
    }

    [Fact]
    public async Task DownloadAsync_CleansStatusMessage()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        var outputProgress = new Progress<string>(_ => { });

        // Act
        await ProcessDownloadProgress(vm, outputProgress, "[download] 50.0% of 100.0 MiB at 2.5 MiB/s ETA 00:40");

        // Assert
        vm.StatusMessage.Should().Be("50.0% of 100.0 MiB at 2.5 MiB/s ETA 00:40");
    }
}
```

#### 4.2.6 Download Completion Tests

```csharp
public partial class MainWindowViewModelTests
{
    [Fact]
    public async Task DownloadAsync_MarksLastItemCompleted_WhenFinished()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });

        // Act
        await CompleteDownload(vm);

        // Assert
        vm.Items[0].Status.Should().Be("Completed");
        vm.StatusMessage.Should().Be("Download complete!");
        vm.ProgressValue.Should().Be(100);
        vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadAsync_SingleVideo_MarksCompletedEvenWithoutDownloadingStatus()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Pending" });

        // Act
        await CompleteDownload(vm);

        // Assert
        vm.Items[0].Status.Should().Be("Completed");
    }

    [Fact]
    public async Task DownloadAsync_AllItemsCompleted_WhenPlaylistFinished()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.Items.Add(new VideoItem { Index = "1", Title = "Video 1", Status = "Downloading..." });
        vm.Items.Add(new VideoItem { Index = "2", Title = "Video 2", Status = "Completed" });
        vm.Items.Add(new VideoItem { Index = "3", Title = "Video 3", Status = "Completed" });

        // Act
        await CompleteDownload(vm);

        // Assert
        vm.Items.Should().AllSatisfy(i => i.Status.Should().Be("Completed"));
    }
}
```

#### 4.2.7 Argument Construction Tests

```csharp
public partial class MainWindowViewModelTests
{
    [Fact]
    public async Task DownloadAsync_ConstructsAudioOnlyArgs_Correctly()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.IsAudioOnly = true;
        vm.SelectedAudioFormat = "mp3";
        var capturedArgs = new List<string>();
        var mockService = CreateMockServiceWithArgCapture(capturedArgs);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("-x") && arg.Contains("--audio-format mp3"));
    }

    [Theory]
    [InlineData("1080p", 1080)]
    [InlineData("720p", 720)]
    [InlineData("480p", 480)]
    [InlineData("360p", 360)]
    public async Task DownloadAsync_ConstructsArgs_WithResolution(string resolution, int expectedHeight)
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.SelectedResolution = resolution;
        var capturedArgs = new List<string>();
        var mockService = CreateMockServiceWithArgCapture(capturedArgs);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains($"[height<={expectedHeight}]"));
    }

    [Fact]
    public async Task DownloadAsync_ConstructsArgs_BestResolution_NoHeightLimit()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.SelectedResolution = "Best";
        var capturedArgs = new List<string>();
        var mockService = CreateMockServiceWithArgCapture(capturedArgs);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        capturedArgs.Should().Contain(arg => !arg.Contains("[height<="));
    }

    [Fact]
    public async Task DownloadAsync_ConstructsPlaylistArgs_YesPlaylist()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.IsPlaylist = true;
        var capturedArgs = new List<string>();
        var mockService = CreateMockServiceWithArgCapture(capturedArgs);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--yes-playlist"));
    }

    [Fact]
    public async Task DownloadAsync_ConstructsSingleVideoArgs_NoPlaylist()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.IsPlaylist = false;
        var capturedArgs = new List<string>();
        var mockService = CreateMockServiceWithArgCapture(capturedArgs);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--no-playlist"));
    }

    [Theory]
    [InlineData("mp4")]
    [InlineData("mkv")]
    [InlineData("webm")]
    public async Task DownloadAsync_ConstructsMergeArgs_VideoFormat(string format)
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        vm.SelectedVideoFormat = format;
        var capturedArgs = new List<string>();
        var mockService = CreateMockServiceWithArgCapture(capturedArgs);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains($"--merge-output-format {format}"));
    }

    [Fact]
    public async Task DownloadAsync_IncludesFfmpegLocation()
    {
        // Arrange
        var vm = CreateViewModelWithMockService();
        var capturedArgs = new List<string>();
        var mockService = CreateMockServiceWithArgCapture(capturedArgs);
        ReplaceService(vm, mockService.Object);

        // Act
        await vm.DownloadAsync();

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--ffmpeg-location"));
    }
}
```

### 4.3 YtDlpServiceTests.cs

**Purpose:** Test the service that manages yt-dlp and ffmpeg binaries.

#### 4.3.1 Initialization Tests

```csharp
namespace dlapp.Tests.Unit.Services;

public class YtDlpServiceTests
{
    private readonly string _testBaseDir;

    public YtDlpServiceTests()
    {
        _testBaseDir = Path.Combine(Path.GetTempPath(), $"dlapp_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testBaseDir);
    }

    [Fact]
    public void Constructor_SetsCorrectPaths()
    {
        // Arrange & Act
        var service = new YtDlpService();

        // Assert
        service.GetType().GetField("_ytDlpPath", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(service)!.ToString()!.Should().EndWith("yt-dlp.exe");
        service.GetType().GetField("_ffmpegPath", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(service)!.ToString()!.Should().EndWith("ffmpeg.exe");
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public void IsReady_ReturnsCorrectly(bool ytDlpExists, bool ffmpegExists, bool expected)
    {
        // Arrange
        var service = CreateServiceWithMockedFiles(ytDlpExists, ffmpegExists);

        // Act
        var result = service.IsReady;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task InitializeAsync_DownloadsYtDlp_WhenMissing()
    {
        // Arrange
        var service = CreateService();
        var downloadCalled = false;
        var mockHttp = CreateMockHttpClient();

        // Act
        await service.InitializeAsync(new Progress<string>(_ => {}));

        // Assert - Verify yt-dlp was downloaded
        File.Exists(Path.Combine(_testBaseDir, "yt-dlp.exe")).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_ChecksUpdates_WhenYtDlpExists()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testBaseDir, "yt-dlp.exe"), "fake yt-dlp");
        var service = new YtDlpService();
        var updateCalled = false;

        // Mock UpdateYtDlpAsync to track call
        var method = typeof(YtDlpService).GetMethod("UpdateYtDlpAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        var originalMethod = method!.CreateDelegate<Func<Task>>(service);
        await method!.InvokeAsync(service);

        // Assert
        // Verify update check was triggered
    }

    [Fact]
    public async Task InitializeAsync_DownloadsFfmpeg_WhenMissing()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testBaseDir, "yt-dlp.exe"), "fake yt-dlp");
        var service = new YtDlpService();

        // Act
        await service.InitializeAsync(new Progress<string>(_ => {}));

        // Assert
        File.Exists(Path.Combine(_testBaseDir, "ffmpeg.exe")).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_SkipsFfmpegDownload_WhenExists()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testBaseDir, "yt-dlp.exe"), "fake yt-dlp");
        File.WriteAllText(Path.Combine(_testBaseDir, "ffmpeg.exe"), "fake ffmpeg");
        var service = new YtDlpService();

        // Act
        await service.InitializeAsync(new Progress<string>(_ => {}));

        // Assert - No zip file should remain
        Directory.GetFiles(_testBaseDir, "*.zip").Should().BeEmpty();
    }
}
```

#### 4.3.2 GetVideoInfoAsync Tests

```csharp
public class YtDlpServiceTests
{
    [Fact]
    public async Task GetVideoInfoAsync_Throws_WhenNotReady()
    {
        // Arrange
        var service = CreateServiceWithMockedFiles(false, false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetVideoInfoAsync("http://test.com", false));
    }

    [Fact]
    public async Task GetVideoInfoAsync_ParsesPlaylistOutput_Correctly()
    {
        // Arrange
        var service = CreateService();
        var mockOutput = "1|First Video\n2|Second Video\n3|Third Video";
        var processResult = CreateMockProcess(mockOutput);
        
        // Act
        var result = await service.GetVideoInfoAsync("http://test.com", true);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be(("1", "First Video"));
        result[1].Should().Be(("2", "Second Video"));
        result[2].Should().Be(("3", "Third Video"));
    }

    [Fact]
    public async Task GetVideoInfoAsync_ParsesSingleVideoOutput_Correctly()
    {
        // Arrange
        var service = CreateService();
        var mockOutput = "My Awesome Video";
        var processResult = CreateMockProcess(mockOutput);

        // Act
        var result = await service.GetVideoInfoAsync("http://test.com", false);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(("1", "My Awesome Video"));
    }

    [Fact]
    public async Task GetVideoInfoAsync_SkipsLogLines()
    {
        // Arrange
        var service = CreateService();
        var mockOutput = "[info] Some info\n1|Video\n[warning] Some warning\n2|Video 2";
        var processResult = CreateMockProcess(mockOutput);

        // Act
        var result = await service.GetVideoInfoAsync("http://test.com", true);

        // Assert
        result.Should().HaveCount(2);
        result[0].Index.Should().Be("1");
        result[1].Index.Should().Be("2");
    }

    [Fact]
    public async Task GetVideoInfoAsync_HandlesMissingIndex_WithCounter()
    {
        // Arrange
        var service = CreateService();
        var mockOutput = "|Video Without Index";
        var processResult = CreateMockProcess(mockOutput);

        // Act
        var result = await service.GetVideoInfoAsync("http://test.com", true);

        // Assert
        result.Should().HaveCount(1);
        result[0].Index.Should().Be("1");
        result[0].Title.Should().Be("Video Without Index");
    }

    [Fact]
    public async Task GetVideoInfoAsync_NormalizesIndex_KeepsLeadingZeros()
    {
        // Arrange
        var service = CreateService();
        var mockOutput = "001|First Video";
        var processResult = CreateMockProcess(mockOutput);

        // Act
        var result = await service.GetVideoInfoAsync("http://test.com", true);

        // Assert
        result[0].Index.Should().Be("001");
    }

    [Fact]
    public async Task GetVideoInfoAsync_SingleVideo_DefensiveTrim()
    {
        // Arrange
        var service = CreateService();
        var mockOutput = "First Video\nSecond Video\nThird Video";
        var processResult = CreateMockProcess(mockOutput);

        // Act
        var result = await service.GetVideoInfoAsync("http://test.com", false);

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("First Video");
    }

    [Fact]
    public async Task GetVideoInfoAsync_RespectsIsPlaylist_True()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();
        var processResult = CreateMockProcessWithArgCapture(capturedArgs);

        // Act
        await service.GetVideoInfoAsync("http://test.com", true);

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--flat-playlist"));
    }

    [Fact]
    public async Task GetVideoInfoAsync_RespectsIsPlaylist_False()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();
        var processResult = CreateMockProcessWithArgCapture(capturedArgs);

        // Act
        await service.GetVideoInfoAsync("http://test.com", false);

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--no-playlist"));
        capturedArgs.Should().Contain(arg => arg.Contains("--playlist-items 1"));
        capturedArgs.Should().Contain(arg => arg.Contains("--max-downloads 1"));
    }
}
```

#### 4.3.3 DownloadVideoAsync Tests

```csharp
public class YtDlpServiceTests
{
    [Fact]
    public async Task DownloadVideoAsync_Throws_WhenNotReady()
    {
        // Arrange
        var service = CreateServiceWithMockedFiles(false, false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DownloadVideoAsync("http://test.com", "C:\\", false, false, null, "mp4",
                new Progress<string>(_ => {}), new Progress<double>(_ => {})));
    }

    [Fact]
    public async Task DownloadVideoAsync_ConstructsAudioArgs_Correctly()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();
        var progressMessages = new List<string>();
        var progressValues = new List<double>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", true, false, null, "mp3",
            new Progress<string>(m => progressMessages.Add(m)),
            new Progress<double>(v => progressValues.Add(v)));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("-x"));
        capturedArgs.Should().Contain(arg => arg.Contains("--audio-format mp3"));
    }

    [Theory]
    [InlineData(2160)]
    [InlineData(1440)]
    [InlineData(1080)]
    [InlineData(720)]
    [InlineData(480)]
    [InlineData(360)]
    public async Task DownloadVideoAsync_ConstructsVideoArgs_WithResolution(int height)
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, height, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains($"[height<={height}]"));
    }

    [Fact]
    public async Task DownloadVideoAsync_ConstructsVideoArgs_Best_NoHeightLimit()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, null, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("bestvideo[height<=") == false);
        capturedArgs.Should().Contain(arg => arg.Contains("best[height<=") == false);
    }

    [Theory]
    [InlineData("mp4")]
    [InlineData("mkv")]
    [InlineData("webm")]
    public async Task DownloadVideoAsync_ConstructsMergeArgs(string container)
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, 1080, container,
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains($"--merge-output-format {container}"));
    }

    [Fact]
    public async Task DownloadVideoAsync_ConstructsPlaylistArgs_YesPlaylist()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, true, null, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--yes-playlist"));
    }

    [Fact]
    public async Task DownloadVideoAsync_ConstructsPlaylistArgs_NoPlaylist()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, null, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--no-playlist"));
    }

    [Fact]
    public async Task DownloadVideoAsync_ConstructsOutputTemplate_Playlist()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, true, null, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("%(playlist_title)s"));
        capturedArgs.Should().Contain(arg => arg.Contains("%(playlist_index)s"));
    }

    [Fact]
    public async Task DownloadVideoAsync_ConstructsOutputTemplate_Single()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, null, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => !arg.Contains("%(playlist_title)s"));
        capturedArgs.Should().Contain(arg => arg.Contains("%(title)s.%(ext)s"));
    }

    [Fact]
    public async Task DownloadVideoAsync_ReportsProgress_Percentage()
    {
        // Arrange
        var service = CreateService();
        var progressValues = new List<double>();
        var progress = new Progress<double>(v => progressValues.Add(v));

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, null, "mp4",
            new Progress<string>(_ => {}), progress);

        // Assert
        progressValues.Should().Contain(50.5);
        progressValues.Should().Contain(100.0);
    }

    [Fact]
    public async Task DownloadVideoAsync_ReportsOutputMessages()
    {
        // Arrange
        var service = CreateService();
        var outputMessages = new List<string>();
        var outputProgress = new Progress<string>(m => outputMessages.Add(m));

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, null, "mp4",
            outputProgress, new Progress<double>(_ => {}));

        // Assert
        outputMessages.Should().Contain(m => m.Contains("Downloading"));
        outputMessages.Should().Contain(m => m.Contains("Destination"));
    }

    [Fact]
    public async Task DownloadVideoAsync_IncludesFfmpegLocation()
    {
        // Arrange
        var service = CreateService();
        var capturedArgs = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, null, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedArgs.Should().Contain(arg => arg.Contains("--ffmpeg-location"));
    }

    [Fact]
    public async Task DownloadVideoAsync_SetsWorkingDirectory()
    {
        // Arrange
        var service = CreateService();
        var capturedWorkingDir = new List<string>();

        // Act
        await service.DownloadVideoAsync(
            "http://test.com", "C:\\Downloads", false, false, null, "mp4",
            new Progress<string>(_ => {}), new Progress<double>(_ => {}));

        // Assert
        capturedWorkingDir.Should().Contain("C:\\Downloads");
    }
}
```

---

## 5. Integration Test Plans

### 5.1 YtDlpServiceIntegrationTests.cs

**Purpose:** Test service behavior with actual or realistic process execution.

```csharp
namespace dlapp.Tests.Integration.Services;

public class YtDlpServiceIntegrationTests : IDisposable
{
    private readonly string _testDir;
    private readonly YtDlpService _service;

    public YtDlpServiceIntegrationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"dlapp_integration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _service = new YtDlpService();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDir, true);
        }
        catch { }
    }

    [Fact(Skip = "Requires network and yt-dlp binary - run manually")]
    public async Task InitializeAsync_DownloadsRealYtDlp()
    {
        // Arrange
        var statusMessages = new List<string>();
        var progress = new Progress<string>(m => statusMessages.Add(m));

        // Act
        await _service.InitializeAsync(progress);

        // Assert
        _service.IsReady.Should().BeTrue();
        statusMessages.Should().Contain(m => m.Contains("yt-dlp"));
    }

    [Fact(Skip = "Requires network and yt-dlp binary - run manually")]
    public async Task GetVideoInfoAsync_FetchesRealVideoInfo()
    {
        // Arrange
        await _service.InitializeAsync(new Progress<string>(_ => {}));

        // Act
        var result = await _service.GetVideoInfoAsync(
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ", false);

        // Assert
        result.Should().NotBeEmpty();
        result[0].Title.Should().NotBeEmpty();
    }

    [Fact(Skip = "Requires network and yt-dlp binary - run manually")]
    public async Task DownloadVideoAsync_DownloadsRealVideo()
    {
        // Arrange
        await _service.InitializeAsync(new Progress<string>(_ => {}));
        var outputMessages = new List<string>();
        var progressValues = new List<double>();
        var outputProgress = new Progress<string>(m => outputMessages.Add(m));
        var valueProgress = new Progress<double>(v => progressValues.Add(v));

        // Act
        await _service.DownloadVideoAsync(
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            _testDir, false, false, 720, "mp4",
            outputProgress, valueProgress);

        // Assert
        var downloadedFile = Directory.GetFiles(_testDir, "*.mp4").FirstOrDefault();
        downloadedFile.Should().NotBeNull();
    }
}
```

---

## 6. UI Test Plans (Optional)

### 6.1 MainWindowTests.cs

**Purpose:** Test UI binding and interaction patterns.

```csharp
namespace dlapp.Tests.UI;

public class MainWindowTests
{
    [Fact]
    public void MainWindow_Initializes_WithCorrectDataContext()
    {
        // Arrange & Act
        var window = new MainWindow();
        var vm = window.DataContext as MainWindowViewModel;

        // Assert
        vm.Should().NotBeNull();
    }

    [Fact]
    public void MainWindow_SetsUpShowOpenFolderDialog()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        var hasDialog = vm.ShowOpenFolderDialog != null;

        // Assert
        hasDialog.Should().BeTrue();
    }

    [Fact]
    public void MainWindow_BindsUrlTextBox()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        vm.Url = "https://test.com";

        // Assert - URL is stored in ViewModel
        vm.Url.Should().Be("https://test.com");
    }

    [Fact]
    public void MainWindow_BindsSavePathTextBox()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        vm.SavePath = "C:\\Test";

        // Assert
        vm.SavePath.Should().Be("C:\\Test");
    }

    [Fact]
    public void MainWindow_BindsDownloadButton_IsEnabled()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        vm.Url = "https://test.com";
        vm.IsBusy = false;

        // Assert
        vm.CanDownload().Should().BeTrue();
    }

    [Fact]
    public void MainWindow_BindsDownloadButton_IsDisabled_WhenBusy()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        vm.IsBusy = true;

        // Assert
        vm.CanDownload().Should().BeFalse();
    }

    [Fact]
    public void MainWindow_BindsItemsListBox()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        vm.Items.Add(new VideoItem { Title = "Test Video" });

        // Assert
        vm.Items.Should().HaveCount(1);
    }

    [Fact]
    public void MainWindow_BindsProgressBar()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        vm.ProgressValue = 50;

        // Assert
        vm.ProgressValue.Should().Be(50);
    }

    [Fact]
    public void MainWindow_BindsStatusMessage()
    {
        // Arrange
        var window = new MainWindow();
        var vm = (MainWindowViewModel)window.DataContext!;

        // Act
        vm.StatusMessage = "Test status";

        // Assert
        vm.StatusMessage.Should().Be("Test status");
    }
}
```

---

## 7. Test Helpers

### 7.1 MockProcessHelper.cs

```csharp
using Moq;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace dlapp.Tests.Helpers;

public static class MockProcessHelper
{
    public static Process CreateMockProcess(string output = "", int exitCode = 0)
    {
        var mockProcess = new Mock<Process>();
        var mockStartInfo = new Mock<ProcessStartInfo>();
        var mockStandardOutput = new Mock<StreamReader>();
        var mockStandardError = new Mock<StreamReader>();

        var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(output));
        var errorStream = new MemoryStream();

        mockStandardOutput.Setup(s => s.ReadLineAsync())
            .Returns(async () =>
            {
                var buffer = new byte[1024];
                var bytesRead = await outputStream.ReadAsync(buffer);
                if (bytesRead == 0) return null;
                return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            });
        mockStandardOutput.Setup(s => s.EndOfStream).Returns(outputStream.Position >= outputStream.Length);

        mockProcess.SetupGet(p => p.StartInfo).Returns(mockStartInfo.Object);
        mockProcess.SetupGet(p => p.StandardOutput).Returns(mockStandardOutput.Object);
        mockProcess.SetupGet(p => p.StandardError).Returns(mockStandardError.Object);
        mockProcess.Setup(p => p.Start()).Returns(true);
        mockProcess.Setup(p => p.WaitForExitAsync()).Returns(Task.CompletedTask);
        mockProcess.SetupGet(p => p.ExitCode).Returns(exitCode);

        return mockProcess.Object;
    }

    public static Mock<Process> CreateMockProcessWithArgCapture(List<string> capturedArgs)
    {
        var mockProcess = new Mock<Process>();
        var mockStartInfo = new Mock<ProcessStartInfo>();

        mockStartInfo.SetupGet(s => s.Arguments)
            .Returns(() => capturedArgs.LastOrDefault() ?? string.Empty);

        mockStartInfo.SetupSet(s => s.Arguments = It.IsAny<string>())
            .Callback<string>(arg => capturedArgs.Add(arg));

        mockProcess.SetupGet(p => p.StartInfo).Returns(mockStartInfo.Object);

        return mockProcess;
    }
}
```

### 7.2 TestHelpers.cs

```csharp
using Moq;
using dlapp.Services;
using dlapp.ViewModels;

namespace dlapp.Tests.Helpers;

public static class TestHelpers
{
    public static YtDlpService CreateMockYtDlpService(
        List<(string Index, string Title)>? videoInfos = null,
        Action<string, string, bool, bool, int?, string, IProgress<string>, IProgress<double>>? downloadCallback = null)
    {
        var mockService = new Mock<YtDlpService>(MockBehavior.Strict);
        
        mockService.Setup(s => s.InitializeAsync(It.IsAny<IProgress<string>>()))
            .Returns(Task.CompletedTask);

        mockService.Setup(s => s.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(videoInfos ?? new List<(string, string)> { ("1", "Test Video") });

        if (downloadCallback != null)
        {
            mockService.Setup(s => s.DownloadVideoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int?>(), It.IsAny<string>(),
                It.IsAny<IProgress<string>>(), It.IsAny<IProgress<double>>()))
                .Callback<string, string, bool, bool, int?, string, IProgress<string>, IProgress<double>>(
                    (url, path, audioOnly, isPlaylist, height, container, outputProgress, valueProgress) =>
                    {
                        downloadCallback(url, path, audioOnly, isPlaylist, height, container, outputProgress, valueProgress);
                    })
                .Returns(Task.CompletedTask);
        }
        else
        {
            mockService.Setup(s => s.DownloadVideoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int?>(), It.IsAny<string>(),
                It.IsAny<IProgress<string>>(), It.IsAny<IProgress<double>>()))
                .Returns(Task.CompletedTask);
        }

        return mockService.Object;
    }

    public static MainWindowViewModel CreateViewModelWithMockService(
        YtDlpService? mockService = null)
    {
        var service = mockService ?? CreateMockYtDlpService();
        
        // Use reflection to set the private _ytDlpService field
        var vm = new MainWindowViewModel();
        var field = typeof(MainWindowViewModel).GetField("_ytDlpService", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(vm, service);
        
        return vm;
    }

    public static async Task ProcessDownloadProgress(MainWindowViewModel vm, IProgress<string> progress, string message)
    {
        progress.Report(message);
        await Task.Delay(10); // Allow async handlers to complete
    }

    public static async Task CompleteDownload(MainWindowViewModel vm)
    {
        var progress = new Progress<double>(v => { });
        progress.Report(100);
        await Task.Delay(10);
    }
}
```

---

## 8. Code Coverage Targets

| Component | Target Coverage | Notes |
|-----------|-----------------|-------|
| ViewModels | 90%+ | Critical UI logic, heavily tested |
| Services | 75%+ | External dependencies harder to test |
| Models | 95%+ | Simple data structures |
| Views | 0-10% | UI testing is optional |

**Critical Path Coverage: 100%**
- Download command execution
- Progress parsing logic
- Error handling paths

---

## 9. CI/CD Integration

### 9.1 Updated GitHub Actions Workflow

```yaml
name: Build & Test

on:
  push:
    branches: ["main"]
  pull_request:
    branches: ["main"]
  workflow_dispatch:

permissions:
  contents: write

jobs:
  build:
    name: Build & Test ${{ matrix.rid }}
    runs-on: ubuntu-latest
    strategy:
      matrix:
        include:
          - rid: win-x64
            suffix: .exe
          - rid: win-arm64
            suffix: .exe
          - rid: linux-x64
            suffix: ''
          - rid: linux-arm64
            suffix: ''

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x

    - name: Restore
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore -c Release

    - name: Test
      run: dotnet test --no-build -c Release --verbosity normal --collect:"XPlat Code Coverage" --formatter trx

    - name: Upload Test Results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.rid }}
        path: dlapp.Tests/TestResults/
        retention-days: 7

    - name: Publish
      run: |
        dotnet publish dlapp.csproj -c Release -r ${{ matrix.rid }} --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o release/${{ matrix.rid }}

    - name: Rename and Prepare Artifact
      run: |
        cd release/${{ matrix.rid }}
        mv dlapp${{ matrix.suffix }} ../../dlapp-${{ matrix.rid }}${{ matrix.suffix }}

    - name: Upload artifact
      uses: actions/upload-artifact@v4
      with:
        name: dlapp-${{ matrix.rid }}
        path: dlapp-${{ matrix.rid }}${{ matrix.suffix }}
        if-no-files-found: error

  test:
    name: Test & Coverage
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 9.0.x

    - name: Restore
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore -c Debug

    - name: Test
      run: dotnet test dlapp.Tests/dlapp.Tests.csproj --no-build -c Debug --verbosity normal --collect:"XPlat Code Coverage" --logger trx --results-directory TestResults

    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        files: ./TestResults/**/*.xml
        fail_ci_if_error: false

  release:
    name: Create Release
    runs-on: ubuntu-latest
    needs: build
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    steps:
    - uses: actions/download-artifact@v4
      with:
        pattern: dlapp-*
        path: artifacts
        merge-multiple: true

    - name: Display structure
      run: ls -R artifacts

    - name: Delete existing release
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      run: gh release delete latest --cleanup-tag -y || true

    - name: Create Release
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      run: |
        gh release create latest artifacts/* \
          --title "Latest Build" \
          --notes "Latest build artifacts from commit ${{ github.sha }}" \
          --target ${{ github.sha }} \
          --draft=false \
          --prerelease=false
```

---

## 10. Implementation Priority

### Phase 1 - Foundation (Week 1)
1. Create `dlapp.Tests` project with dependencies
2. Set up test helpers and mock utilities
3. Write `VideoItemTests` (simple, quick wins)
4. Write `ViewModelBase` tests (if needed)

### Phase 2 - ViewModel Tests (Week 1-2)
5. Write `MainWindowViewModelTests` - initialization
6. Write `MainWindowViewModelTests` - CanDownload logic
7. Write `MainWindowViewModelTests` - SelectSavePathCommand
8. Write `MainWindowViewModelTests` - DownloadAsync info fetching
9. Write `MainWindowViewModelTests` - progress parsing (critical)
10. Write `MainWindowViewModelTests` - completion scenarios
11. Write `MainWindowViewModelTests` - argument construction

### Phase 3 - Service Tests (Week 2)
12. Write `YtDlpServiceTests` - initialization
13. Write `YtDlpServiceTests` - GetVideoInfoAsync
14. Write `YtDlpServiceTests` - DownloadVideoAsync arg construction
15. Write `YtDlpServiceTests` - progress parsing callbacks

### Phase 4 - Integration Tests (Week 3)
16. Write `YtDlpServiceIntegrationTests`

### Phase 5 - UI Tests (Week 3 - Optional)
17. Write `MainWindowTests` if needed
18. Consider Avalonia UI test scenarios

### Phase 6 - CI/CD (Week 3)
19. Add test job to GitHub Actions
20. Set up code coverage reporting

---

## 11. Testing Best Practices

### 11.1 For ViewModels
- Mock all external dependencies (YtDlpService, dialogs)
- Test command CanExecute logic
- Test property change notifications
- Test async command execution with Task completion
- Verify IsBusy state transitions
- Use `[Theory]` with `[InlineData]` for parameterized tests

### 11.2 For Services
- Mock Process.Start for external command execution
- Test argument construction
- Test stdout/stderr parsing
- Test error handling
- Test file I/O operations (File.Exists, File.Delete)

### 11.3 General Guidelines
- Use Arrange-Act-Assert pattern
- Each test should test ONE thing
- Use descriptive test names
- Avoid implementation details in test names
- Use FluentAssertions for readability
- Keep tests fast (avoid real process execution)
- Test edge cases and error conditions
- Isolate tests from each other
- Use setup/teardown for common test state

---

## 12. Known Challenges & Solutions

### Challenge 1: YtDlpService creates Process directly
**Solution:** Create an `IProcessService` interface and inject it into YtDlpService, then mock for tests.

```csharp
public interface IProcessService
{
    Process Start(ProcessStartInfo info);
}

public class YtDlpService
{
    private readonly IProcessService _processService;
    
    public YtDlpService(IProcessService? processService = null)
    {
        _processService = processService ?? new ProcessService();
    }
}
```

### Challenge 2: MainWindowViewModel constructs YtDlpService in constructor
**Solution:** Add `IYtDlpService` dependency injection to ViewModel.

```csharp
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IYtDlpService _ytDlpService;
    
    public MainWindowViewModel(IYtDlpService? ytDlpService = null)
    {
        _ytDlpService = ytDlpService ?? new YtDlpService();
    }
}
```

### Challenge 3: Async initialization in constructor
**Solution:** Test initialization completion state separately.

```csharp
[Fact]
public async Task InitializeAsync_SetsIsBusy_ToFalse_WhenComplete()
{
    var vm = new MainWindowViewModel();
    await Task.Delay(100); // Wait for initialization
    vm.IsBusy.Should().BeFalse();
}
```

### Challenge 4: Progress parsing with complex string patterns
**Solution:** Create dedicated test data files with sample outputs.

### Challenge 5: File I/O in tests
**Solution:** Use System.IO.Abstractions or mock File static methods with Moq.

---

## 13. Running Tests

### Run All Tests
```bash
dotnet test
```

### Run with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~MainWindowViewModelTests"
```

### Run Tests in Parallel
```bash
dotnet test --parallel
```

### Generate Coverage Report
```bash
dotnet reportgenerator -reports:TestResults/**/*.xml -targetdir:coverage-report
```

---

## 14. Appendix: Test Data Files

### 14.1 YtDlpPlaylistOutput.txt
```
1|First Video Title
2|Second Video Title
3|Third Video Title
```

### 14.2 YtDlpSingleVideoOutput.txt
```
My Awesome Video Title
```

### 14.3 YtDlpProgressOutputs.txt
```
[download] Downloading video 1 of 5
[download] Downloading video 2 of 5
[download] Downloading item 3 of 5
[download] Destination: C:\Downloads\video.mp4
50.5%
100.0%
```

---

## 15. References

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Avalonia Testing Guide](https://docs.avaloniaui.net/docs/guides/development-guides/testing)
- [coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [.NET Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)

---

*Document Version: 1.0*  
*Created: December 30, 2025*  
*Project: dlapp - YouTube Downloader Application*
