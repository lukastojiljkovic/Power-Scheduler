using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Pwrschdlr.Core.Updates;

namespace Pwrschdlr.Dialogs;

/// <summary>
/// Renders parsed release notes as a short, readable list: one block per
/// section, one row per item, with the small markdown subset a changelog uses
/// (bold, inline code and https links). No markdown package is involved.
/// </summary>
internal static class ReleaseNotesView
{
    private static readonly Regex Inline = new(
        @"`(?<code>[^`]+)`|\[(?<label>[^\]]+)\]\((?<url>[^)\s]+)\)|(?<bare>https://\S+)|(?<bold>\*\*(?<boldText>[^*]+)\*\*)",
        RegexOptions.Compiled);

    public static StackPanel Build(ReleaseNotes notes)
    {
        var root = new StackPanel { Spacing = 20 };
        if (notes.IsEmpty)
        {
            root.Children.Add(new TextBlock { Text = "This release has no notes.", TextWrapping = TextWrapping.Wrap });
            return root;
        }

        foreach (var section in notes.Sections)
        {
            if (section.Items.Count == 0)
                continue;

            var group = new StackPanel { Spacing = 8 };
            if (section.Title.Length > 0)
                group.Children.Add(new TextBlock
                {
                    Text = section.Title,
                    Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
                });
            foreach (var item in section.Items)
                group.Children.Add(Row(item));
            root.Children.Add(group);
        }
        return root;
    }

    /// <summary>A hanging bullet with the item beside it, so wrapped lines stay aligned.</summary>
    private static Grid Row(string item)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var bullet = new TextBlock
        {
            Text = "\u2022",
            Style = (Style)Application.Current.Resources["ReleaseNotesBulletStyle"],
        };
        Grid.SetColumn(bullet, 0);

        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
        AddInline(text, item);
        Grid.SetColumn(text, 1);

        grid.Children.Add(bullet);
        grid.Children.Add(text);
        return grid;
    }

    /// <summary>Markdown in the item: bold, code as plain text, and https links. Anything else stays text.</summary>
    private static void AddInline(TextBlock target, string text)
    {
        var index = 0;
        foreach (Match match in Inline.Matches(text))
        {
            if (match.Index > index)
                target.Inlines.Add(new Run { Text = text[index..match.Index] });

            if (match.Groups["code"].Success)
                target.Inlines.Add(new Run { Text = match.Groups["code"].Value });
            else if (match.Groups["bold"].Success)
                target.Inlines.Add(new Bold { Inlines = { new Run { Text = match.Groups["boldText"].Value } } });
            else
            {
                var url = match.Groups["url"].Success ? match.Groups["url"].Value : match.Groups["bare"].Value;
                var label = match.Groups["label"].Success ? match.Groups["label"].Value : url;
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                    target.Inlines.Add(new Hyperlink { NavigateUri = uri, Inlines = { new Run { Text = label } } });
                else
                    target.Inlines.Add(new Run { Text = label });
            }
            index = match.Index + match.Length;
        }
        if (index < text.Length)
            target.Inlines.Add(new Run { Text = text[index..] });
    }
}
