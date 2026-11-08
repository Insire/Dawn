# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Dawn is a desktop utility that updates a deployment directory's contents while automatically backing up every file it overwrites, so earlier states can be restored later. Users drag files/folders/zips onto the window, stage them, then apply them as a named backup. Configuration can be injected via CLI args (`json=<base64>`, `url=<base address>`) or a local `Dawn.Wpf.Settings.json` next to the executable (see `ConfigurationService.Get()`).

Target: .NET 10 (SDK pinned in `build.config`). There is no test project.

## Commands

```sh
# Day-to-day (from repo root)
dotnet build Dawn.sln
dotnet run --project src/Dawn/Dawn.Avalonia/Dawn.Avalonia.csproj   # cross-platform app
dotnet run --project src/Dawn/Dawn.Wpf/Dawn.Wpf.csproj             # Windows only (net10.0-windows)

# Format XAML (also run by the build scripts before building)
dotnet tool restore
dotnet xstyler -d ./src -r -c .XamlStyler

# Release build (Cake Frosting, src/build): clean → assembly info → licenses → publish → zip into ./binaries
./build.ps1                      # Windows
./build.sh                       # Linux/macOS
dotnet run --project src/build/Build.csproj -- --target=Build   # any single task by name
```

Cake tasks (`src/build/Tasks`): `Default` → `Package` → `Build` → `CleanSolution`, `UpdateAssemblyInfo`, `GenerateLicenseFile`, `BuildWpf`/`BuildAvalonia`. WPF tasks only run on Windows. Both apps publish as self-contained single-file `win-x64`. The build's working directory is set to the repo root (`UseWorkingDirectory("../../")`).

## Architecture

Three projects under `src/Dawn`:

- **Dawn.Core** — UI-framework-agnostic view models, models and services, organized by feature folder (`Features/Backups`, `Staging`, `ChangeDetection`, `Configuration`, `Filesystem`, `Logging`, `About`) plus `ShellViewModel`. Built on MvvmScarletToolkit (`BusinessViewModelListBase`, `IScarletCommandBuilder`, etc.) and CommunityToolkit.Mvvm. Common namespaces are imported in `GlobalUsings.cs`. Anything platform-specific is behind an interface (`IFileSystem`, `IFileDialogs`, `IClipboardService`) implemented in the UI projects.
- **Dawn.Avalonia** — the cross-platform UI (active development on the `avalonia_version` branch). Avalonia 12 with the SukiUI 7 theme, Font Awesome icons as `PathIcon` geometries (`Infrastructure/Icons.axaml`), compiled bindings on by default, HotAvalonia for hot reload in Debug. Views mirror Core's feature folders under `Features/`.
- **Dawn.Wpf** — the original Windows UI (AdonisUI, MahApps icons). Consumes the same Dawn.Core.

Each UI project has its own `CompositionRoot.cs` that wires the DryIoc container: Serilog logger (file sink `./logs/log.txt`, plus `LogViewModel` as a live sink for the in-app log window), the Scarlet toolkit singletons (`Infrastructure/Scarlet*.cs` in Avalonia adapt the toolkit's dispatcher/command manager/etc. to Avalonia), and all Core view models as singletons. When adding a view model or service to Core, register it in each UI's composition root.

Avalonia specifics:
- `Infrastructure/ViewLocator.cs` maps view model types to views via an explicit dictionary — new item templates (view model → view) must be added there; there is no reflection-based convention lookup.
- `JsonFileStore` persists UI state such as window size/position (`App.axaml.cs`), stored under ApplicationData on Linux and CommonApplicationData elsewhere.
- `App.OnShutDownRequested` saves configuration and disposes the container.
- `App.OnFrameworkInitializationCompleted` applies the saved theme explicitly via `SukiTheme.ChangeBaseTheme`. Without it SukiUI 7 starts with unresolved theme brushes (black selected-tab text, title-bar buttons ignoring the mouse until the first theme toggle).
- Icons: add new glyphs as `StreamGeometry` entries in `Infrastructure/Icons.axaml` (Font Awesome Free SVG path data, key `Icon.<Name>`) and use `<PathIcon Data="{StaticResource Icon.<Name>}" />`. There is no icon library (Projektanker has no Avalonia 12 release). SukiUI's `WindowControlsButton` style overrides a global `PathIcon` size, so title-bar icons carry an explicit `Width`/`Height`.
- Keyboard: F1 About, Ctrl+, Configuration, Ctrl+L Log, Ctrl+T theme, Ctrl+Shift+T keep on top, Ctrl+O / Ctrl+Shift+O add files / folder, F5 refresh, Ctrl+Enter apply, Esc closes dialogs (each dialog exposes a `CloseCommand`), Enter confirms Edit Backup. Title-bar buttons are focusable and show a focus ring (style in `App.axaml`). Keep shortcuts in sync with the tooltips in `Shell.axaml`.
- Verifying UI on Linux (GNOME/Wayland, app runs through XWayland): the root window can't be screenshotted and `xdotool` can't move the mouse, but it can send keys and the app's own window can be captured by id (`ffmpeg -f x11grab -window_id`), so drive the UI with the keyboard shortcuts above.

Versioning/metadata: `src/Dawn/SharedAssemblyInfo.cs` is linked into both UI projects and rewritten by the `UpdateAssemblyInfo` Cake task; versions come from Nerdbank.GitVersioning (`version.json`). `Properties/licenses.json` in each UI project is generated by `nuget-license` during the build and embedded as a resource for the About window.

## Workflow

Do not create git commits (or push) unless explicitly asked. Leave committing to the user.

## Troubleshooting

`LibGit2SharpException: repository path ... is not owned by current user` during the build: run `git config --global --add safe.directory '<path to repo>'`.
