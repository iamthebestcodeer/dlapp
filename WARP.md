# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

# Project Overview

This is a .NET 9.0 Desktop Application using **Avalonia UI**.
The project follows the **MVVM (Model-View-ViewModel)** architectural pattern, utilizing the **CommunityToolkit.Mvvm** library for MVVM support.

## Core Dependencies
- **Framework**: .NET 9.0
- **UI**: Avalonia UI (v11.3.9)
- **MVVM**: CommunityToolkit.Mvvm

# Common Commands

### Build & Run
- **Build**: `dotnet build`
- **Run**: `dotnet run`
- **Restore Dependencies**: `dotnet restore`
- **Clean**: `dotnet clean`

### Formatting
- **Format Code**: `dotnet format`

# Architecture & Structure

## High-Level Layout
- **Entry Point**: `Program.cs` initializes the Avalonia application.
- **Application Logic**: `App.axaml` and `App.axaml.cs` handle application lifetime, theme loading, and the main window initialization.
- **Views**: Located in `Views/`. These are `.axaml` files (and corresponding `.cs` code-behinds) defining the UI structure.
- **ViewModels**: Located in `ViewModels/`. These handle the presentation logic and state.
- **Models**: Located in `Models/`. Domain objects and business logic.
- **Services**: Located in `Services/`. Handles external interactions and business logic (e.g., `YtDlpService` for managing yt-dlp binaries and execution).

## External Binaries
- The application manages `yt-dlp.exe` and `ffmpeg.exe` automatically.
- `YtDlpService` downloads these binaries on first run and checks for `yt-dlp` updates.

## MVVM Pattern
- **Base Class**: All ViewModels should inherit from `ViewModelBase`, which inherits from `ObservableObject` (CommunityToolkit.Mvvm).
- **Data Binding**: The `DataContext` is typically set in the code-behind or parent ViewModel (e.g., `App.axaml.cs` sets `MainWindow.DataContext`).
- **Reactivity**: Use `[ObservableProperty]` to generate property change notifications automatically.
- **Commands**: Use `[RelayCommand]` to generate `ICommand` implementations for methods.

## Dependency Injection
Currently, the project does not use a Dependency Injection (DI) container.
- ViewModels are instantiated manually (e.g., in `App.axaml.cs`).
- Services should be passed via constructors if/when introduced.

## UI Interaction
- **Dialogs/Pickers**: Since there is no Dialog Service, ViewModels expose `Func` or `Action` properties (e.g., `ShowOpenFolderDialog`) that the View assigns (typically in `OnDataContextChanged` or code-behind) to handle UI-specific operations like file/folder pickers using `StorageProvider`.

# Development Guidelines
- **Avalonia & CommunityToolkit**: Note that `App.axaml.cs` contains logic to disable Avalonia's native data annotation validation (`DisableAvaloniaDataAnnotationValidation`) to avoid conflicts with CommunityToolkit's validation.
- **Compiled Bindings**: `AvaloniaUseCompiledBindingsByDefault` is enabled in the `.csproj`. Ensure bindings are correct to avoid build-time errors.
