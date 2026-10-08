# Changelog

All notable changes to Pwrschdlr are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/),
and versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.0.0] - 2026-10-08

Pwrschdlr can now start the timer when something ends, and save a repeat that comes back on the days you choose. Its update notices are shorter and clearer, and it shows you what changed after an update.

### Added

- Wait for something to end instead of a fixed time. While Pwrschdlr is open, it watches for your downloads to finish
  (the PC has received less than a rate you choose for a number of minutes in a row), for an app with a window to
  close, or for nobody to use the PC (no mouse or keyboard input for a time you choose). It shows what it is reading
  while it waits, and when the thing you waited for happens the ordinary timer takes over, with its warning and the
  same chance to cancel.
- Repeat: a time and the days of the week, for example shut down on weekdays at 23:30. A per-user scheduled task
  opens Pwrschdlr for the warning before each occurrence, so it works with the window closed. The schedule stays
  until you remove it, and every occurrence is an ordinary timer you can cancel.
- After an update, the first start of the new version shows what changed in it.

### Changed

- The What's new notice shows only what changed, grouped New, Improved and Fixed, with the release date and one link
  to the full notes on GitHub, instead of the whole release notes with their download and verification details.

## [1.1.1] - 2026-10-07

### Fixed

- Uninstalling Pwrschdlr removes `%LOCALAPPDATA%\Pwrschdlr\Updates`, where an update installer that was
  downloaded but never run used to stay behind.

## [1.1.0] - 2026-10-05

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

[Unreleased]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v1.1.1...v2.0.0
[1.1.1]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/lukastojiljkovic/Power-Scheduler/releases/tag/v1.0.0
