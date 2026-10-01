# Pwrschdlr Terms of Use

Last updated: 1 October 2026

These terms apply to the Pwrschdlr application and installer published at
https://github.com/lukastojiljkovic/Power-Scheduler. Pwrschdlr's source code is licensed under the MIT License
(LICENSE). Nothing in these terms limits the rights the MIT License gives you for the source code.

By installing or using Pwrschdlr, you agree to these terms. If you don't agree, don't install or use it.

## 1. What Pwrschdlr does

Pwrschdlr runs a timer that shuts down, restarts, puts to sleep, hibernates or signs out of your PC when it runs out.
To keep the timer running while Pwrschdlr is closed, it adds a scheduled task to Windows Task Scheduler, which opens
Pwrschdlr again shortly before the end. Pwrschdlr then shows a warning that counts down, and does what the timer was
set to do unless you cancel or postpone it.

## 2. Your responsibility

- Save your work before the timer runs out. When Windows shuts down, restarts or signs out, apps close, and work you
  haven't saved can be lost. With **Close apps without asking** on, apps close even when they have unsaved work.
- Make sure nothing is running that a shutdown, restart or sign-out would harm, such as an update, a backup, a download,
  a file copy or a recording.
- Only use Pwrschdlr on PCs you're allowed to shut down. On a PC managed by an organization, or one that other people
  use at the same time, follow its policies.
- You're responsible for complying with the laws and agreements that apply to you.

## 3. Limits of Pwrschdlr

- Pwrschdlr asks Windows to do the action. Windows, an app or a policy can delay or block it, for example an app with
  unsaved work, a running update or a setting that turns off sleep or hibernation.
- The timer only runs while you're signed in to Windows. If your PC is off, asleep or signed out when the timer runs
  out, nothing happens, and Pwrschdlr tells you the next time it opens.
- Don't rely on Pwrschdlr where a missed or early action could cause harm or loss.

## 4. No warranty

Pwrschdlr is provided free of charge, "as is" and "as available", without warranty of any kind, express or implied,
including the warranties of merchantability, fitness for a particular purpose and non-infringement. You bear the entire
risk of using Pwrschdlr.

## 5. Limitation of liability

To the fullest extent permitted by applicable law, the author and contributors aren't liable for any damages arising
from the use of, or inability to use, Pwrschdlr. This includes loss of data, loss of profits, business interruption
and any direct, indirect, incidental, special or consequential damages, even if they were advised of their possibility.
Some jurisdictions don't allow certain liability to be excluded or limited, for example liability for intent or gross
negligence. Where that's the case, liability is limited to the extent the law allows.

## 6. Third-party components

The installer includes the Microsoft .NET runtime and the Microsoft Windows App SDK, which are licensed under their own
terms. Their license files are installed in the `licenses` folder next to Pwrschdlr.exe and listed in
THIRD-PARTY-NOTICES.md. By using Pwrschdlr, you also agree to those terms. Microsoft and the other licensors provide
their components "as is" and have no liability to you in connection with Pwrschdlr.

Pwrschdlr isn't affiliated with or endorsed by Microsoft. Windows is a trademark of the Microsoft group of companies.

## 7. Privacy

Pwrschdlr doesn't collect or send personal data, and it doesn't connect to the internet. See PRIVACY.md.

## 8. Changes

These terms may change in future releases. The terms included with a release apply to that release.

## Contact

Open an issue at https://github.com/lukastojiljkovic/Power-Scheduler/issues.
