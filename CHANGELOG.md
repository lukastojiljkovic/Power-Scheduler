# Changelog

All notable changes to Pwrschdlr are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/),
and versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Pwrschdlr checks GitHub for a newer release when it starts (at most once a day) and from Settings, shows a banner
  with the release notes when one exists, and installs it after verifying the installer against the SHA-256 checksum
  published with the release. The automatic check can be turned off.

## [1.0.0] - 2026-10-01

The first release.

### Added

- A timer that shuts down, restarts, puts to sleep, hibernates or signs out of your PC, set for a delay or for a time
  of day.
- A countdown ring that shows the time left, and the countdown in the taskbar.
- Cancel or postpone the timer by 15 minutes at any time.
- The timer keeps running when Pwrschdlr is closed. Pwrschdlr opens again 30 seconds, 1, 2 or 5 minutes before the end
  and counts down in a warning, so you can still cancel or postpone.
- A timer that ran out while your PC was off, asleep or signed out is reported, never run late.
- An option to close apps without asking, so apps with unsaved work can't hold up a shutdown.
- Light and dark themes that follow Windows, or a theme you choose.

[Unreleased]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/lukastojiljkovic/Power-Scheduler/releases/tag/v1.0.0
