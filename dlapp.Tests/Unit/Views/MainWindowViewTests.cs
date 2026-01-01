using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using FluentAssertions;

using Xunit;

namespace dlapp.Tests.Unit.Views;

public class MainWindowXamlTests
{
    private readonly string _solutionRoot;

    public MainWindowXamlTests()
    {
        _solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private string ReadFile(string relativePath)
    {
        var fullPath = Path.Combine(_solutionRoot, relativePath);
        return File.ReadAllText(fullPath);
    }

    [Fact]
    public void MainWindowXaml_DoesNotContainSpacerControl()
    {
        var xaml = ReadFile("Views/MainWindow.axaml");

        xaml.Should().NotContain("<Spacer",
            "Avalonia does not have a Spacer control - use Rectangle instead");
    }

    [Fact]
    public void MainWindowXaml_DoesNotContainItemContainerStylesTypo()
    {
        var xaml = ReadFile("Views/MainWindow.axaml");

        xaml.Should().NotContain("ItemContainerStyles",
            "Use singular 'ItemContainerStyle' not plural 'ItemContainerStyles'");
    }

    [Fact]
    public void MainWindowXaml_BindingsMatchViewModelProperties()
    {
        var xaml = ReadFile("Views/MainWindow.axaml");
        var vmCode = ReadFile("ViewModels/MainWindowViewModel.cs");

        var bindingPatterns = new Dictionary<string, string[]>
        {
            { "Url", new[] { "{Binding Url}" } },
            { "SavePath", new[] { "{Binding SavePath}" } },
            { "IsAudioOnly", new[] { "{Binding !IsAudioOnly}", "{Binding IsAudioOnly}" } },
            { "IsPlaylist", new[] { "{Binding IsPlaylist}" } },
            { "SelectedResolution", new[] { "{Binding SelectedResolution}" } },
            { "SelectedVideoFormat", new[] { "{Binding SelectedVideoFormat}" } },
            { "SelectedAudioFormat", new[] { "{Binding SelectedAudioFormat}" } },
            { "DownloadCommand", new[] { "{Binding DownloadCommand}" } },
            { "SelectSavePathCommand", new[] { "{Binding SelectSavePathCommand}" } },
            { "Items", new[] { "{Binding Items}" } },
            { "ProgressValue", new[] { "{Binding ProgressValue}" } },
            { "StatusMessage", new[] { "{Binding StatusMessage}" } },
            { "IsBusy", new[] { "{Binding !IsBusy}" } }
        };

        foreach (var (propertyName, patterns) in bindingPatterns)
        {
            var patternFound = patterns.Any(p => xaml.Contains(p));
            if (patternFound)
            {
                var hasProperty = vmCode.Contains($"private string _{char.ToLowerInvariant(propertyName[0])}{propertyName.Substring(1)} =") ||
                                  vmCode.Contains($"private bool _{char.ToLowerInvariant(propertyName[0])}{propertyName.Substring(1)};") ||
                                  vmCode.Contains($"private double _{char.ToLowerInvariant(propertyName[0])}{propertyName.Substring(1)};") ||
                                  vmCode.Contains($"public ObservableCollection<VideoItem> {propertyName}") ||
                                  vmCode.Contains($"public List<string> {propertyName}") ||
                                  vmCode.Contains("DownloadCommand") && propertyName == "DownloadCommand" ||
                                  vmCode.Contains("SelectSavePathCommand") && propertyName == "SelectSavePathCommand";

                if (propertyName == "DownloadCommand" || propertyName == "SelectSavePathCommand")
                {
                    var methodName = propertyName.Replace("Command", "") + "Async";
                    vmCode.Should().Contain(methodName, $"View binds to '{propertyName}' but ViewModel should have corresponding method for source generation");
                }
                else if (!hasProperty)
                {
                    vmCode.Should().Contain($"private string _{char.ToLowerInvariant(propertyName[0])}{propertyName.Substring(1)}",
                        $"View binds to '{propertyName}' but ViewModel may not have this property (checking for generated backing field)");
                }
            }
        }
    }

    [Fact]
    public void MainWindowXaml_StaticResourcesExistInAppAxaml()
    {
        var mainWindowXaml = ReadFile("Views/MainWindow.axaml");
        var appXaml = ReadFile("App.axaml");

        var resourceReferences = Regex.Matches(mainWindowXaml, @"\{StaticResource\s+(\w+)\}")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct();

        foreach (var resourceName in resourceReferences)
        {
            appXaml.Should().Contain(resourceName,
                $"StaticResource '{resourceName}' referenced in MainWindow.axaml should exist in App.axaml");
        }
    }

    [Fact]
    public void MainWindowXaml_HasValidWindowProperties()
    {
        var xaml = ReadFile("Views/MainWindow.axaml");

        xaml.Should().Contain("Width=\"720\"", "Window should have width of 720");
        xaml.Should().Contain("Height=\"520\"", "Window should have height of 520");
        xaml.Should().Contain("Title=\"KAMO Video Downloader\"", "Window should have correct title");
    }

    [Fact]
    public void MainWindowXaml_HasBackgroundSet()
    {
        var xaml = ReadFile("Views/MainWindow.axaml");

        xaml.Should().Contain("Background=\"{StaticResource Background}\"",
            "Window should have Background set to StaticResource Background");
    }

    [Fact]
    public void AppAxaml_AnimationsHaveValidSyntax()
    {
        var appXaml = ReadFile("App.axaml");

        var translateMatches = Regex.Matches(appXaml, @"translateY\(([^)]+)\)")
            .Cast<System.Text.RegularExpressions.Match>();

        foreach (System.Text.RegularExpressions.Match match in translateMatches)
        {
            var value = match.Groups[1].Value;
            value.Should().EndWith("px",
                $"translateY value should end with 'px' unit. Found: '{value}'");
        }
    }
}

public class ViewModelBindingTests
{
    private readonly string _solutionRoot;

    public ViewModelBindingTests()
    {
        _solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private string ReadFile(string relativePath)
    {
        var fullPath = Path.Combine(_solutionRoot, relativePath);
        return File.ReadAllText(fullPath);
    }

    [Fact]
    public void MainWindowViewModel_HasAllRequiredProperties()
    {
        var vmCode = ReadFile("ViewModels/MainWindowViewModel.cs");

        var requiredProperties = new[]
        {
            "private string _url",
            "private string _savePath",
            "private bool _isAudioOnly",
            "private bool _isPlaylist",
            "private string _selectedResolution",
            "private string _selectedVideoFormat",
            "private string _selectedAudioFormat",
            "public ObservableCollection<VideoItem> Items",
            "private double _progressValue",
            "private string _statusMessage",
            "private bool _isBusy"
        };

        foreach (var property in requiredProperties)
        {
            vmCode.Should().Contain(property,
                $"ViewModel should have '{property}' that View binds to");
        }
    }

    [Fact]
    public void MainWindowViewModel_HasRequiredCommands()
    {
        var vmCode = ReadFile("ViewModels/MainWindowViewModel.cs");

        vmCode.Should().Contain("[RelayCommand(CanExecute = nameof(CanDownload))]", "ViewModel should have DownloadCommand with RelayCommand attribute");
        vmCode.Should().Contain("private async Task SelectSavePathAsync()", "ViewModel should have SelectSavePathAsync method for source-generated command");
    }

    [Fact]
    public void MainWindowViewModel_HasRequiredCollections()
    {
        var vmCode = ReadFile("ViewModels/MainWindowViewModel.cs");

        vmCode.Should().Contain("List<string> Resolutions", "ViewModel should have Resolutions collection");
        vmCode.Should().Contain("List<string> VideoFormats", "ViewModel should have VideoFormats collection");
        vmCode.Should().Contain("List<string> AudioFormats", "ViewModel should have AudioFormats collection");
    }
}
