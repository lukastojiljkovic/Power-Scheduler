# Support

## Where to ask

| You want to | Go to |
| --- | --- |
| Report something that is broken | **Issues** — use the bug report form |
| Ask for a feature or a change | **Issues** — use the feature request form |
| Ask a question | **Issues** — open an issue |
| Report a security problem | **Not an issue.** See [SECURITY.md](SECURITY.md) |
| Read the licence or the terms | [LICENSE](LICENSE), [TERMS.md](TERMS.md) |
| Check what data leaves your device | [PRIVACY.md](PRIVACY.md) |

There is no Discussions tab. Issues and the issue forms are the only channel.

## Before opening an issue

- Say which version you are running. **Settings > About** shows it.
- Say which Windows version and build you are on (`winver`).
- Say what you did, what you expected, and what happened instead. If a timer
  was involved, say whether it was for a delay or a time of day, and which of
  the five actions it was set to.
- **Do not paste personal data**: no user names, no personal file paths, no
  account SIDs. If a line looks personal, replace it with `[redacted]`.

## What is in scope

The timer, the scheduled task it registers, the power actions and the source in
this repository. Whether Windows carries out an action depends on your settings
and on other apps; the README's Limitations section describes what Pwrschdlr
does not control.

The only network request Pwrschdlr makes is the update check against GitHub's
public Releases API; there is no telemetry. See [PRIVACY.md](PRIVACY.md).

The Microsoft .NET runtime and the Windows App SDK ship with the installer.
Report a problem in one of those to Microsoft.

## What is not in scope

- Support for a modified build, or for a build from a fork.
- A Windows setting or a policy that blocks sleep, hibernate, shutdown or
  sign-out. The README says what Pwrschdlr asks Windows to do.
- Response-time guarantees. This is a project maintained by one person, with no
  support contract and no paid tier.
