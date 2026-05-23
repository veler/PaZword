# Sprint 0 — AOT notes

> Maintained by Nova during T-0.2. Append-only ledger of every AOT-related decision,
> hint, suppression, or analyzer interaction we hit while bringing `PaZword.App` up
> on NativeAOT + WinUI 3 + WinAppSDK 2.1.

## Headline status

- `dotnet publish -c Release -r win-x64 src/PaZword.App` produces a working AOT
  binary (`PaZword.App.exe`, ~5.6 MB) with **zero AOT/trim warnings** and **zero
  errors** under `TreatWarningsAsErrors=true`.
- All MEDI registrations are explicit (no scanning).
- Page navigation is statically reachable (`RootFrame.Navigate(typeof(MainPage))`),
  so the AOT compiler keeps the page constructor without reflection hints.
- View-model resolution flows through `App.Services.GetRequiredService<T>()` with
  statically-known types — no `Activator.CreateInstance`, no open generics that
  require runtime instantiation.

## Hints in effect

None yet. No `DynamicDependency`, no `UnconditionalSuppressMessage`, no
`RD.xml`, no `JsonSerializerContext` (no JSON yet — `ApplicationData.LocalSettings`
handles theme persistence). The current code path is naturally AOT-friendly.

## Issues encountered (and fixes)

### 1. `MVVMTK0045` on field-based `[ObservableProperty]`

`CommunityToolkit.Mvvm` (8.4.0) raises `MVVMTK0045` for any field-based
`[ObservableProperty]` declaration in a WinRT scenario (WinUI 3 / UWP):

> The field … using [ObservableProperty] will generate code that is not AOT
> compatible in WinRT scenarios … a partial property should be used instead.

The recommended replacement is the C# 13 partial-property syntax
(`public partial string Status { get; set; }`). With the toolkit version
pinned in CPM (8.4.0) and `LangVersion=latest` from `Directory.Build.props`,
the partial-property generator did not emit an implementation in our
environment (`CS9248: Partial property … must have an implementation part`).

**Fix (sprint 0):** use the manual `ObservableObject.SetProperty(ref _f, value)`
pattern instead. It is unambiguously AOT-clean, requires no generator, and
produces no analyzer noise. See `MainPageViewModel.cs`.

**Follow-up for sprint 2:** revisit when we either (a) bump `CommunityToolkit.Mvvm`
to a release whose generator emits partial-property implementations in this
toolchain, or (b) confirm a `LangVersion=preview` is acceptable. Until then,
manual `SetProperty` is the team default for VM-side observable properties.

### 2. `Microsoft.Extensions.Logging.ILoggingBuilder.AddDebug()` missing

The `AddDebug` extension lives in the separate
`Microsoft.Extensions.Logging.Debug` package which is **not** listed in
`Directory.Packages.props`. PROJECT_BRIEF §3 names only
`Microsoft.Extensions.Logging`, so we did not add a new package.

**Fix:** removed the call. Sprint 2 will introduce a file sink and at that
point can also opt-in to the debug sink if useful for dev builds. Until then,
the `ILogger` pipeline is empty — `DiagnosticsService.LogStartup()` is a
no-op at runtime but the DI registration still proves the constructor-injection
contract.

### 3. WinUI XAML compiler requires `Platform=x64`

`dotnet build` against the solution without `-p:Platform=x64` produced
`WMC1509` ("No LocalAssembly parameter given during MarkupCompilePass2") and
`WMC9999` ("Object reference not set to an instance of an object") on the
WinUI Markup Compiler.

**Fix:** always pass `-p:Platform=x64` (or `ARM64`) when invoking
`dotnet build`/`publish` for `PaZword.App`. The CI workflow already does this
(`runtime: win-x64`). Documented here so future contributors don't trip on
the cryptic error.

## Library AOT-cleanliness audit (sprint 0 inventory)

| Project | `IsTrimmable` | AOT analyzer | Notes |
|---|---|---|---|
| `PaZword.Core` | true | on | Interface stubs + DiagnosticsService + IThemeService. Pure managed, no reflection. |
| `PaZword.Crypto` | true | on | Konscious.Argon2 1.3.1 — verified clean by Sage (T-0.6). |
| `PaZword.Kdbx` | true | on | Interface stubs only. |
| `PaZword.Migration` | true | on | Empty in sprint 0. |
| `PaZword.Sync` | true | on | Empty in sprint 0. |
| `PaZword.Localization` | true | on | Empty in sprint 0. |
| `PaZword.App` | false (AOT root) | on (via `PublishAot=true`) | Verified clean via `dotnet publish`. |

## Standing rules (enforced by reviewers)

1. `{x:Bind}` only — never `{Binding}` (reflection-based path resolution).
2. No `Activator.CreateInstance`, no `Assembly.GetType(string)`, no
   `Type.GetMethod(string)`. If a future feature needs dynamic dispatch, add
   the `DynamicallyAccessedMembers` attribute or wire a static factory.
3. No `BinaryFormatter`, no `XmlSerializer` over arbitrary graphs, no
   `Newtonsoft.Json`. Settings/JSON go through `System.Text.Json`
   source-generated `JsonSerializerContext` (to be added when first needed).
4. DI: every registration explicit and statically typed. No assembly
   scanning, no `services.AddScoped(typeof(IFoo<>), typeof(Foo<>))` with
   runtime-instantiated open generics.
5. CommunityToolkit.Mvvm: until partial-property generator emits cleanly in
   our toolchain, use manual `SetProperty(ref ..)` for observable properties.
   `[RelayCommand]` is fine and is in use.
6. Any new NuGet dependency must be re-validated by re-running
   `dotnet publish -c Release -r win-x64 src/PaZword.App` and confirming
   zero AOT/trim warnings.
