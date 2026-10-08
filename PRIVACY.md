# Pwrschdlr Privacy Statement

Last updated: 8 October 2026

Pwrschdlr doesn't collect or send personal data to its author or anyone else. It has no accounts, telemetry,
analytics, crash reporting or ads. The only request Pwrschdlr makes on its own is the update check described below.

## What stays on your PC

- **Settings** and your last timer's choices are stored in the Windows registry under
  `HKEY_CURRENT_USER\Software\Pwrschdlr`.
- **The running timer** is stored under `HKEY_CURRENT_USER\Software\Pwrschdlr\Timer`: what it does, and when it started
  and runs out.
- **A repeating schedule**, if you save one, is stored under `HKEY_CURRENT_USER\Software\Pwrschdlr\Repeat`: what it
  does, the time of day and the days of the week.
- **The scheduled tasks** named `Pwrschdlr timer-<your account's SID>` and `Pwrschdlr repeat-<your account's SID>`
  open Pwrschdlr for the warning. The timer's task exists only while a timer is running; the repeat's stays until you
  remove the repeat. Both run without administrator rights.
- **A downloaded installer.** If you choose to update, the new installer is saved to
  `%LOCALAPPDATA%\Pwrschdlr\Updates` before it runs. A later download removes the earlier files from that folder.

Cancelling the timer, or letting it run out, removes the timer and its task. Removing a repeat removes its task.
Uninstalling Pwrschdlr removes the settings, the timer, the repeat and the update downloads for the account that runs
the uninstaller.

## What Pwrschdlr reads

The waits for something to end read three things, all on your PC, and nothing leaves it:

- **Network counters.** To wait for downloads to finish, Pwrschdlr reads the bytes received on your network adapters,
  and keeps a running total. This counts everything the PC receives, not only downloads.
- **Running apps.** To wait for an app to close, Pwrschdlr lists the apps running with a visible window and the path of
  their executable.
- **Input idle time.** To wait until nobody uses the PC, Pwrschdlr reads how long it has been since the last mouse or
  keyboard input.

Pwrschdlr reads these only while its window is open, once a second, and keeps nothing of them when it closes. None of
it is sent anywhere.

## Update check

When Pwrschdlr starts, and when you press **Check now** in **Settings** > **Updates**, it asks GitHub for the latest
release over HTTPS at `api.github.com/repos/lukastojiljkovic/Power-Scheduler/releases/latest`. The request carries
Pwrschdlr's version in its User-Agent header; GitHub sees your IP address and the usual connection metadata, and
GitHub's privacy statement applies. No other data is sent, and nothing about your PC is included. When you choose to
update, the installer is downloaded from GitHub's release servers, and the SHA-256 checksum published with the release
is verified before the installer is started. The automatic check runs at most once a day and can be turned off in
**Settings** > **Updates**.

## Other

- **Links** to GitHub and to Pwrschdlr's website open in your web browser, where GitHub's privacy statement applies.
- **Microsoft components.** The Microsoft Windows App SDK included with Pwrschdlr may collect diagnostic information as
  described in its license terms (in the `licenses` folder) and the Microsoft Privacy Statement at
  https://aka.ms/privacy. Any such data goes to Microsoft, not to Pwrschdlr's author.

## Contact

Open an issue at https://github.com/lukastojiljkovic/Power-Scheduler/issues.
