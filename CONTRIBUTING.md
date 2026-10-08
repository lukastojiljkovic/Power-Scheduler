# Contributing to Pwrschdlr

Thanks for helping. Bug reports and fixes are welcome.

## Before you start

- For a bug, open an issue with the steps to reproduce it, and your Pwrschdlr and Windows versions.
- For a new feature or a bigger change, open an issue first, so we can agree on the approach before you write code.
  Pwrschdlr does one thing on purpose, so features that make it harder to use are unlikely to be accepted.
- For a security problem, follow [SECURITY.md](SECURITY.md) instead of opening an issue.

## Building

You need Windows 10 version 1809 or later and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
The installer also needs [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
dotnet test --project tests/Pwrschdlr.Core.Tests              # unit tests
dotnet run --project src/Pwrschdlr -p:Platform=x64             # WinUI projects need an explicit platform
dotnet format Pwrschdlr.slnx --verify-no-changes               # the CI format check
.\build.ps1                                                    # tests, publish, licenses and the installer
```

**Debug builds never shut down, restart, sleep, hibernate or sign out.** When a timer runs out, they show what a
release build would do instead, so you can test the timer on the PC you work on. Everything else, including the
scheduled task and the warning, works as in a release build.

## Rules the code follows

- **English only**, in code, UI text, docs and commits.
- **No invented numbers.** The README and the website only claim what the app does.
- **Never act late.** A timer whose end passed while the PC was off, asleep or signed out is reported, never run.
- **Never run elevated.** Pwrschdlr does what the signed-in user can do, and nothing more.
- **Plain language.** UI text says what happens to the user's PC and their work, in words anyone can follow.
- **Tests first.** Core behavior changes come with a test that failed before the change.

## Pull requests

- Keep a pull request to one change, and describe what it changes and how you tested it.
- The format check, the tests and the build must pass. CI runs them on every pull request.
- Add a line to the *Unreleased* section of [CHANGELOG.md](CHANGELOG.md) for anything users notice. Pwrschdlr shows
  these lines in its update dialogs, so write each one as the user would describe the change, not as the code does:
  "Uninstalling Pwrschdlr also removes updates it downloaded but never installed", not the folder path.

By contributing, you agree that your contribution is licensed under the [MIT License](LICENSE).
