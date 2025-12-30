using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;

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
        mockStandardOutput.SetupGet(s => s.EndOfStream).Returns(outputStream.Position >= outputStream.Length);

        mockProcess.SetupGet(p => p.StartInfo).Returns(mockStartInfo.Object);
        mockProcess.SetupGet(p => p.StandardOutput).Returns(mockStandardOutput.Object);
        mockProcess.SetupGet(p => p.StandardError).Returns(mockStandardError.Object);
        mockProcess.Setup(p => p.Start()).Returns(true);
        mockProcess.Setup(p => p.WaitForExitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
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
