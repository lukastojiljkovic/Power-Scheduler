using System.Text;
using System.Text.RegularExpressions;

namespace Pwrschdlr.Core.Updates;

/// <summary>One titled group of release-note items, such as New or Fixed.</summary>
public sealed record ReleaseNotesSection(string Title, IReadOnlyList<string> Items);

/// <summary>
/// The part of a release an end user needs: short items grouped New, Improved
/// and Fixed, with no install or verification details. Built from the
/// <c>## What's new</c> section of a GitHub release body, or from the entries
/// of a Keep a Changelog document.
/// </summary>
public sealed record ReleaseNotes(IReadOnlyList<ReleaseNotesSection> Sections)
{
    /// <summary>True when nothing was written, or everything parsed away.</summary>
    public bool IsEmpty => Sections.All(section => section.Items.Count == 0);

    /// <summary>
    /// The notes a release body carries. A body with a <c>## What's new</c>
    /// heading exposes only that section, so the installer, verification and
    /// terms text written for the download page stays out of the app; a body
    /// without one is read whole.
    /// </summary>
    public static ReleaseNotes FromReleaseBody(string? body) =>
        Parse(Block(body, IsWhatsNewHeading, stopAtLinkReference: false) ?? Lines(body));

    /// <summary>
    /// The notes for one version in a Keep a Changelog document.
    /// <see langword="null"/> when the version has no section.
    /// </summary>
    public static ReleaseNotes? FromChangelog(string changelog, Version version)
    {
        var heading = $"## [{version.ToString(3)}]";
        var block = Block(changelog, line => IsVersionHeading(line, heading), stopAtLinkReference: true);
        return block is null ? null : Parse(block);
    }

    /// <summary>The lines between a heading and the next heading, link reference or the end.</summary>
    private static IReadOnlyList<string>? Block(string? text, Func<string, bool> isStart, bool stopAtLinkReference)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var lines = Normalise(text).Split('\n');
        var start = Array.FindIndex(lines, line => isStart(line));
        if (start < 0)
            return null;

        var block = new List<string>();
        for (var i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("## ", StringComparison.Ordinal))
                break;
            if (stopAtLinkReference && LinkReference.IsMatch(line))
                break;
            block.Add(line);
        }
        return block;
    }

    /// <summary>
    /// Turns a block into sections: <c>### Title</c> starts one, bullets and
    /// paragraphs become items, and a two-space indented line continues the
    /// item above it.
    /// </summary>
    private static ReleaseNotes Parse(IReadOnlyList<string> lines)
    {
        var sections = new List<ReleaseNotesSection>();
        var items = new List<string>();
        var builder = new StringBuilder();
        var title = string.Empty;
        var bullet = false;
        var comment = false;

        foreach (var raw in lines)
        {
            if (comment)
            {
                comment = !raw.Contains("-->", StringComparison.Ordinal);
                continue;
            }
            if (raw.TrimStart().StartsWith("<!--", StringComparison.Ordinal))
            {
                comment = !raw.Contains("-->", StringComparison.Ordinal);
                continue;
            }
            if (raw.StartsWith("### ", StringComparison.Ordinal))
            {
                CloseSection();
                title = raw[4..].Trim();
                continue;
            }
            // Level 1 and 2 headings inside the block carry no user content.
            if (raw.StartsWith('#'))
                continue;
            if (raw.Trim().Length == 0)
            {
                Flush();
                continue;
            }

            var isBullet = raw.StartsWith("- ", StringComparison.Ordinal) || raw.StartsWith("* ", StringComparison.Ordinal);
            var indented = raw.Length - raw.TrimStart(' ').Length >= 2;
            if (builder.Length > 0)
            {
                if (bullet && indented)
                {
                    Append(builder, raw.Trim());
                    continue;
                }
                if (!bullet && !isBullet)
                {
                    Append(builder, raw.Trim());
                    continue;
                }
                Flush();
            }

            builder.Append(isBullet ? raw[2..].Trim() : raw.Trim());
            bullet = isBullet;
        }
        CloseSection();
        return new ReleaseNotes(sections);

        void Append(StringBuilder target, string text)
        {
            if (target.Length > 0)
                target.Append(' ');
            target.Append(text);
        }

        void Flush()
        {
            if (builder.Length == 0)
                return;
            items.Add(builder.ToString());
            builder.Clear();
            bullet = false;
        }

        void CloseSection()
        {
            Flush();
            if (items.Count > 0)
                sections.Add(new ReleaseNotesSection(MapTitle(title), items.ToArray()));
            items.Clear();
            title = string.Empty;
        }
    }

    /// <summary>Keep a Changelog words become words an end user reads.</summary>
    private static string MapTitle(string title) => title.ToLowerInvariant() switch
    {
        "added" => "New",
        "changed" => "Improved",
        _ => title,
    };

    private static bool IsWhatsNewHeading(string line) =>
        line.Trim().Equals("## What's new", StringComparison.OrdinalIgnoreCase);

    private static bool IsVersionHeading(string line, string heading)
    {
        if (!line.StartsWith(heading, StringComparison.Ordinal))
            return false;
        var rest = line[heading.Length..];
        return rest.Length == 0
            || rest.StartsWith(" - ", StringComparison.Ordinal)
            || rest.StartsWith(" \u2014 ", StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> Lines(string? text) =>
        string.IsNullOrEmpty(text) ? [] : Normalise(text).Split('\n');

    private static string Normalise(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    private static readonly Regex LinkReference = new(@"^\[[^\]]+\]:\s*http", RegexOptions.Compiled);
}
