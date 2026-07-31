# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

NetSonar is a cross-platform (Windows/macOS/Linux, x64/arm64) Avalonia desktop network-diagnostics tool: multi-protocol service probes, network interface management, speed tests.

- TFM: `net10.0`, `LangVersion=preview`, `Nullable=enable`, signed assembly (`NetSonar.snk`).
- UI: Avalonia 12.1 (`$(AvaloniaVersion)` in `Directory.Build.props`) + SukiUI (Fluent), LiveCharts, Material.Icons, Svg.Controls, MarkdownViewer.
- MVVM: CommunityToolkit.Mvvm (source-gen `[ObservableProperty]` on `public partial` properties, `[RelayCommand]`). `INotifyPropertyChanging` support is **disabled** (`MvvmToolkitEnableINotifyPropertyChangingSupport=false`) — do not rely on it.
- DI: `Microsoft.Extensions.DependencyInjection`. Logging: ZLogger. Updates: Updatum + Octokit.
- Compiled bindings are on by default (`AvaloniaUseCompiledBindingsByDefault=true`); XAML must declare `x:DataType`.
- See `AGENTS.md` for the repo's allocation/performance rules (DotNext buffer writers, `stackalloc`, `SpanOwner<T>`/`MemoryOwner<T>`). Follow them when touching probe/network hot paths.

## Build / Run

The repo uses Nuke. Top-level scripts bootstrap a local dotnet if needed and forward args to `build/build.csproj`.

- `./build.cmd <target>` (Windows) / `./build.sh <target>` (Unix) / `./build.ps1 <target>`
- Targets (`build/Build.cs`): `Print`, `Clean`, `Restore`, `Compile` (default), `Publish`.
- For day-to-day dev just use `dotnet build` / `dotnet run --project src/NetSonar.Desktop` against the solution.
- `Publish` produces zips/MSI/.app/AppImage per RID into `artifacts/publish/`. Defaults to all six RIDs (`win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64`); override with `--rids "win-x64"`. Other params: `--configuration`, `--create-bundles`, `--keep-only-bundles`, `--bundle-all-arch`. MSI requires Windows host; AppImage requires Linux host; macOS `.app` codesigns only on macOS. Skipped silently otherwise.
- There is **no test project** in the solution.

Runtime args (handled via `ApplicationKit`): `--portable [level]`, `--profile-path <path>`, `--minimized`, `--crash-report <index>`.

## Solution layout

- `src/NetSonar/` — `NetSonar.Avalonia.csproj`. Library holding all app code, views, view-models, settings, network logic. Edits almost always go here.
- `src/NetSonar.Desktop/` — `WinExe` entry point. Nearly empty; references `NetSonar.Avalonia` and copies native binaries from `dependencies/<rid>/`, `dependencies/<runtime-family>/` (e.g. `osx`), and `dependencies/win*` (design-time fallback) into `binaries/`. Currently `speedtest` and `gsudo`.
- `src/NetSonar.MsiInstaller/` — WiX installer (Windows only).
- `build/` — Nuke build project.
- `Directory.Build.props` — central `<Version>`, package metadata, `$(AvaloniaVersion)`, OS-detection props (`IsWindows`/`IsOSX`/`IsLinux`) and `DefineConstants` (`_WINDOWS_`/`_OSX_`/`_LINUX_`).

## StageKit

`StageKit` (NuGet) supplies the app-shell infrastructure that would otherwise look hand-rolled. Check it before reimplementing:

- `ApplicationKit` — profile/config paths (`ConfigsDirectoryName = "settings"`), `ParseProfilePathFromArgs()`, `ApplicationArgs`, `HasCrashReportFlag`/`CrashReportIndex`/`CrashReport`.
- `EntryApplication` — assembly product/version/repository metadata (used all over `App.Information.cs`).
- `UnhandledExceptions` — AppDomain/TaskScheduler hooks, Avalonia safe-exception filtering, `BeforeForcedExit`.
- `CrashReportsFile` — persisted crash store (`crash_reports.json`).
- `ApplicationInstanceGuard.AcquirePerUser()` — single-instance guard (replaces the old named `Mutex`).
- `RootSettingsFile<T>` / `SubSettings` — the JSON settings base types.

## App architecture

`App` is one class split across `App.*.cs` partials in `src/NetSonar/`:

- `App.axaml.cs` — lifecycle: `Initialize` → `OnFrameworkInitializationCompleted`. Branches on `ApplicationKit.HasCrashReportFlag` to show `CrashReportDialogView` in a `GenericWindow`; otherwise acquires `ApplicationInstanceGuard` (**bypassed under `#if DEBUG`** — the guard code is unreachable in Debug builds) and falls back to `InstanceAlreadyRunningDialogView`. Builds `MainWindow` from `MainViewModel` via the view registry, hooks `desktop.Exit += DesktopOnExit`.
- `App.Views.cs` — DI/view-registry bootstrap (`SetupViews`, `ConfigureViews`, `ConfigureServices`).
- `App.Globals.cs` — shared statics: `RuntimeGlobals`, `AppSettings`, `HttpClient` (redirect-following, decompressing `SocketsHttpHandler`), `JsonSerializerOptions` (IP address/endpoint + enum converters).
- `App.Settings.cs` — `PanicSaveSettings()`.
- `App.Updater.cs`, `App.Logger.cs`, `App.Theme.cs`, `App.Information.cs`, `App.Messages.cs`, `App.Resources.cs`, `App.Utilities.cs` — focused responsibilities; open the matching partial instead of grepping the whole class.

### View ↔ ViewModel resolution

Pattern lives in `Common/AppViews.cs` + `Common/ViewLocator.cs`:

- Register pairs in `App.Views.cs` via `.AddView<TView, TViewModel>(services)`. This maps VM-type → View-type and registers the VM in DI.
- VMs deriving from `PageViewModelBase` are registered as `AddSingleton(typeof(PageViewModelBase), viewModelType)` so the navigation host can `GetServices<PageViewModelBase>()` to enumerate pages. Others are singletons of their own type.
- `ViewLocator` is added to `Application.DataTemplates` and matches any `ObservableObject`. It caches built controls per-VM-instance, so VMs are effectively bound 1:1 to a single Control instance.
- New page/dialog: create `FooPage.axaml(.cs)` + `FooPageModel.cs` (`PageViewModelBase` for pages, `ViewModelBase`/`DialogViewModelBase` otherwise) and add one `.AddView<FooPage, FooPageModel>(services)` line in `App.Views.cs`.

### Network probes

- `Network/ServiceProtocolType.cs` enumerates the supported probes: ICMP, TCP, UDP, TLS, DNS, NTP, HTTP, WebSocket, SSH, SMTP, IMAP, MQTT, STUN, SIP.
- `Network/BasePingableCollectionObject<T>` (~1k lines) holds protocol-agnostic state: DNS resolution, `ObservableList<T> Pings` history, counters/percentages/streaks, `IsBusy`, `CanTimerExecute`, `PingEverySeconds`/`TimeoutSeconds`, `PingStarted`/`PingCompleted` events, and the `Ping`/`PingAsync` wrappers around abstract `PingCore`/`PingCoreAsync`.
- `Network/PingableService.cs` (~1.6k lines) is the single concrete implementation. It owns per-protocol defaults (`GetDefaultPort`, packet lengths, tcp/udp classification, `CanUseBufferSize`/`CanUseTtl`/`CanUseDontFragment`) and the `Create*Request` / `ReceiveAndValidate*` / `Validate*` method families. **Adding a protocol means touching all of these families plus the enum, plus README's protocol table and the text-list scheme parsing.** Probes validate an actual protocol response — a bare connect/send is not treated as success.
- Scheduling: `PingableServicesPageModel` runs a 500 ms `System.Timers.Timer`; each tick selects services where `CanTimerExecute` and dispatches via `Parallel.ForEach(..., App.GetParallelOptions(), ...)`. A slow service must not delay others, and overlapping probes for the same service are blocked by `IsBusy`.
- `Network/BaseProvider.cs` + `DnsProvider.cs` hold the public-endpoint catalogues imported from the Add Ping Services dialog.
- `Network/NetworkInterfaceBridge.cs` wraps interface enumeration/config; `SpeedTestService.cs` shells the bundled `speedtest` CLI via ProcessX.

### Collections

Uses Cysharp `ObservableCollections`, not `System.Collections.ObjectModel` (the old `FastObservableCollection` and MintPlayer deps are gone). Backing stores are `ObservableList<T>`/`ObservableListExtended<T>`; UI-bound views are created with `.ToNotifyCollectionChanged(...)` / `.ToNotifyCollectionChangedSlim(SynchronizationContextCollectionEventDispatcher.Current)`. Prefer `ZLinq`'s `.AsValueEnumerable()` over LINQ in hot/enumeration-heavy paths.

### Settings persistence

Three independent JSON files, each a StageKit settings singleton: `AppSettings` (composed of `SubSettings` partials — `PingServicesSettings`, `NetworkInterfacesSettings`, `SpeedTestSettings`), `PingableServicesFile` (services + optional resilient ping-reply history, gated on `AppSettings.Instance.PingServices.ResilientReplies`), and `SpeedTestsFile`. `App.PanicSaveSettings()` saves all four stores and is invoked from `DesktopOnExit` and `UnhandledExceptions.BeforeForcedExit` — UI code that already triggers a normal shutdown should **not** also call it (double-save). Exit via `IClassicDesktopStyleApplicationLifetime.Shutdown()` rather than `Environment.Exit` so this path runs.

### Crash reports

Unhandled exceptions route through `UnhandledExceptions` into `CrashReportsFile`, then relaunch the app with `--crash-report <index>` so the dialog runs in a clean process.

## Conventions

- Any new XAML control: include `x:DataType` (compiled bindings are on by default).
- XAML formatting is governed by `Settings.XamlStyler` at the repo root.
- Native external binaries live under `dependencies/<rid>/` (or `<family>/`) and are copied to `binaries/` by `NetSonar.Desktop.csproj` — add new ones there, not via code.
- Use the OS define constants (`_WINDOWS_`/`_OSX_`/`_LINUX_`) for platform-specific code paths; runtime checks live in `SystemOS/`.
- User-visible protocol behavior is documented in `README.md`'s protocol table and `CHANGELOG.md` — keep both in sync when probe semantics change.