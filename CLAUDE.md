# Pwrschdlr

Native Windows 11 power scheduler (WinUI 3, unpackaged, self-contained .NET 10). One timer shuts down, restarts,
sleeps, hibernates or signs out of the PC. The timer lives in `HKCU\Software\Pwrschdlr\Timer` and in a per-user
scheduled task that opens the app with `--due <id>` when the warning starts, so it survives closing the app.

## Commands

```powershell
dotnet test --project tests/Pwrschdlr.Core.Tests           # unit tests (Microsoft.Testing.Platform, xUnit v3)
dotnet build src/Pwrschdlr -c Release -p:Platform=x64      # WinUI projects need an explicit Platform
dotnet run --project src/Pwrschdlr -p:Platform=x64         # Debug builds never run power actions (TimerService.DryRun)
dotnet format Pwrschdlr.slnx --verify-no-changes           # the CI lint step
.\build.ps1                                                # tests, publish, licenses, installer
```

## Layout

- `src/Pwrschdlr.Core`: all logic, no UI. `Schedule` (phases: Waiting, Warning, Up, Missed), `Timing` (next time of
  day with DST, countdown and wording), `ScheduleStore` (registry), `TimerTask` (Task Scheduler XML via schtasks),
  `Power` (capabilities and actions).
- `src/Pwrschdlr`: WinUI 3 app. `Program.cs` handles `--uninstall` and the single instance; `App.xaml.cs` handles
  `--due`. `Services/TimerService.cs` owns the timer; `MainWindow` runs the warning.
- `tests/Pwrschdlr.Core.Tests`: unit tests, including a real Task Scheduler round trip.

## Rules

- English only: code, UI text, docs, commits.
- Never act late: a timer that ran out while the PC was off, asleep or signed out is reported, never run.
- Never elevate. The installer is per-user (`PrivilegesRequired=lowest`), and the task runs with least privileges.
- Glyphs: use XAML entities or int code points with `char.ConvertFromUtf32`, never `\u` escapes of private-use
  characters.
- Git: plain `git commit` with the global identity, no co-author trailers or generated-by footers. Everything green
  before pushing.
