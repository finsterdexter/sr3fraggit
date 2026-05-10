# SR3Generator

A desktop character creation tool for **Shadowrun 3rd Edition**, built with Avalonia UI.

## What This Is

SR3Generator is a cross-platform desktop application that guides players through the Shadowrun 3rd Edition character creation process. It covers the full priority-based creation system including attributes, skills, magic, gear, contacts, and more, with real-time validation and a reactive UI.

## Architecture Overview

The solution is organized into 4 core projects plus test projects, with dependencies flowing inward from the UI to the data layer.

### Projects

| Project | Purpose |
|---------|---------|
| **SR3Generator.Data** | Core domain models (Character, Attributes, Skills, Gear, Magic, Contacts) |
| **SR3Generator.Database** | SQLite data access with Dapper ORM, static lookup tables for game rules |
| **SR3Generator.Creation** | `CharacterBuilder` fluent API with creation logic and validation |
| **SR3Generator.Avalonia** | Desktop UI built with Avalonia 11.3 using MVVM and DI |
| **SR3Generator.Creation.Test** | xUnit tests for character building |
| **SR3Generator.Database.Test** | xUnit tests for database queries |

### Dependency Graph

```
SR3Generator.Data (no dependencies)
    ↑
SR3Generator.Database (depends on Data)
    ↑
SR3Generator.Creation (depends on Database, Data)
    ↑
SR3Generator.Avalonia (depends on Creation, Database, Data)
```

### Key Patterns

- **Builder Pattern**: `CharacterBuilder` provides fluent, chainable methods for constructing a character with validation at each step.
- **MVVM**: The Avalonia UI uses CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`) for reactive bindings.
- **Service Facade**: `CharacterBuilderService` wraps the builder and broadcasts a `CharacterChanged` event so all tab ViewModels stay in sync.
- **Query/Handler Pattern**: CQRS-like data access (e.g., `ReadSkillsQuery` → `ReadSkillsQueryHandler`).
- **Options Pattern**: `DbConnectionFactory` uses `IOptions<DatabaseOptions>` for configuration.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 10.0

## Run the Desktop App

From the repository root:

```bash
dotnet run --project SR3Generator.Avalonia
```

This launches the Shadowrun 3rd Edition character generator.

## Run with Hot Reload (Watch Mode)

To automatically rebuild and reload while developing:

```bash
dotnet watch run --project SR3Generator.Avalonia
```

`dotnet watch` monitors source files and restarts the app when changes are detected.

## Useful Commands

```bash
dotnet build    # Build the solution
dotnet test     # Run all tests
```

## Publish (Single-File Executables)

Self-contained, single-file builds for distribution — no .NET runtime required:

```bash
dotnet publish SR3Generator.Avalonia -p:PublishProfile=win-x64
dotnet publish SR3Generator.Avalonia -p:PublishProfile=linux-x64
dotnet publish SR3Generator.Avalonia -p:PublishProfile=osx-x64
```

Output lands in `artifacts/<rid>/`.

### Linux AppImage

```bash
dotnet publish SR3Generator.Avalonia -p:PublishProfile=linux-x64-appimage
```

Requires [appimagetool](https://github.com/AppImage/AppImageKit) on `PATH`. The target builds an AppDir at `artifacts/appimage/AppDir/` and wraps it into `artifacts/appimage/SR3Generator-x86_64.AppImage`. If appimagetool isn't installed, the AppDir is left ready for manual wrapping.

### Linux ARM

Duplicate `linux-x64.pubxml` → `linux-arm64.pubxml` and swap the `RuntimeIdentifier`:

```xml
<RuntimeIdentifier>linux-arm64</RuntimeIdentifier>
```

## License

This project is a fan work for Shadowrun 3rd Edition. Shadowrun is a registered trademark of The Topps Company, Inc. This project is not affiliated with or endorsed by Catalyst Game Labs or The Topps Company.