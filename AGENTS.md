# AGENTS.md

dlapp is a .NET 9.0 Avalonia UI desktop app for video downloading via yt-dlp. The year is 2026

## Key Technologies
- .NET 9.0, C# 12+, Avalonia UI 11.3.9, CommunityToolkit.Mvvm 8.2.1
- xUnit 2.6.2, FluentAssertions 6.12.0, Moq 4.20.70

## Build Commands

```bash
dotnet build                      # Build solution
dotnet build -c Release           # Release build
dotnet format dlapp.csproj        # Format code
dotnet publish -c Release -o release/win-x64  # Windows publish
```

## Test Commands

```bash
dotnet test                                      # All tests
dotnet test --filter "FullyQualifiedName~YtDlpServiceTests"  # Test class
dotnet test --filter "FullyQualifiedName~YtDlpServiceTests.IsReady_True"  # Single test
dotnet test --verbosity normal                   # Verbose output
```

## Code Style

### General
- File-scoped namespaces (`namespace dlapp.ViewModels;`)
- Nullable reference types enabled (`string?`)
- No wildcard imports

### Naming Conventions
| Element | Convention | Example |
|---------|------------|---------|
| Classes/Methods/Properties | PascalCase | `DownloadAsync`, `IsBusy` |
| Private fields | `_camelCase` | `_ytDlpService`, `_savePath` |
| Parameters/Local vars | camelCase | `url`, `progressCallback` |
| Interfaces | `I` prefix | `IYtDlpService` |

### CommunityToolkit.Mvvm Patterns

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
private string _url = string.Empty;

[RelayCommand(CanExecute = nameof(CanDownload))]
private async Task DownloadAsync() { }
```

### Error Handling
```csharp
try { /* work */ }
catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
finally { IsBusy = false; }
```

### Async/Threading
- Use `async Task` for I/O operations
- **Always run Process operations on background threads** with `Task.Run()`
- Use `ConfigureAwait(false)` in service methods

### Import Ordering
System imports first, then external libraries, then project imports:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using dlapp.Services;
```

### String Comparisons
Use explicit `StringComparison`:
```csharp
// Ordinal for performance
if (line.StartsWith("[", StringComparison.Ordinal)) continue;

// OrdinalIgnoreCase for user-facing
if (msg.Contains("Downloading", StringComparison.OrdinalIgnoreCase))
```

## File Organization
- `Views/` - Avalonia XAML (`.axaml`)
- `ViewModels/` - MVVM ViewModels
- `Services/` - Business logic
- `dlapp.Tests/` - Unit tests

## Testing Patterns
- FluentAssertions: `result.Should().BeTrue()`
- `[Fact]` / `[Theory]` with `[InlineData]`
- Mock dependencies with Moq

### Testing Private Members
```csharp
var field = typeof(MainWindowViewModel).GetField("_ytDlpService",
    BindingFlags.NonPublic | BindingFlags.Instance);
field!.SetValue(vm, mockService.Object);
```

## Avalonia
- Use compiled bindings (`x:DataType`)
- `ObservableCollection<T>` for dynamic collections
- `IProgress<T>` for progress callbacks

## Git
- Concise commit messages
- Run tests before commit: `dotnet test --no-build`
- **Ignored Files**: `reports/` and `coverage-report/`

## Testing Pitfalls

### Mocking Non-Virtual/Non-Interface Methods
Moq cannot mock non-virtual methods or methods on concrete classes without interfaces. Before writing tests that mock a service:
1. Check if the service has an interface (e.g., `IYtDlpService`)
2. If not, either create one or use a different testing approach

### Avoid Arbitrary Task.Delay in Tests
Never use `Task.Delay()` to wait for async operations. Instead:
- Test synchronous operations synchronously (no await)
- For async operations, `await` the actual operation
- Verify result state directly

### Test Initial Values
Constructor initializers vs async `InitializeAsync()` calls may differ. Test the actual initial value, not the post-initialization value.

### Remove Unused Test Code
Delete unused helper methods, test fixtures, and subclasses. Unused `using` statements cause compile errors after file modifications.

###
The user is not a programmer, you need to explain things very simply to him.
