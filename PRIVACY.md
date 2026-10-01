# Pwrschdlr Privacy Statement

Last updated: 1 October 2026

Pwrschdlr doesn't collect or send personal data to its author or anyone else. It has no accounts, telemetry,
analytics, crash reporting or ads, and it doesn't connect to the internet.

## What stays on your PC

- **Settings** and your last timer's choices are stored in the Windows registry under
  `HKEY_CURRENT_USER\Software\Pwrschdlr`.
- **The running timer** is stored under `HKEY_CURRENT_USER\Software\Pwrschdlr\Timer`: what it does, and when it started
  and runs out.
- **The scheduled task** named `Pwrschdlr timer-<your account's SID>` opens Pwrschdlr for the warning. It exists only
  while a timer is running, and runs without administrator rights.

Cancelling the timer, or letting it run out, removes the timer and the task. Uninstalling Pwrschdlr removes all of the
above for the account that runs the uninstaller.

## Other

- **Links** to GitHub and to Pwrschdlr's website open in your web browser, where GitHub's privacy statement applies.
- **Microsoft components.** The Microsoft Windows App SDK included with Pwrschdlr may collect diagnostic information as
  described in its license terms (in the `licenses` folder) and the Microsoft Privacy Statement at
  https://aka.ms/privacy. Any such data goes to Microsoft, not to Pwrschdlr's author.

## Contact

Open an issue at https://github.com/lukastojiljkovic/Power-Scheduler/issues.
