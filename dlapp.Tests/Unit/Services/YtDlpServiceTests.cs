using System.Reflection;
using dlapp.Services;

namespace dlapp.Tests.Unit.Services;

public class YtDlpServiceTests : IDisposable
{
    private readonly string _testDir;

    public YtDlpServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"dlapp_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDir, true);
        }
        catch
        {
        }
    }

    [Fact]
    public void Constructor_SetsCorrectPaths()
    {
        var service = new YtDlpService();

        var ytDlpPathField = typeof(YtDlpService).GetField("_ytDlpPath", BindingFlags.NonPublic | BindingFlags.Instance);
        var ffmpegPathField = typeof(YtDlpService).GetField("_ffmpegPath", BindingFlags.NonPublic | BindingFlags.Instance);

        ytDlpPathField.Should().NotBeNull();
        ffmpegPathField.Should().NotBeNull();

        var ytDlpPath = ytDlpPathField!.GetValue(service)!.ToString();
        var ffmpegPath = ffmpegPathField!.GetValue(service)!.ToString();

        ytDlpPath.Should().EndWith("yt-dlp.exe");
        ffmpegPath.Should().EndWith("ffmpeg.exe");
    }

    [Fact]
    public void Constructor_PathsUseAppData()
    {
        var service = new YtDlpService();

        var ytDlpPathField = typeof(YtDlpService).GetField("_ytDlpPath", BindingFlags.NonPublic | BindingFlags.Instance);
        var ffmpegPathField = typeof(YtDlpService).GetField("_ffmpegPath", BindingFlags.NonPublic | BindingFlags.Instance);

        var ytDlpPath = ytDlpPathField!.GetValue(service)!.ToString();
        var ffmpegPath = ffmpegPathField!.GetValue(service)!.ToString();

        var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        ytDlpPath.Should().StartWith(appDataRoot);
        ffmpegPath.Should().StartWith(appDataRoot);

        ytDlpPath.Should().Contain("dlapp");
        ffmpegPath.Should().Contain("dlapp");
    }

    [Fact]
    public void AppDataRoot_StaticProperty_ReturnsCorrectPath()
    {
        var appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var expected = Path.Combine(appDataRoot, "dlapp");

        var appDataRootMethod = typeof(YtDlpService).GetProperty("AppDataRoot", BindingFlags.NonPublic | BindingFlags.Static);
        var actual = appDataRootMethod!.GetValue(null)!.ToString();

        actual.Should().Be(expected);
    }

    [Fact]
    public void IsReady_True_WhenBothFilesExist()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");

        File.WriteAllText(ytDlpPath, "fake yt-dlp");
        File.WriteAllText(ffmpegPath, "fake ffmpeg");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);

        service.IsReady.Should().BeTrue();
    }

    [Fact]
    public void IsReady_False_WhenYtDlpMissing()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");

        File.WriteAllText(ffmpegPath, "fake ffmpeg");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);

        service.IsReady.Should().BeFalse();
    }

    [Fact]
    public void IsReady_False_WhenFfmpegMissing()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");

        File.WriteAllText(ytDlpPath, "fake yt-dlp");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);

        service.IsReady.Should().BeFalse();
    }

    [Fact]
    public void IsReady_False_WhenBothMissing()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);

        service.IsReady.Should().BeFalse();
    }

    [Fact]
    public void GetVideoInfoAsync_Throws_WhenNotReady()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);

        Func<Task> act = () => service.GetVideoInfoAsync("http://test.com", false);

        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void DownloadVideoAsync_Throws_WhenNotReady()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);

        Func<Task> act = () => service.DownloadVideoAsync(
            "http://test.com", "C:\\", false, false, null, "mp4",
            new Progress<string>(_ => { }), new Progress<double>(_ => { }));

        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task InitializeAsync_DoesNotDownload_WhenYtDlpExists()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");
        File.WriteAllText(ytDlpPath, "fake yt-dlp");
        File.WriteAllText(ffmpegPath, "fake ffmpeg");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);
        var messages = new List<string>();
        var progress = new Progress<string>(msg => messages.Add(msg));

        await service.InitializeAsync(progress);

        service.IsReady.Should().BeTrue();
        messages.Should().Contain("Ready.");
    }

    [Fact]
    public void InitializeAsync_SetsIsReady_WhenBothFilesExist()
    {
        var ytDlpPath = Path.Combine(_testDir, "yt-dlp.exe");
        var ffmpegPath = Path.Combine(_testDir, "ffmpeg.exe");
        File.WriteAllText(ytDlpPath, "fake yt-dlp");
        File.WriteAllText(ffmpegPath, "fake ffmpeg");

        var service = CreateServiceWithPaths(ytDlpPath, ffmpegPath);

        service.IsReady.Should().BeTrue();
    }

    private static YtDlpService CreateServiceWithPaths(string ytDlpPath, string ffmpegPath)
    {
        var pathsField = typeof(YtDlpService).GetField("_ytDlpPath", BindingFlags.NonPublic | BindingFlags.Instance);
        var ffmpegPathField = typeof(YtDlpService).GetField("_ffmpegPath", BindingFlags.NonPublic | BindingFlags.Instance);

        var service = new YtDlpService();
        pathsField!.SetValue(service, ytDlpPath);
        ffmpegPathField!.SetValue(service, ffmpegPath);

        return service;
    }
}
