# Developing QuickPane

This guide covers building, testing, and releasing this fork. The README describes what QuickPane does and how its parts fit together. This file covers how to work on it safely.

## Requirements

- Windows 10 or 11, x64.
- Visual Studio or the Visual Studio Build Tools, 2019 or later, with MSBuild and the .NET desktop build tools.
- The .NET 8 SDK, which runs the unit tests.
- The .NET Framework 4.8 targeting pack is optional. `scripts/build.ps1` downloads Microsoft's signed reference-assembly package when the pack is missing.
- Inno Setup, only to build the installer.

## Build

```powershell
pwsh -File scripts/build.ps1                      # Release, x64
pwsh -File scripts/build.ps1 -Configuration Debug
```

The script finds MSBuild through `vswhere`, builds `QuickPane/QuickPane.csproj`, and prints the output path, version, and SHA-256. The exe lands in `QuickPane\bin\x64\<Configuration>\QuickPane.exe`.

The script builds the app project, not `QuickPane.sln`. Visual Studio 2019's MSBuild cannot load the SDK-style test project, so the test project stays out of the solution. With Visual Studio 2022 you may still open the solution as usual.

## Test

```powershell
pwsh -File scripts/test.ps1            # builds, then runs the tests
pwsh -File scripts/test.ps1 -NoBuild   # tests only
```

The tests live in `tests/QuickPane.Tests`, an SDK-style `net48` NUnit project. It references the built `QuickPane.exe` through a `HintPath` rather than a project reference, because the .NET SDK cannot build the legacy WPF project. Build the app first with the same configuration.

`[assembly: InternalsVisibleTo("QuickPane.Tests")]` lets tests reach `internal` code. Logic worth testing should be `internal`, not `private`.

### Rules for tests

- A test must only touch a temporary directory that it creates and deletes. `TempDir.cs` provides one.
- Never use `SettingsStore`'s default constructor in a test. It reads and writes the real `%APPDATA%\QuickPane`. Use `new SettingsStore(tempDir)`, and point `groupsPath` inside the temporary directory too.
- Never call `GroupStore.Recycle` or `DeleteTab` on real folders. Either would put test folders in the developer's Recycle Bin.
- Never start `QuickPane.exe` from a test, and never show a window or dialog.
- Prefer pulling a decision out into a pure `internal static` method and testing that. `DialogPaneWatcher.ResolveMode` is an example.

## CI

`.github/workflows/ci.yml` runs on every push to `main` and on every pull request. It calls the same two scripts on `windows-latest` and keeps the built exe as a workflow artifact. CI cannot exercise the app itself: Explorer windows, window hooks, and file dialogs need a real desktop.

`main` is protected. A change reaches it only through a pull request whose `build-and-test` check passed, and the rule applies to administrators too. No approving review is required, because GitHub does not let authors approve their own pull requests. Force-pushes to `main` and deleting `main` are blocked, and merged branches are deleted automatically. The rule lives in the repository's Settings, under Branches.

## Running a development build

Only one copy of QuickPane runs at a time, because a single-instance mutex guards it. A development build therefore replaces the installed copy while it runs.

1. Back up `%APPDATA%\QuickPane`. A development build reads and writes the same settings and groups.
2. Close the installed copy: `& "$env:LOCALAPPDATA\QuickPane\QuickPane.exe" --exit`. Do not kill the process. A kill skips the layout restore, and the watchdog process restarts the app.
3. Run `QuickPane\bin\x64\Release\QuickPane.exe`.
4. When finished, close it with `--exit` and start the installed copy again. `ShortcutRepair` points the Start menu shortcut at whichever copy ran last, so restarting the installed copy repairs it.

Windows Sandbox makes a safe test bed for UI checks. It is a disposable desktop with no personal files, where scripted clicks cannot disturb your own windows.

## Code conventions

- **C# 7.3 and .NET Framework 4.8 only.** That rules out switch expressions, `is not` patterns, nullable reference types, `using` declarations, ranges, records, and target-typed `new()`.
- **The app project lists every file.** A new `.cs` file needs a `<Compile Include="..." />` line in `QuickPane/QuickPane.csproj`, or it will not compile.
- **Comments explain why.** The codebase documents the reason behind each non-obvious choice, often a past bug. Keep that style.
- **One change per commit.** Each fix should be one commit that can be cherry-picked onto a branch cut from upstream.
- **No personal data.** Use invented paths such as `C:\Demo\Projects` in tests, comments, issues, and commit messages.

## Traps in the code

| Area | What to know |
|---|---|
| Inside mode | `ExplorerWindow` hosts the pane as a child window of Explorer's `CabinetWClass` and shifts the tab content right with `SetWindowPos`. |
| Windows 11 tabs | Each Explorer tab has its own `ShellTabWindowClass` child. All of them stay visible, with the active one on top, and Explorer resets every tab to full width when a tab opens or closes. `ExplorerWindow` therefore walks all of them live on every layout pass. Never cache one tab's handle, because tabs open and close while the window lives. See #4. |
| Keyboard focus | The embedded pane is a child of another process's window, so it cannot take keyboard focus normally. Typing happens in separate top-level windows such as `TextPrompt`. See lso2/QuickPane#4. |
| File dialogs | `DialogPaneWatcher` attaches panes to standard `#32770` dialogs, inside or beside. An app whose dialog fights the inside layout is moved to beside for the rest of the session. QuickPane's own pickers receive a pane too. |
| Folder picker | WinForms `FolderBrowserDialog` on .NET Framework 4.8 is the legacy tree dialog. Use `UI/FolderPicker.cs`, which wraps the modern `IFileOpenDialog`. |
| Deleting | `GroupStore.Recycle` sends a folder to the Recycle Bin. Use it, never `Directory.Delete`, for anything that holds a user's pins. |
| Settings UI | The Settings tab bodies expose no controls to UI Automation, so screen readers and UI-test tools cannot see them. New controls should set `AutomationProperties.Name`. |
| Versions | `UpdateService.Current` reads the assembly version. `UpdateService.Display` spells any version the way its tag does, with four parts when the fourth is not zero. Use it, never `Version.ToString(3)`, which drops a fork build's fourth part. |
| Update check | `UpdateService` asks GitHub for this fork's `releases/latest`, which skips drafts and pre-releases. A 404 means no published release yet and reads as "Up to date". |

## Working with upstream

The `upstream` remote points at `lso2/QuickPane`, and `origin` points at this fork.

- **Sync:** `git fetch upstream`, then create a branch from `main`, run `git merge upstream/main` on it, and open a pull request. `main` accepts no direct pushes. Upstream ships large monthly commits, so expect conflicts where the fork has changed the same lines.
- **Report first:** a bug that affects upstream gets an upstream issue before or alongside the fork's fix.
- **Upstream pull requests:** cut a branch from `upstream/main` and cherry-pick only the commit for that change. Never offer the fork's `main`, which carries fork-only commits such as CI, the scripts, and these docs.
- **Fork-only commits:** the commit that points the update check at this fork must never go upstream. When merging upstream, keep the fork's repository in `UpdateService`'s two URLs.

## Releasing the fork

1. Set the version in `QuickPane/Properties/AssemblyInfo.cs` (both attributes) and in `Installer.iss`. Fork versions add a fourth number to the upstream base, for example 3.10.0.1.
2. Add a `CHANGELOG.md` entry with Added, Changed, and Fixed sections, linking upstream issues where they exist.
3. Merge to `main` through a pull request with green CI.
4. Build from `main` with `scripts/test.ps1`.
5. Zip `QuickPane.exe` and `QuickPane.exe.config`, and record the SHA-256 of each file in `SHA256SUMS.txt`.
6. Create a **draft** GitHub release for tag `v<version>` with the zip, the checksums, and notes taken from the changelog. A draft stays private and creates no tag. Check that the draft's tag name is `v<version>`, not `untagged-...`, or publishing creates the wrong tag.
7. Test the build on a real desktop, then publish the draft as a full release, not a pre-release. The in-app update check reads `releases/latest`, which never returns a pre-release.

The binaries are not code-signed. To install a fork build over an installed copy, close QuickPane with `--exit` and replace `QuickPane.exe` in `%LOCALAPPDATA%\QuickPane`. Add/Remove Programs keeps showing the installed version until an installer built from this fork is used.
