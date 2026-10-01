Pwrschdlr shuts down, restarts, puts to sleep, hibernates or signs out of your PC when a timer runs out, in a native Windows 11 app.

## Download

**{{FILE}}** for Windows 11, or Windows 10 version 1809 or later, x64.

SHA-256: `{{SHA256}}`

- **SmartScreen.** The installer isn't code-signed yet, so Windows may warn you. Check the hash with `Get-FileHash .\{{FILE}}`, then select **More info** > **Run anyway**.
- **No administrator approval.** Setup installs Pwrschdlr for your account only, under `%LOCALAPPDATA%\Programs`.
- **Provenance.** GitHub attests that this installer was built by this repository's release workflow: `gh attestation verify {{FILE}} --repo {{REPOSITORY}}`.

## What's new

{{CHANGES}}

## Verification

The [release build]({{RUN_URL}}) passed all {{TESTS}} unit tests before it built this installer. The README describes [how Pwrschdlr is verified]({{SERVER}}/{{REPOSITORY}}#verification), including what is checked by hand.

## Limitations

See [Limitations]({{SERVER}}/{{REPOSITORY}}#limitations) in the README.

[Terms of Use]({{SERVER}}/{{REPOSITORY}}/blob/{{TAG}}/TERMS.md) · [Privacy Statement]({{SERVER}}/{{REPOSITORY}}/blob/{{TAG}}/PRIVACY.md) · [Third-Party Notices]({{SERVER}}/{{REPOSITORY}}/blob/{{TAG}}/THIRD-PARTY-NOTICES.md)
