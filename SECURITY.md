# Security Policy

## Supported versions

Security fixes go into the latest release. Update to it before reporting.

## Reporting a vulnerability

Please don't open a public issue. Report it privately through
[GitHub's private vulnerability reporting](https://github.com/lukastojiljkovic/Power-Scheduler/security/advisories/new),
with the steps to reproduce it, the Pwrschdlr and Windows versions, and the impact you expect.

You'll get a reply in the advisory. Once a fix is released, the advisory is published with credit to you, unless you
prefer otherwise.

## What's in scope

Pwrschdlr never runs as administrator. It installs for the current user under `%LOCALAPPDATA%\Programs`, and it can
only do what that user can already do. These parts matter most:

- **The scheduled task.** It runs `Pwrschdlr.exe --due <timer ID>` as the current user, with the least privileges, and
  only while that user is signed in. Anything that lets the task run as another user, with more privileges or with a
  different program is a vulnerability.
- **Command-line modes.** `--due` only shows the warning for the running timer, and never acts without one. `--uninstall`
  only removes Pwrschdlr's own task and registry key. Anything that makes either one do something else is a
  vulnerability.

Out of scope: anything that requires an attacker who can already run code as the same user, and problems in Windows,
`shutdown.exe` or Task Scheduler. Report those to Microsoft.
