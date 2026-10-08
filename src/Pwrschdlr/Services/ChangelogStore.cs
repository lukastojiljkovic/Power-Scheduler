using Pwrschdlr.Core.Updates;

namespace Pwrschdlr.Services;

/// <summary>
/// The changelog the app embeds at build time, so a first run after an update
/// can show what changed without asking GitHub. A missing or unreadable
/// resource is simply no notes.
/// </summary>
internal static class ChangelogStore
{
    private const string ResourceName = "CHANGELOG.md";

    public static ReleaseNotes? ForVersion(Version version)
    {
        var stream = typeof(ChangelogStore).Assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
            return null;

        try
        {
            using (stream)
            using (var reader = new StreamReader(stream))
                return ReleaseNotes.FromChangelog(reader.ReadToEnd(), version);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            return null;
        }
    }
}
