# AGENTS.md

This file provides guidance for AI agents working on the dlapp codebase.

## Project Overview

dlapp is a .NET 9.0 desktop application using Avalonia UI for video downloading via yt-dlp. It follows MVVM architecture with CommunityToolkit.Mvvm.

**Key Technologies:**
- .NET 9.0 with C# 12+
- Avalonia UI 11.3.9
- CommunityToolkit.Mvvm 8.2.1
- xUnit 2.6.2 for testing
- FluentAssertions 6.12.0 and Moq 4.20.70 for tests

## Build Commands

```bash
# Build the solution
dotnet build

# Build Release configuration
dotnet build -c Release

# Restore dependencies
dotnet restore

# Clean build artifacts
dotnet clean

# Format code (applies editorconfig rules)
dotnet format dlapp.csproj

# Verify code formatting without changes
dotnet format dlapp.csproj --verify-no-changes

# Publish for Windows (single exe, self-contained)
dotnet publish -c Release -o release/win-x64

# Publish for Linux
dotnet publish dlapp.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o release/linux-x64
```

## Test Commands

```bash
# Run all tests
dotnet test

# Run all tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~MainWindowViewModelTests"

# Run single test method
dotnet test --filter "FullyQualifiedName~VideoItemTests.Constructor_InitializesWithDefaults"

# Run tests in parallel
dotnet test --parallel

# Run tests with verbose output
dotnet test --verbosity normal
```

## Code Style Guidelines

### General Principles

- **No comments** unless explicitly required by the task. Let code explain itself.
- **File-scoped namespaces** (`namespace dlapp.ViewModels;`)
- **Nullable reference types enabled** - use `?` for nullable types.
- **Use explicit `using` statements** - no wildcard imports.

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `MainWindowViewModel`, `VideoItem` |
| Methods | PascalCase | `DownloadAsync`, `InitializeAsync` |
| Properties | PascalCase | `Url`, `SavePath`, `IsBusy` |
| Fields (private) | camelCase with `_` prefix | `_ytDlpService`, `_savePath` |
| Constants | PascalCase | `YtDlpUrl`, `MaxVideoHeight` |
| Local variables | camelCase | `videoInfo`, `progressCallback` |
| Parameters | camelCase | `string url`, `bool isPlaylist` |
| Interfaces | PascalCase with `I` prefix | `IYtDlpService` |

### Property and Command Patterns

Use CommunityToolkit.Mvvm source generators:

```csharp
// Observable property with automatic INotifyPropertyChanged
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
private string _url = string.Empty;

// Read-only collection
public List<string> Resolutions { get; } = new() { "Best", "1080p", "720p" };

// RelayCommand with CanExecute
[RelayCommand(CanExecute = nameof(CanDownload))]
private async Task DownloadAsync() { }

// Command without CanExecute
[RelayCommand]
private async Task SelectSavePathAsync() { }
```

### Error Handling

- Use `try-catch` blocks for async operations that may fail.
- Catch specific exceptions before general `Exception`.
- Preserve exception messages for user feedback.
- Set `IsBusy = false` in `finally` blocks.

```csharp
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
```

### Async/Await Patterns

- Use `async Task` for methods performing I/O.
- Use `async ValueTask` for hot paths if needed.
- Avoid `.Result` or `.Wait()` - use `await` always.
- Handle async initialization in constructors via fire-and-forget:

```csharp
public MainWindowViewModel()
{
    _ytDlpService = new YtDlpService();
    _ = InitializeAsync();
}
```

### String Comparisons

Use `StringComparison` overloads for explicit comparison behavior:

```csharp
// Ordinal for performance-sensitive paths
if (line.StartsWith("[", StringComparison.Ordinal)) continue;

// OrdinalIgnoreCase for user-facing strings
if (msg.Contains("Downloading video", StringComparison.OrdinalIgnoreCase))
```

### Import Ordering

Order imports alphabetically within groups:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using dlapp.Services;
```

### File Organization

| Folder | Purpose |
|--------|---------|
| `Views/` | Avalonia XAML views (`.axaml`) |
| `ViewModels/` | MVVM ViewModels |
| `Services/` | Business logic (YtDlpService) |
| `Models/` | Domain objects |
| `dlapp.Tests/` | Unit and integration tests |

### Test Patterns

- Use **FluentAssertions** for readable assertions:

```csharp
vm.Url.Should().BeEmpty();
vm.Items.Should().HaveCount(3);
result.Should().BeTrue();
```

- Use `[Fact]` for single tests, `[Theory]` with `[InlineData]` for parameterized tests.
- Mock external dependencies with Moq.
- Use `IDisposable` for test fixtures requiring cleanup.
- Test file: `Namespace.Tests.Unit.ComponentTests.cs`

### Testing Gotchas

**Main Project File Exclusions:**
The main `dlapp.csproj` may accidentally include test files from nested test projects. If tests fail with missing dependencies (xUnit, Moq, etc.), add exclusions:

```xml
<ItemGroup>
  <Compile Remove="dlapp.Tests\**" />
  <EmbeddedResource Remove="dlapp.Tests\**" />
  <None Remove="dlapp.Tests\**" />
</ItemGroup>
```

**Testing Private Members:**
Use reflection to test private methods or inject test doubles:

```csharp
var method = typeof(MainWindowViewModel).GetMethod("CanDownload", BindingFlags.NonPublic | BindingFlags.Instance);
var result = (bool)method!.Invoke(vm, null)!;
```

**Testing Fire-and-Forget Async Initialization:**
ViewModels that call `_ = InitializeAsync()` in the constructor need delays before testing:

```csharp
var vm = new MainWindowViewModel();
Task.Delay(500).Wait(); // Wait for async initialization to complete
```

**Testing Private Fields:**
Inject mock services via private field reflection:

```csharp
var field = typeof(MainWindowViewModel).GetField("_ytDlpService", BindingFlags.NonPublic | BindingFlags.Instance);
field!.SetValue(vm, mockService.Object);
```

**Testing YtDlpService:**
`IsReady` is computed from `File.Exists` checks. Use reflection to inject test paths. Fields are `readonly` so set them directly:

```csharp
var ytDlpField = typeof(YtDlpService).GetField("_ytDlpPath", BindingFlags.NonPublic | BindingFlags.Instance);
var ffmpegField = typeof(YtDlpService).GetField("_ffmpegPath", BindingFlags.NonPublic | BindingFlags.Instance);
ytDlpField!.SetValue(service, testYtDlpPath);
ffmpegField!.SetValue(service, testFfmpegPath);
```

**Testing Static Properties:**
When a class uses static properties for computed paths (e.g., `AppDataRoot`), test the static property directly:

```csharp
var appDataRootProp = typeof(YtDlpService).GetProperty("AppDataRoot", BindingFlags.NonPublic | BindingFlags.Static);
var actual = appDataRootProp!.GetValue(null)!.ToString();
actual.Should().Be(expectedPath);
```

**Avoid UI Tests in Headless CI:**
Tests requiring Avalonia's `IWindowingPlatform` will fail without a display. Skip UI tests or use separate headless-compatible tests.

**Integration Tests:**
Tests requiring network access or actual yt-dlp binaries should be skipped with `[Fact(Skip = "...")]` or run manually.

### ViewModelBase Pattern

All ViewModels inherit from `ViewModelBase`:

```csharp
public partial class MainWindowViewModel : ViewModelBase
```

### Null Handling

- Use nullable reference types (`string?`, `int?`).
- Check for null/empty: `string.IsNullOrWhiteSpace(url)`.
- Use null-conditional operators: `items?.Count ?? 0`.
- Provide defaults: `SavePath = value ?? defaultPath;`

### Avalonia Specific

- Use compiled bindings (`x:DataType`).
- Bind to ViewModel properties in XAML.
- Avoid code-behind logic; prefer ViewModel commands.
- Use `ObservableCollection<T>` for dynamic collections.

### Progress Reporting

Use `IProgress<T>` for progress callbacks:

```csharp
var progress = new Progress<string>(msg => StatusMessage = msg);
await service.InitializeAsync(progress);
```

### Git Workflow

- Commit messages should be concise and descriptive.
- Group related changes in single commits.
- Run tests before committing: `dotnet test --no-build`

### Common Tasks

**Add a new property:**
1. Add `[ObservableProperty]` field in ViewModel
2. Add binding in corresponding `.axaml` file
3. Add unit test for property behavior

**Add a new command:**
1. Add method with `[RelayCommand]` attribute
2. Add `CanExecute` method if needed
3. Bind to Button in XAML
4. Add unit tests for command execution

**Add a new test:**
1. Create test class in `dlapp.Tests/Unit/Component/`
2. Use `[Fact]` or `[Theory]`
3. Use FluentAssertions for assertions
4. Mock dependencies as needed
