using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using dlapp.Services;
using dlapp.ViewModels;

namespace dlapp.Tests.Helpers;

public static class TestHelpers
{
    public static Mock<YtDlpService> CreateMockYtDlpService(
        List<(string Index, string Title)>? videoInfos = null,
        Action<string, string, bool, bool, int?, string, IProgress<string>, IProgress<double>>? downloadCallback = null)
    {
        var mockService = new Mock<YtDlpService>();

        mockService.Setup(s => s.InitializeAsync(It.IsAny<IProgress<string>>()))
            .Returns(Task.CompletedTask);

        mockService.Setup(s => s.GetVideoInfoAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(videoInfos ?? [("1", "Test Video")]);

        if (downloadCallback != null)
        {
            mockService.Setup(s => s.DownloadVideoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int?>(), It.IsAny<string>(),
                It.IsAny<IProgress<string>>(), It.IsAny<IProgress<double>>(), It.IsAny<System.Threading.CancellationToken>()))
                .Callback<string, string, bool, bool, int?, string, IProgress<string>, IProgress<double>, System.Threading.CancellationToken>(
                    (url, path, audioOnly, isPlaylist, height, container, outputProgress, valueProgress, ct) =>
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
                It.IsAny<IProgress<string>>(), It.IsAny<IProgress<double>>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        return mockService;
    }

    public static MainWindowViewModel CreateViewModelWithMockService(
        YtDlpService? mockService = null)
    {
        var service = mockService ?? CreateMockYtDlpService().Object;

        var vm = new MainWindowViewModel();
        var field = typeof(MainWindowViewModel).GetField("_ytDlpService", BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(vm, service);

        return vm;
    }
}
