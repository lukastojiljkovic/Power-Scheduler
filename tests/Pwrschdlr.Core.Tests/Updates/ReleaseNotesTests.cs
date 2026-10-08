using Pwrschdlr.Core.Updates;

namespace Pwrschdlr.Core.Tests.Updates;

public sealed class ReleaseNotesTests
{
    /// <summary>The body of the 1.1.1 release, exactly as GitHub published it.</summary>
    private const string RealBody = """
        Pwrschdlr shuts down, restarts, puts to sleep, hibernates or signs out of your PC when a timer runs out, in a native Windows 11 app.

        ## Download

        **Pwrschdlr-1.1.1-Setup.exe** for Windows 11, or Windows 10 version 1809 or later, x64.

        SHA-256: `A4102FD15F53E2768C07D21DF2BEB9970976877115E5B60B64D1DE5BE8904981`

        - **SmartScreen.** The installer isn't code-signed yet, so Windows may warn you. Check the hash with `Get-FileHash .\Pwrschdlr-1.1.1-Setup.exe`, then select **More info** > **Run anyway**.
        - **No administrator approval.** Setup installs Pwrschdlr for your account only, under `%LOCALAPPDATA%\Programs`.
        - **Provenance.** GitHub attests that this installer was built by this repository's release workflow: `gh attestation verify Pwrschdlr-1.1.1-Setup.exe --repo lukastojiljkovic/Power-Scheduler`.

        ## What's new

        ### Fixed

        - Uninstalling Pwrschdlr removes `%LOCALAPPDATA%\Pwrschdlr\Updates`, where an update installer that was downloaded but never run used to stay behind.

        ## Verification

        The [release build](https://github.com/lukastojiljkovic/Power-Scheduler/actions/runs/37702776381) passed all 100 unit tests before it built this installer. The README describes [how Pwrschdlr is verified](https://github.com/lukastojiljkovic/Power-Scheduler#verification), including what is checked by hand.

        ## Limitations

        See [Limitations](https://github.com/lukastojiljkovic/Power-Scheduler#limitations) in the README.

        [Terms of Use](https://github.com/lukastojiljkovic/Power-Scheduler/blob/v1.1.1/TERMS.md) · [Privacy Statement](https://github.com/lukastojiljkovic/Power-Scheduler/blob/v1.1.1/PRIVACY.md) · [Third-Party Notices](https://github.com/lukastojiljkovic/Power-Scheduler/blob/v1.1.1/THIRD-PARTY-NOTICES.md)
        """;

    /// <summary>A copy of this repository's changelog, as <see cref="ReleaseNotes.FromChangelog"/> reads it.</summary>
    private const string Changelog = """
        # Changelog

        All notable changes to Pwrschdlr are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/),
        and versions follow [Semantic Versioning](https://semver.org/).

        ## [Unreleased]

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

        [Unreleased]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v1.1.1...HEAD
        [1.1.1]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v1.1.0...v1.1.1
        [1.1.0]: https://github.com/lukastojiljkovic/Power-Scheduler/compare/v1.0.0...v1.1.0
        [1.0.0]: https://github.com/lukastojiljkovic/Power-Scheduler/releases/tag/v1.0.0
        """;

    [Fact]
    public void The_real_body_gives_only_its_whats_new_section()
    {
        var notes = ReleaseNotes.FromReleaseBody(RealBody);

        var section = Assert.Single(notes.Sections);
        Assert.Equal("Fixed", section.Title);
        var item = Assert.Single(section.Items);
        Assert.Contains("Uninstalling Pwrschdlr removes", item);
        Assert.Contains("downloaded but never run", item);

        foreach (var text in notes.Sections.SelectMany(section => section.Items))
        {
            Assert.DoesNotContain("SHA-256", text);
            Assert.DoesNotContain("SmartScreen", text);
            Assert.DoesNotContain("attestation", text);
            Assert.DoesNotContain("unit tests", text);
            Assert.DoesNotContain("Terms of Use", text);
        }
    }

    [Fact]
    public void A_body_without_a_whats_new_heading_is_parsed_whole()
    {
        var notes = ReleaseNotes.FromReleaseBody("* something fixed");

        var section = Assert.Single(notes.Sections);
        Assert.Equal(string.Empty, section.Title);
        Assert.Equal("something fixed", Assert.Single(section.Items));
    }

    [Fact]
    public void A_wrapped_bullet_is_one_item_joined_with_one_space()
    {
        var notes = ReleaseNotes.FromReleaseBody("## What's new\n\n- First line\n  continues here\n");

        Assert.Equal("First line continues here", Assert.Single(Assert.Single(notes.Sections).Items));
    }

    [Fact]
    public void Known_section_titles_use_user_words_and_unknown_ones_are_kept()
    {
        var notes = ReleaseNotes.FromReleaseBody(
            "## What's new\n\n### Added\n\n- a\n\n### Changed\n\n- b\n\n### Fixed\n\n- c\n\n### Notes\n\n- d\n");

        Assert.Equal(new[] { "New", "Improved", "Fixed", "Notes" }, notes.Sections.Select(section => section.Title));
        Assert.Equal(new[] { "a", "b", "c", "d" }, notes.Sections.SelectMany(section => section.Items));
    }

    [Fact]
    public void FromChangelog_reads_the_section_and_stops_before_the_next_version()
    {
        var notes = ReleaseNotes.FromChangelog(Changelog, new Version(1, 1, 1));

        Assert.NotNull(notes);
        var section = Assert.Single(notes!.Sections);
        Assert.Equal("Fixed", section.Title);
        var item = Assert.Single(section.Items);
        Assert.Contains("Uninstalling Pwrschdlr removes", item);
        Assert.Contains("downloaded but never run", item);
        Assert.DoesNotContain("checks GitHub", item);
    }

    [Fact]
    public void FromChangelog_renames_keep_a_changelog_words()
    {
        var notes = ReleaseNotes.FromChangelog(Changelog, new Version(1, 1, 0));

        Assert.Equal("New", Assert.Single(notes!.Sections).Title);
    }

    [Fact]
    public void FromChangelog_is_null_when_the_version_is_not_there()
    {
        Assert.Null(ReleaseNotes.FromChangelog(Changelog, new Version(2, 0, 0)));
        Assert.Null(ReleaseNotes.FromChangelog(Changelog, new Version(0, 1, 0)));
    }

    [Fact]
    public void Crlf_gives_the_same_result_as_lf()
    {
        const string lf = "## What's new\n\n### Added\n\n- First line\n  continues here\n";
        var crlf = lf.Replace("\n", "\r\n");

        Assert.Equal(
            ReleaseNotes.FromReleaseBody(lf).Sections.Select(section => section.Title),
            ReleaseNotes.FromReleaseBody(crlf).Sections.Select(section => section.Title));
        Assert.Equal(
            Assert.Single(ReleaseNotes.FromReleaseBody(lf).Sections).Items,
            Assert.Single(ReleaseNotes.FromReleaseBody(crlf).Sections).Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Empty_input_gives_empty_notes(string? body)
    {
        var notes = ReleaseNotes.FromReleaseBody(body);

        Assert.Empty(notes.Sections);
        Assert.True(notes.IsEmpty);
    }
}
