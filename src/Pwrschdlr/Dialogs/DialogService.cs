using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pwrschdlr.Core;
using Pwrschdlr.Services;

namespace Pwrschdlr.Dialogs;

internal enum WarningChoice
{
    Cancel,
    Postpone,
    Now,
    TimeUp,
    Missed,
}

/// <summary>The app's dialogs, shown one at a time over the window's content.</summary>
internal sealed class DialogService(FrameworkElement root)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ContentDialog? _open;

    /// <summary>Waits for any open dialog first, since only one ContentDialog can be open at a time.</summary>
    public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
    {
        await _gate.WaitAsync();
        try
        {
            _open = dialog;
            dialog.XamlRoot = root.XamlRoot;
            dialog.RequestedTheme = root.ActualTheme;
            dialog.Style ??= (Style)Application.Current.Resources["DefaultContentDialogStyle"];
            return await dialog.ShowAsync();
        }
        finally
        {
            _open = null;
            _gate.Release();
        }
    }

    /// <summary>The last chance to stop the timer. It counts down to the end and then closes by itself.</summary>
    public async Task<WarningChoice> WarnAsync(Schedule schedule, string message, string postponeLabel)
    {
        // The warning can't wait behind another dialog, such as the welcome screen.
        _open?.Hide();

        var info = Actions.Get(schedule.Action);
        var dialog = new ContentDialog
        {
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = $"{info.Name} now",
            SecondaryButtonText = postponeLabel,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        WarningChoice? ended = null;
        var clock = root.DispatcherQueue.CreateTimer();
        clock.Interval = TimeSpan.FromMilliseconds(250);
        clock.Tick += (_, _) => Update();
        Update();
        clock.Start();
        try
        {
            var result = await ShowAsync(dialog);
            return ended ?? result switch
            {
                ContentDialogResult.Primary => WarningChoice.Now,
                ContentDialogResult.Secondary => WarningChoice.Postpone,
                _ => WarningChoice.Cancel,
            };
        }
        finally
        {
            clock.Stop();
        }

        void Update()
        {
            var now = DateTimeOffset.Now;
            dialog.Title = $"{info.Ongoing} in {Timing.Countdown(schedule.Remaining(now))}";
            ended ??= schedule.PhaseAt(now, TimerService.Warning) switch
            {
                TimerPhase.Up => WarningChoice.TimeUp,
                // The PC slept through the end, for example because its lid was closed during the warning.
                TimerPhase.Missed => WarningChoice.Missed,
                _ => null,
            };
            if (ended is not null)
                dialog.Hide();
        }
    }
}
