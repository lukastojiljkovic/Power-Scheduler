# Product

<!-- impeccable:product-schema 1 -->

## Platform
windows

## Users

People who want their PC to shut down, restart, sleep, hibernate or sign out on
a timer they set, and who want a visible countdown and a last chance to cancel.

## Product Purpose

Run one timer that performs a power action, keep that timer alive when the
window is closed, and warn before the action so the user can still cancel or
postpone it. Pwrschdlr asks Windows to do the action; it does not act on the
hardware itself.

## Positioning

The website puts it next to `shutdown /s /t 5400`: the same result in a window
the user can see, with a countdown and the ability to change their mind. Where
other timers are a background process, Pwrschdlr keeps the timer in Windows
Task Scheduler instead, so closing the app — or a crash — does not lose it, and
nothing of Pwrschdlr runs between the start and the warning. It never runs
elevated and never acts late.

## Operating Context

- Windows 11, or Windows 10 version 1809 or later, x64.
- Setup installs for the current user only, under `%LOCALAPPDATA%\Programs`, and
  needs no administrator approval.
- Starting a timer registers a one-time task in Windows Task Scheduler that
  runs as the current user with the least privileges, only while that user is
  signed in, and opens Pwrschdlr when the warning before the end begins. The
  task is removed when the timer ends or the user cancels it.
- The running timer and the settings live in
  `HKEY_CURRENT_USER\Software\Pwrschdlr`.
- The installer is not code-signed yet, so SmartScreen may warn on first run;
  each release publishes a SHA-256.
- Shut down, restart, sign out and hibernate use `shutdown.exe`; sleep uses the
  Windows power API, turning off the display on Modern Standby PCs.
- The only network request is the update check against GitHub's public Releases
  API, at most once a day, which can be turned off in Settings.

## Capabilities and Constraints

Capabilities, from the README:

- Five actions: shut down, restart, sleep, hibernate or sign out. An action the
  PC cannot do is greyed out with the reason.
- A delay (presets from 15 minutes to 4 hours, or a custom duration) or a time
  of day.
- A countdown ring and the time left shown in the taskbar.
- Cancel at any time, or postpone by 15 minutes.
- A warning that opens 30 seconds, 1, 2 or 5 minutes before the end, stays on
  top, and counts down, so the action can still be cancelled or postponed. The
  window can be closed and the timer keeps running.
- An option to close apps without asking, so an app with unsaved work cannot
  hold up a shutdown.
- Light and dark themes that follow Windows, or a theme chosen in Settings.
- A timer whose end passed while the PC was off, asleep or signed out is
  reported and never run late.

Constraints, from the README's *Limitations*:

- The timer only runs while the user is signed in.
- Windows, an app or a policy can delay or block the action.
- On Modern Standby PCs, sleep turns off the display and Windows decides when
  the PC actually sleeps.
- Timers can be up to 23 hours 59 minutes away.
- x64 only, and English only; tested on Windows 11, not on Windows 10.
- Uninstalling removes the settings and timer of the account that runs the
  uninstaller.

## Brand Commitments

- Free and open source under the MIT License.
- No telemetry, accounts, analytics, crash reporting or ads; the only request
  the app makes is the update check.
- Never runs elevated; it does only what the signed-in user can do.
- Never acts late: a timer whose end passed while the PC was off, asleep or
  signed out is reported, not run.
- Plain language: the interface says what happens to the PC and to unsaved
  work, in words anyone can follow.
- No invented numbers: the README and the website claim only what the app does.
- English only, in the interface, the code and the documentation.
- The name: "a power scheduler without the vowels".

## Evidence on Hand

- Screenshots in [docs/images](docs/images/): `countdown-light.png`,
  `countdown-dark.png`, `setup-light.png`, `setup-dark.png`,
  `warning-light.png`, `warning-dark.png`.
- The product site under [site/](site/) (`index.html`, `site.css`, `llms.txt`),
  published to GitHub Pages by `.github/workflows/pages.yml` at
  https://lukastojiljkovic.github.io/Power-Scheduler/.
- Signed releases with a SHA-256: `.github/workflows/release.yml` publishes the
  installer and its checksum, and the app verifies the checksum before it runs
  an update.
- CI gates in `.github/workflows/ci.yml`: a `dotnet format --verify-no-changes`
  check, the unit tests, the installer build, and a silent install / uninstall
  pass that checks the uninstaller removes the settings and the timer's task.
- The xUnit suite in `tests/Pwrschdlr.Core.Tests`, which covers the timer
  phases, postponing, the countdown and its daylight-saving behaviour, the
  registry store, the `shutdown.exe` arguments and a round trip through Task
  Scheduler.
- [CHANGELOG.md](CHANGELOG.md), in Keep a Changelog format.

Not on hand: no user counts or other usage metrics, no testimonials, no
performance benchmarks, and no Windows 10 test results. Do not fabricate them.

## Product Principles

- Do one thing: one timer, one window, five actions.
- Ask Windows to act; never act beyond what the user can do.
- Never act late, and say so when a timer was missed.
- Show the time left and give a real last chance to cancel.
- Keep the timer in Windows itself, so closing the app does not lose it.
- Prefer plain language over jargon and over clever wording.

## Accessibility & Inclusion

- Light and dark themes, with a follow-Windows option.
- The countdown is a large, high-contrast number set at 60px, not only a ring,
  so the time left is readable as text.
- Actions a PC cannot perform are disabled with a written reason rather than
  hidden.
- The interface is English only, and x64 only.
