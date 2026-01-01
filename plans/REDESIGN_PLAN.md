# KAMO Video Downloader - UI Redesign Plan

## Executive Summary

This document outlines a complete UI redesign for dlapp, transforming it into **KAMO Video Downloader** - a minimalist, pure monochrome Windows 11 native application. The redesign focuses on clean aesthetics, system-native integration, and subtle visual polish without sacrificing performance.

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| App Name | KAMO Video Downloader | Brand identity, memorable, professional |
| Color Theme | Pure monochrome (black/grey only) | Minimalist aesthetic, no distractions |
| Accent Color | None | Maximum minimalism |
| Window Style | Windows 11 native chrome | OS consistency, performance |
| Corner Radius | 8px | Modern Windows 11 default feel |
| Icons | App logo only | Pure typography UI |
| Background | System dark theme | Native integration |

---

## 1. Design System

### 1.1 Color Palette

```csharp
// Monochrome Dark Theme
Color.Background        = "#0D0D0D";  // Deepest black-grey (window bg)
Color.Panel             = "#141414";  // Slightly lighter (content areas)
Color.Surface           = "#1A1A1A";  // Interactive elements bg
Color.Border            = "#2A2A2A";  // Subtle borders
Color.BorderHover       = "#404040";  // Border on hover
Color.BorderFocus       = "#606060";  // Border on focus (not accent)
Color.TextPrimary       = "#E5E5E5";  // Primary text
Color.TextSecondary     = "#888888";  // Secondary text (labels, placeholders)
Color.TextTertiary      = "#666666";  // Tertiary text (disabled, hints)
Color.Divider           = "#252525";  // Horizontal dividers
```

### 1.2 Typography

```csharp
// Font Family - System fonts (no external dependencies)
FontFamily = "Segoe UI, Helvetica, Arial, sans-serif";

// Font Sizes
FontSize.H1              = 28;        // App title (KAMO)
FontSize.H2              = 16;        // Section headers
FontSize.Body            = 14;        // Regular text
FontSize.Caption         = 12;        // Labels, hints
FontSize.Monospace       = 13;        // Path display, URLs

// Font Weights
FontWeight.Regular       = 400;
FontWeight.Medium        = 500;
FontWeight.SemiBold      = 600;
FontWeight.Bold          = 700;

// Letter Spacing
LetterSpacing.Normal     = 0;
LetterSpacing.Caption    = 0.5;       // Uppercase labels
```

### 1.3 Spacing System

```csharp
// Consistent spacing scale (8px base unit)
Spacing.XS               = 4;         // Tight spacing
Spacing.SM               = 8;         // Standard spacing
Spacing.MD               = 16;        // Element spacing
Spacing.LG               = 24;        // Section spacing
Spacing.XL               = 32;        // Page margins
Spacing.XXL              = 48;        // Full page
```

### 1.4 Corner Radius

```csharp
// 8px for modern Windows 11 feel
CornerRadius.Small       = 4;         // Small elements (chips, tags)
CornerRadius.Medium      = 8;         // Standard elements (buttons, inputs)
CornerRadius.Large       = 12;        // Large containers
CornerRadius.Full        = 9999;      // Pill shapes
```

### 1.5 Border Width

```csharp
BorderWidth.Thin         = 1;         // Subtle dividers
BorderWidth.Normal       = 1;         // Standard elements
BorderWidth.Focus        = 1.5;       // Focus ring (subtle)
```

### 1.6 Shadows (Performance-Safe)

```csharp
// Soft, subtle shadows only where needed
Shadow.None              = "none";
Shadow.Subtle            = "0 1 2 rgba(0,0,0,0.3)";    // Cards
Shadow.Medium            = "0 4 8 rgba(0,0,0,0.4)";    // Dropdowns
Shadow.Window            = "0 8 24 rgba(0,0,0,0.5)";   // Window
```

---

## 2. Layout Redesign

### 2.1 Current Layout (To Be Replaced)

```
┌─────────────────────────────────────────┐
│ ┌─────────┬───────────────────────────┐ │
│ │ YOUTUBE │  URL Input                │ │
│ │    DL   │  Save Path                │ │
│ │   APP   │  Format Selection         │ │
│ │         │  Download Button          │ │
│ │  v1.0.0 │  Progress Bar             │ │
│ └─────────┴───────────────────────────┘ │
│           Video List                    │
└─────────────────────────────────────────┘
```

### 2.2 New Layout (Single Panel)

```
┌─────────────────────────────────────┐
│ ┌──┐  KAMO                       v1│
│ │◼│  Video Downloader               │
├─────────────────────────────────────┤
│                                     │
│  URL                                 │
│  ╭─────────────────────────────────╮│
│  │ https://youtube.com/watch?v=... ││
│  ╰─────────────────────────────────╯│
│                                     │
│  Save to                             │
│  ╭─────────────────────────────────╮│
│  │ C:\Users\...\Downloads        │█│
│  ╰─────────────────────────────────╯│
│                                     │
│  Video + Audio    Audio Only        │
│  ⭘                 ○                │
│                                     │
│  ☑ Playlist                           │
│                                     │
│  Quality          Format            │
│  ┌──────┐         ┌──────┐          │
│  │1080p▼│         │MP4 ▼ │          │
│  └──────┘         └──────┘          │
│                                     │
│  ╭─────────────────────────────────╮│
│  │      INITIATE DOWNLOAD          ││
│  ╰─────────────────────────────────╯│
│                                     │
│  ─────────────────────────────────  │
│                                     │
│  1.  Video Title Here       ●       │
│  2.  Another Video...       ✓       │
│                                     │
└─────────────────────────────────────┘
```

### 2.3 Layout Specifications

```xml
<!-- Main Window -->
<Window>
    Width="720" Height="520"
    MinWidth="640" MinHeight="480"
    WindowStartupLocation="CenterScreen"
    ExtendClientAreaToDecorationsHint="True"
</Window>

<!-- Grid Layout -->
<Grid RowDefinitions="Auto,*,Auto" Margin="24">
    <!-- Header Row -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="12">
        <Image Source="/Assets/logo.png" Width="32" Height="32"/>
        <TextBlock Text="KAMO" FontSize="24" FontWeight="Bold"/>
        <TextBlock Text="Video Downloader" FontSize="14" VerticalAlignment="Center" Foreground="{StaticResource TextSecondary}"/>
        <Spacer/>
        <TextBlock Text="v1.0.0" FontSize="11" VerticalAlignment="Center" Foreground="{StaticResource TextTertiary}"/>
    </StackPanel>

    <!-- Content Row -->
    <Grid Grid.Row="1" RowDefinitions="Auto,Auto,Auto,Auto,Auto,*,Auto" Margin="0,24">
        <!-- URL Input -->
        <StackPanel Grid.Row="0" Spacing="6">
            <TextBlock Text="URL" FontSize="11" Foreground="{StaticResource TextSecondary}"/>
            <TextBox PlaceholderText="https://youtube.com/watch?v=..."/>
        </StackPanel>

        <!-- Save Path -->
        <StackPanel Grid.Row="1" Spacing="6" Margin="0,16">
            <TextBlock Text="SAVE TO" FontSize="11" Foreground="{StaticResource TextSecondary}"/>
            <Grid ColumnDefinitions="*,Auto">
                <TextBox Grid.Column="0" IsReadOnly="True" PlaceholderText="Select folder..."/>
                <Button Grid.Column="1" Content="Browse" Margin="8,0,0,0"/>
            </Grid>
        </StackPanel>

        <!-- Format Selection -->
        <StackPanel Grid.Row="2" Spacing="6" Margin="0,8">
            <TextBlock Text="MODE" FontSize="11" Foreground="{StaticResource TextSecondary}"/>
            <StackPanel Orientation="Horizontal" Spacing="16">
                <RadioButton Content="Video + Audio"/>
                <RadioButton Content="Audio Only"/>
            </StackPanel>
            <CheckBox Content="Playlist" Margin="0,4"/>
        </StackPanel>

        <!-- Quality & Format -->
        <Grid Grid.Row="3" ColumnDefinitions="*,*" Margin="0,16">
            <StackPanel Grid.Column="0" Spacing="6">
                <TextBlock Text="QUALITY" FontSize="11" Foreground="{StaticResource TextSecondary}"/>
                <ComboBox/>
            </StackPanel>
            <StackPanel Grid.Column="1" Spacing="6">
                <TextBlock Text="FORMAT" FontSize="11" Foreground="{StaticResource TextSecondary}"/>
                <ComboBox/>
            </StackPanel>
        </Grid>

        <!-- Download Button -->
        <Button Grid.Row="4" Content="DOWNLOAD" Margin="0,8"/>

        <!-- Video List -->
        <Border Grid.Row="5" Margin="0,16" Background="{StaticResource Surface}">
            <ListBox/>
        </Border>
    </Grid>

    <!-- Status Bar -->
    <StackPanel Grid.Row="2" Spacing="4">
        <ProgressBar Height="2" IsIndeterminate="True"/>
        <TextBlock Text="Ready" FontSize="11" HorizontalAlignment="Center"/>
    </StackPanel>
</Grid>
```

---

## 3. Component Specifications

### 3.1 TextBox (Input Fields)

```xml
<Style Selector="TextBox">
    <Setter Property="Background" Value="{StaticResource Surface}"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
    <Setter Property="BorderBrush" Value="{StaticResource Border}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Padding" Value="12,10"/>
    <Setter Property="FontFamily" Value="Consolas, Monospace"/>
    <Setter Property="FontSize" Value="13"/>
    <Setter Property="CaretBrush" Value="{StaticResource TextPrimary}"/>
    <Setter Property="Height" Value="40"/>
</Style>

<Style Selector="TextBox:focus /template/ Border#PART_BorderElement">
    <Setter Property="BorderBrush" Value="{StaticResource BorderFocus}"/>
    <Setter Property="BorderThickness" Value="1.5"/>
</Style>

<Style Selector="TextBox:pointerover /template/ Border#PART_BorderElement">
    <Setter Property="BorderBrush" Value="{StaticResource BorderHover}"/>
</Style>
```

### 3.2 Button (Primary)

```xml
<Style Selector="Button.primary">
    <Setter Property="Background" Value="{StaticResource Surface}"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
    <Setter Property="BorderBrush" Value="{StaticResource Border}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Padding" Value="20,12"/>
    <Setter Property="FontWeight" Value="600"/>
    <Setter Property="FontSize" Value="13"/>
    <Setter Property="Height" Value="40"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="Background" Duration="0:0:0.15"/>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.15"/>
            <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>

<Style Selector="Button.primary:pointerover /template/ ContentPresenter">
    <Setter Property="Background" Value="{StaticResource BorderHover}"/>
    <Setter Property="RenderTransform" Value="scale(1.01)"/>
</Style>

<Style Selector="Button.primary:pressed /template/ ContentPresenter">
    <Setter Property="Background" Value="{StaticResource Border}"/>
    <Setter Property="RenderTransform" Value="scale(0.99)"/>
</Style>
```

### 3.3 RadioButton

```xml
<Style Selector="RadioButton">
    <Setter Property="Foreground" Value="{StaticResource TextSecondary}"/>
    <Setter Property="FontWeight" Value="500"/>
    <Setter Property="Padding" Value="8,4"/>
</Style>

<Style Selector="RadioButton:checked">
    <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
</Style>

<Style Selector="RadioButton:pointerover">
    <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
</Style>
```

### 3.4 CheckBox

```xml
<Style Selector="CheckBox">
    <Setter Property="Foreground" Value="{StaticResource TextSecondary}"/>
    <Setter Property="FontWeight" Value="500"/>
    <Setter Property="Padding" Value="8,4"/>
</Style>

<Style Selector="CheckBox:checked">
    <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
</Style>
```

### 3.5 ComboBox

```xml
<Style Selector="ComboBox">
    <Setter Property="Background" Value="{StaticResource Surface}"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
    <Setter Property="BorderBrush" Value="{StaticResource Border}"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="CornerRadius" Value="8"/>
    <Setter Property="Padding" Value="12,10"/>
    <Setter Property="Height" Value="40"/>
</Style>

<Style Selector="ComboBox:focus /template/ Border#PART_BorderElement">
    <Setter Property="BorderBrush" Value="{StaticResource BorderFocus}"/>
</Style>
```

### 3.6 ProgressBar

```xml
<Style Selector="ProgressBar">
    <Setter Property="Background" Value="{StaticResource Border}"/>
    <Setter Property="Foreground" Value="{StaticResource TextPrimary}"/>
    <Setter Property="BorderThickness" Value="0"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="Height" Value="3"/>
</Style>

<Style Selector="ProgressBar:indeterminate">
    <Setter Property="Foreground" Value="{StaticResource TextSecondary}"/>
</Style>
```

### 3.7 ListBox (Video Items)

```xml
<ListBox Background="Transparent">
    <ListBox.Styles>
        <Style Selector="ListBoxItem">
            <Setter Property="Padding" Value="12,10"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="CornerRadius" Value="6"/>
            <Setter Property="Transitions">
                <Transitions>
                    <BrushTransition Property="Background" Duration="0:0:0.1"/>
                </Transitions>
            </Setter>
        </Style>

        <Style Selector="ListBoxItem:pointerover">
            <Setter Property="Background" Value="{StaticResource Surface}"/>
        </Style>

        <Style Selector="ListBoxItem:selected">
            <Setter Property="Background" Value="{StaticResource Border}"/>
        </Style>
    </ListBox.Styles>
</ListBox>
```

---

## 4. Animations & Micro-Interactions

### 4.1 Window Entrance Animation

```xml
<!-- Add to Window resources -->
<Window.Styles>
    <Style Selector="Window">
        <Setter Property="Opacity" Value="0"/>
        <Setter Property="RenderTransform" Value="translateY(10)"/>
        <Setter Property="Transitions">
            <Transitions>
                <DoubleTransition Property="Opacity" Duration="0:0:0.3" Easing="CubicEaseOut"/>
                <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" Easing="CubicEaseOut"/>
            </Transitions>
        </Setter>
    </Style>
</Window.Styles>
```

### 4.2 Button Hover/Press (Already Exists, Will Refine)

Current implementation is good - will keep but remove lime green references.

### 4.3 Focus Transitions

```xml
<Style Selector="TextBox, ComboBox">
    <Setter Property="Transitions">
        <Transitions>
            <BrushTransition Property="BorderBrush" Duration="0:0:0.15"/>
            <ThicknessTransition Property="BorderThickness" Duration="0:0:0.15"/>
        </Transitions>
    </Setter>
</Style>
```

### 4.4 List Item Animations

```xml
<ListBox.ItemContainerStyles>
    <Style Selector="ListBoxItem">
        <Setter Property="Opacity" Value="0"/>
        <Setter Property="RenderTransform" Value="translateY(5)"/>
        <Setter Property="Transitions">
            <Transitions>
                <DoubleTransition Property="Opacity" Duration="0:0:2.0" Easing="CubicEaseOut"/>
                <TransformOperationsTransition Property="RenderTransform" Duration="0:0:2.0" Easing="CubicEaseOut"/>
            </Transitions>
        </Setter>
    </Style>
</ListBox.ItemContainerStyles>
```

---

## 5. File Changes Summary

### 5.1 Files to Modify

| File | Changes |
|------|---------|
| `Views/MainWindow.axaml` | Complete UI redesign |
| `App.axaml` | New color palette, updated styles |
| `App.axaml.cs` | Update `Application.Title` |
| `Installer/Product.wxs` | Update product name |
| `Assets/logo.png` | Update or create logo |

### 5.2 Files to Delete

| File | Reason |
|------|--------|
| `Views/MainWindow.axaml.cs` | No changes needed (code-behind unchanged) |

---

## 6. Implementation Phases

### Phase 1 - Foundation (2-3 hours)

1. **Create color resource dictionary** in `App.axaml`
   - Define all colors as static resources
   - Set up light/dark theme support (though using dark only)

2. **Update basic styles**
   - TextBox, Button, RadioButton, CheckBox, ComboBox
   - ProgressBar, ListBox

3. **Create new layout structure**
   - Single panel design
   - Header with logo + title
   - Main content grid

### Phase 2 - Components (2-3 hours)

4. **Implement all input components**
   - URL text box
   - Save path with browse button
   - Format selection (radio + check)
   - Quality/format dropdowns

5. **Download button**
   - Outlined style
   - Hover/press animations

6. **Video list**
   - Clean row design
   - Status indicators (text-based, no icons)
   - Hover effects

### Phase 3 - Polish (1-2 hours)

7. **Animations**
   - Window entrance
   - List item stagger (if needed)
   - Focus transitions

8. **Window properties**
   - Size constraints
   - Title/icon
   - System chrome

### Phase 4 - Metadata (30 min)

9. **Update application metadata**
   - Title in App.axaml.cs
   - Installer name
   - Window title

---

## 7. Performance Considerations

### 7.1 What to Avoid

- ❌ Heavy blur filters (BackdropBlurBrush)
- ❌ Complex composition effects
- ❌ Animating layout properties (Width, Height, Margin)
- ❌ Too many simultaneous animations
- ❌ External icon/font dependencies

### 7.2 Performance-Safe Practices

- ✅ Animate only `Opacity` and `RenderTransform` (GPU-accelerated)
- ✅ Use `Transitions` for simple state changes
- ✅ Use system fonts (no loading time)
- ✅ Simple solid colors (no gradients, no images except logo)
- ✅ Minimal shadows (cheap to render)

---

## 8. Testing Checklist

### Visual Testing

- [ ] All text is readable and has proper contrast
- [ ] Focus states are visible but not distracting
- [ ] Hover states provide feedback
- [ ] Disabled states are clearly visible
- [ ] Window is properly sized and positioned
- [ ] No visual artifacts or glitches
- [ ] Dark mode integration is seamless

### Functional Testing

- [ ] All inputs accept text/keyboard input
- [ ] Buttons respond to clicks
- [ ] Radio/check boxes toggle correctly
- [ ] Combo boxes open and select items
- [ ] Listbox scrolls and selects items
- [ ] Progress bar shows correctly

### Performance Testing

- [ ] Window opens within 500ms
- [ ] No lag during typing
- [ ] No jank during hover animations
- [ ] Memory usage is reasonable (<100MB)

---

## 9. Rollback Plan

If the redesign causes issues:

1. Keep original files backed up in git
2. Changes are contained to:
   - `Views/MainWindow.axaml`
   - `App.axaml`
   - `App.axaml.cs`
   - `Installer/Product.wxs`

3. Simple `git checkout` can restore original state

---

## 10. Future Enhancements (Post-Redesign)

After the initial redesign, consider:

1. **System theme support** - Auto-detect light/dark mode
2. **Compact mode** - Smaller window for quick downloads
3. **Accent color option** - If users want it (toggleable)
4. **Custom fonts** - Optional, not included by default
5. **Lottie animations** - For status indicators (optional)

---

## Appendix A: Complete Style Reference

### App.axaml Style Dictionary

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="dlapp.App"
             RequestedThemeVariant="Dark">

    <Application.Resources>
        <!-- Colors -->
        <SolidColorBrush x:Key="Background" Color="#0D0D0D"/>
        <SolidColorBrush x:Key="Panel" Color="#141414"/>
        <SolidColorBrush x:Key="Surface" Color="#1A1A1A"/>
        <SolidColorBrush x:Key="Border" Color="#2A2A2A"/>
        <SolidColorBrush x:Key="BorderHover" Color="#404040"/>
        <SolidColorBrush x:Key="BorderFocus" Color="#606060"/>
        <SolidColorBrush x:Key="TextPrimary" Color="#E5E5E5"/>
        <SolidColorBrush x:Key="TextSecondary" Color="#888888"/>
        <SolidColorBrush x:Key="TextTertiary" Color="#666666"/>
        <SolidColorBrush x:Key="Divider" Color="#252525"/>

        <!-- Fonts -->
        <FontFamily x:Key="FontRegular">Segoe UI, Helvetica, Arial, sans-serif</FontFamily>
        <FontFamily x:Key="FontMonospace">Consolas, Monospace</FontFamily>
    </Application.Resources>

    <Application.Styles>
        <FluentTheme/>
        <!-- Custom styles will be added here -->
    </Application.Styles>
</Application>
```

---

## Appendix B: Estimated Time Breakdown

| Task | Estimated Time |
|------|----------------|
| Color palette setup | 30 min |
| Basic component styles | 1 hour |
| New layout structure | 1 hour |
| Input components | 1.5 hours |
| Download button | 30 min |
| Video list | 1 hour |
| Animations | 30 min |
| Window metadata | 15 min |
| Testing & polish | 1 hour |
| **Total Estimated** | **7-8 hours** |

---

## Appendix C: Validation Commands

After implementation, run these commands:

```bash
# Build the project
dotnet build

# Run tests
dotnet test

# Format code
dotnet format dlapp.csproj

# Build release
dotnet build -c Release

# Publish
dotnet publish -c Release -o release/win-x64
```

---

*Plan created: December 2025*
*Target framework: .NET 9.0 Avalonia UI 11.3.9*
