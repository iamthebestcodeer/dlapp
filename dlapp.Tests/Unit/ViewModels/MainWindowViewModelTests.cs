using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using dlapp.Services;
using dlapp.ViewModels;
using Moq;

namespace dlapp.Tests.Unit.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        var vm = CreateViewModel();

        vm.Url.Should().BeEmpty();
        vm.SelectedResolution.Should().Be("Best");
        vm.SelectedVideoFormat.Should().Be("mp4");
        vm.SelectedAudioFormat.Should().Be("mp3");
        vm.Items.Should().BeEmpty();
        vm.Resolutions.Should().HaveCount(7);
        vm.VideoFormats.Should().HaveCount(3);
        vm.AudioFormats.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public async Task CanDownload_ReturnsCorrectly(string url, bool expected)
    {
        var vm = CreateViewModel();
        vm.Url = url;
        await Task.Delay(200);

        var result = CallCanDownload(vm);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanDownload_ReturnsTrue_WhenUrlIsValidAndNotBusy()
    {
        var vm = CreateViewModel();
        vm.Url = "http://valid.com";
        await Task.Delay(500);

        var isBusyBefore = vm.IsBusy;

        if (isBusyBefore)
        {
            vm.IsBusy = false;
        }

        var result = CallCanDownload(vm);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanDownload_ReturnsFalse_WhenIsBusy()
    {
        var vm = CreateViewModel();
        vm.Url = "http://valid.com";
        vm.IsBusy = true;

        var result = CallCanDownload(vm);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanDownload_ReturnsTrue_WhenReady()
    {
        var vm = CreateViewModel();
        vm.Url = "http://valid.com";
        vm.IsBusy = false;

        var result = CallCanDownload(vm);

        result.Should().BeTrue();
    }

    private static bool CallCanDownload(MainWindowViewModel vm)
    {
        var method = typeof(MainWindowViewModel).GetMethod("CanDownload", BindingFlags.NonPublic | BindingFlags.Instance);
        return (bool)(method!.Invoke(vm, null)!);
    }

    [Fact]
    public async Task SelectSavePathAsync_UpdatesSavePath_WhenDialogReturnsPath()
    {
        var vm = CreateViewModel();
        var expectedPath = @"C:\Downloads";
        vm.ShowOpenFolderDialog = () => Task.FromResult<string?>(expectedPath);

        await vm.SelectSavePathCommand.ExecuteAsync(null);

        vm.SavePath.Should().Be(expectedPath);
    }

    [Fact]
    public async Task SelectSavePathAsync_DoesNothing_WhenDialogReturnsNull()
    {
        var vm = CreateViewModel();
        var originalPath = vm.SavePath;
        vm.ShowOpenFolderDialog = () => Task.FromResult<string?>(null);

        await vm.SelectSavePathCommand.ExecuteAsync(null);

        vm.SavePath.Should().Be(originalPath);
    }

    [Fact]
    public async Task SelectSavePathAsync_DoesNothing_WhenDialogReturnsEmpty()
    {
        var vm = CreateViewModel();
        var originalPath = vm.SavePath;
        vm.ShowOpenFolderDialog = () => Task.FromResult<string?>(string.Empty);

        await vm.SelectSavePathCommand.ExecuteAsync(null);

        vm.SavePath.Should().Be(originalPath);
    }

    [Fact]
    public async Task SelectSavePathAsync_DoesNothing_WhenShowOpenFolderDialogIsNull()
    {
        var vm = CreateViewModel();
        var originalPath = vm.SavePath;
        vm.ShowOpenFolderDialog = null;

        await vm.SelectSavePathCommand.ExecuteAsync(null);

        vm.SavePath.Should().Be(originalPath);
    }

    [Fact]
    public void Resolutions_HasExpectedValues()
    {
        var vm = CreateViewModel();

        vm.Resolutions.Should().Contain("Best");
        vm.Resolutions.Should().Contain("1080p");
        vm.Resolutions.Should().Contain("720p");
        vm.Resolutions.Should().Contain("480p");
    }

    [Fact]
    public void VideoFormats_HasExpectedValues()
    {
        var vm = CreateViewModel();

        vm.VideoFormats.Should().Contain("mp4");
        vm.VideoFormats.Should().Contain("mkv");
        vm.VideoFormats.Should().Contain("webm");
    }

    [Fact]
    public void AudioFormats_HasExpectedValues()
    {
        var vm = CreateViewModel();

        vm.AudioFormats.Should().Contain("mp3");
        vm.AudioFormats.Should().Contain("m4a");
        vm.AudioFormats.Should().Contain("wav");
    }

    [Fact]
    public void IsAudioOnly_DefaultIsFalse()
    {
        var vm = CreateViewModel();

        vm.IsAudioOnly.Should().BeFalse();
    }

    [Fact]
    public void IsPlaylist_DefaultIsFalse()
    {
        var vm = CreateViewModel();

        vm.IsPlaylist.Should().BeFalse();
    }

    [Fact]
    public void ProgressValue_DefaultIsZero()
    {
        var vm = CreateViewModel();

        vm.ProgressValue.Should().Be(0);
    }

    [Fact]
    public void StatusMessage_NotEmptyAfterConstruction()
    {
        var vm = CreateViewModel();

        vm.StatusMessage.Should().NotBeNullOrEmpty();
    }

    private static MainWindowViewModel CreateViewModel()
    {
        return new TestableMainWindowViewModel();
    }

    private class TestableMainWindowViewModel : MainWindowViewModel
    {
        public TestableMainWindowViewModel()
        {
        }

        public void SetIsBusy(bool value)
        {
            var field = typeof(MainWindowViewModel).GetField("_isBusy", BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(this, value);
            OnPropertyChanged(nameof(IsBusy));
        }
    }
}
