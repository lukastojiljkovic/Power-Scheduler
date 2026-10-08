using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pwrschdlr.Core.Updates;

namespace Pwrschdlr.Dialogs;

/// <summary>The update dialogs the window shows: what's new before and after an update, and the release page fallback.</summary>
internal static class UpdatePrompts
{
    /// <summary>
    /// Shows a release's notes and, when the update can start now, offers to
    /// update. Returns true when the user chose to update.
    /// </summary>
    public static async Task<bool> ShowReleaseNotesAsync(DialogService dialogs, ReleaseInfo release, bool canUpdate)
    {
        var dialog = new ContentDialog
        {
            Title = $"What's new in Pwrschdlr {release.Version.ToString(3)}",
            Content = NotesContent(release.PublishedAt, ReleaseNotes.FromReleaseBody(release.Body), release.PageUrl),
            PrimaryButtonText = "Update now",
            IsPrimaryButtonEnabled = canUpdate,
            CloseButtonText = "Later",
            DefaultButton = ContentDialogButton.Primary,
        };
        dialog.Resources["ContentDialogMaxWidth"] = 640.0;
        return await dialogs.ShowAsync(dialog) == ContentDialogResult.Primary;
    }

    /// <summary>Shows what changed in the version that was just installed.</summary>
    public static async Task ShowInstalledNotesAsync(DialogService dialogs, Version version, ReleaseNotes notes)
    {
        var url = $"https://github.com/lukastojiljkovic/Power-Scheduler/releases/tag/v{version.ToString(3)}";
        var dialog = new ContentDialog
        {
            Title = $"Pwrschdlr was updated to {version.ToString(3)}",
            Content = NotesContent(null, notes, url),
            CloseButtonText = "Got it",
        };
        dialog.Resources["ContentDialogMaxWidth"] = 640.0;
        await dialogs.ShowAsync(dialog);
    }

    /// <summary>Tells the user why the update stopped and offers the release page.</summary>
    public static async Task ShowUpdateFailureAsync(DialogService dialogs, string message, string? releasePageUrl)
    {
        var dialog = new ContentDialog
        {
            Title = "The update could not be installed",
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = "Close",
            SecondaryButtonText = releasePageUrl is null ? string.Empty : "Open the release page",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialogs.ShowAsync(dialog) == ContentDialogResult.Secondary && releasePageUrl is not null)
            await OpenAsync(releasePageUrl);
    }

    /// <summary>The notes and the one link to the full, technical release page, in a scrollable column.</summary>
    private static ScrollViewer NotesContent(DateTimeOffset? publishedAt, ReleaseNotes notes, string pageUrl)
    {
        var panel = new StackPanel();
        if (publishedAt is { } published)
            panel.Children.Add(new TextBlock
            {
                Text = $"Released on {published.ToLocalTime().ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}",
                Style = (Style)Application.Current.Resources["ReleaseNotesDateStyle"],
            });
        panel.Children.Add(ReleaseNotesView.Build(notes));
        panel.Children.Add(new HyperlinkButton
        {
            Content = "See the full release notes on GitHub",
            NavigateUri = Uri.TryCreate(pageUrl, UriKind.Absolute, out var uri) ? uri : null,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 20, 0, 0),
        });
        return new ScrollViewer
        {
            Content = panel,
            MaxHeight = 420,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
    }

    public static async Task OpenAsync(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            await Windows.System.Launcher.LaunchUriAsync(uri);
    }
}
