using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Pwrschdlr.Core;
using Pwrschdlr.Core.Conditions;
using Pwrschdlr.Services;
using Windows.Foundation;

namespace Pwrschdlr.Views;

/// <summary>Sets a timer, waits for something to end, saves a repeating schedule, or counts down a running timer.</summary>
public sealed partial class TimerView : UserControl
{
    private const double RingSize = 320;
    private const double RingThickness = 14;

    /// <summary>The download rates, quiet periods and idle times the choices offer, in the order of the boxes.</summary>
    private static readonly (string Label, double BytesPerSecond)[] Thresholds =
        [("100 KB/s", 100 * 1024), ("500 KB/s", 500 * 1024), ("1 MB/s", 1024 * 1024)];

    private static readonly int[] QuietPeriods = [2, 5, 10, 15];
    private static readonly int[] IdlePeriods = [10, 15, 30, 60];

    private readonly MainWindow _window;
    private readonly TimerService _timer;
    private readonly NetworkReader _network = new();
    private readonly RunningAppsReader _runningApps = new();
    private readonly InputIdle _input = new();
    private readonly List<ToggleButton> _days = [];
    private bool _loading = true;
    private DateTimeOffset _reading;

    public TimerView(MainWindow window)
    {
        _window = window;
        _timer = window.Timer;
        InitializeComponent();

        foreach (var info in Actions.All)
            ActionList.Items.Add(Tile(info));
        var unavailable = Actions.All.Select(info => Power.WhyUnavailable(info.Action)).OfType<string>().ToList();
        UnavailableText.Text = string.Join(" ", unavailable);
        UnavailableText.Visibility = unavailable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (var (label, _) in Thresholds)
            ThresholdBox.Items.Add(label);
        foreach (var minutes in QuietPeriods)
            QuietBox.Items.Add(Minutes(minutes));
        foreach (var minutes in IdlePeriods)
            IdleBox.Items.Add(Minutes(minutes));
        BuildDays();

        // Start from the last timer's choices.
        var last = Power.IsAvailable(AppSettings.LastAction) ? AppSettings.LastAction : PowerAction.ShutDown;
        ActionList.SelectedIndex = (int)last;
        HoursBox.Value = AppSettings.LastDelay.Hours;
        MinutesBox.Value = AppSettings.LastDelay.Minutes;
        AtPicker.SelectedTime = AppSettings.LastTime.ToTimeSpan();
        ThresholdBox.SelectedIndex = 0;
        QuietBox.SelectedIndex = 1;
        IdleBox.SelectedIndex = 2;
        DownloadsChoice.IsChecked = true;
        WhenBar.SelectedItem = AppSettings.LastWasAtTime ? AtItem : InItem;
        PostponeButton.Content = MainWindow.PostponeLabel;
        _loading = false;

        Loaded += (_, _) =>
        {
            _timer.Changed += OnTimerChanged;
            _timer.Ticked += OnTicked;
            _window.Conditions.Changed += OnConditionsChanged;
            _window.Repeat.Changed += OnConditionsChanged;
            Show();
        };
        Unloaded += (_, _) =>
        {
            _timer.Changed -= OnTimerChanged;
            _timer.Ticked -= OnTicked;
            _window.Conditions.Changed -= OnConditionsChanged;
            _window.Repeat.Changed -= OnConditionsChanged;
        };
    }

    private PowerAction SelectedAction => ActionList.SelectedItem is GridViewItem { Tag: PowerAction action } ? action : PowerAction.ShutDown;

    private bool AtTime => WhenBar.SelectedItem == AtItem;

    /// <summary>Waiting for something to end, rather than for a length of time or a time of day.</summary>
    private bool ForSomething => WhenBar.SelectedItem == ConditionItem;

    /// <summary>Whether the time of day is saved as a repeating schedule rather than set as a timer.</summary>
    private bool Repeats => AtTime && RepeatToggle.IsOn;

    private TimeSpan Delay => TimeSpan.FromHours(Whole(HoursBox)) + TimeSpan.FromMinutes(Whole(MinutesBox));

    private static int Whole(NumberBox box) => double.IsNaN(box.Value) ? 0 : (int)box.Value;

    private static GridViewItem Tile(ActionInfo info)
    {
        var tile = new GridViewItem
        {
            Tag = info.Action,
            IsEnabled = Power.IsAvailable(info.Action),
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new FontIcon { Glyph = info.Glyph, FontSize = 24 },
                    new TextBlock { Text = info.Name, HorizontalAlignment = HorizontalAlignment.Center },
                },
            },
        };
        AutomationProperties.SetName(tile, info.Name);
        return tile;
    }

    /// <summary>The day toggles, in the order the region starts its week, named as the region shortens them.</summary>
    private void BuildDays()
    {
        var culture = CultureInfo.CurrentCulture;
        for (var day = 0; day < 7; day++)
        {
            var current = (DayOfWeek)(((int)culture.DateTimeFormat.FirstDayOfWeek + day) % 7);
            var toggle = new ToggleButton
            {
                Content = culture.DateTimeFormat.AbbreviatedDayNames[(int)current],
                Tag = current,
                MinWidth = 52,
                // A bedtime habit is the usual case: Monday to Friday.
                IsChecked = current is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
            };
            AutomationProperties.SetName(toggle, culture.DateTimeFormat.DayNames[(int)current]);
            toggle.Checked += OnDayToggled;
            toggle.Unchecked += OnDayToggled;
            DaysRow.Children.Add(toggle);
            _days.Add(toggle);
        }
    }

    /// <summary>When the timer would run out if it started now, or <see langword="null"/> while nothing is set.</summary>
    private DateTimeOffset? Target(DateTimeOffset now) =>
        AtTime
            ? AtPicker.SelectedTime is { } time ? Timing.NextAt(TimeOnly.FromTimeSpan(time), now, TimeZoneInfo.Local) : null
            : Delay > TimeSpan.Zero ? now + Delay : null;

    private void Show()
    {
        var waiting = _window.Conditions.Running;
        var running = _timer.Current is not null;
        SetupPanel.Visibility = waiting || running ? Visibility.Collapsed : Visibility.Visible;
        RunningPanel.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        WaitingPanel.Visibility = waiting ? Visibility.Visible : Visibility.Collapsed;
        KeepsRunningBar.Message = $"The timer keeps running. Pwrschdlr opens again {AppSettings.WarningLabel} before it runs out, so you can still cancel.";
        Update();
    }

    private void Update()
    {
        var now = DateTimeOffset.Now;
        UpdateRepeatCard();
        if (_window.Conditions.Running)
            UpdateWaiting();
        else if (_timer.Current is { } schedule)
            UpdateRunning(schedule, now);
        else
        {
            UpdateSummary(now);
            UpdateReading(now);
        }
    }

    private void UpdateWaiting()
    {
        var status = _window.Conditions.Status;
        WaitingTitle.Text = status?.Waiting ?? string.Empty;
        WaitingReading.Text = status?.Reading ?? string.Empty;
        WaitingProgress.Text = status?.Progress ?? string.Empty;
        WaitingProgress.Visibility = status?.Progress is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateSummary(DateTimeOffset now)
    {
        var info = Actions.Get(SelectedAction);
        SummaryGlyph.Glyph = info.Glyph;
        StartButton.Content = Repeats ? "Save schedule" : "Start";
        UpdateConditionText();

        if (ForSomething)
        {
            var clause = Clause();
            StartButton.IsEnabled = clause is not null;
            SummaryText.Text = clause is null ? "Choose when" : $"{info.Future} when {clause}";
            SummaryDetail.Text = clause is null ? "Pick an app that is running." : "Pwrschdlr watches this while it is open.";
            return;
        }

        if (Repeats)
        {
            var days = SelectedDays();
            StartButton.IsEnabled = days.Count > 0;
            if (days.Count == 0)
            {
                SummaryText.Text = "Choose when";
                SummaryDetail.Text = "Pick at least one day.";
                return;
            }

            var time = RepeatTime();
            SummaryText.Text = $"{info.Future} {Repeat.DaysInWords(days, CultureInfo.CurrentCulture)} at {Clock(time)}";
            SummaryDetail.Text = $"Next: {MainWindow.When(Repeat.NextOccurrence(time, days, now, TimeZoneInfo.Local))}";
            return;
        }

        var target = Target(now);
        StartButton.IsEnabled = target is not null;
        if (target is { } next)
        {
            SummaryText.Text = $"{info.Future} {MainWindow.When(next)}";
            SummaryDetail.Text = $"In {Timing.Words(next - now)}";
        }
        else
        {
            SummaryText.Text = "Choose when";
            SummaryDetail.Text = "Set how long to wait, or pick a time.";
        }
    }

    private void UpdateRunning(Schedule schedule, DateTimeOffset now)
    {
        var info = Actions.Get(schedule.Action);
        RunningGlyph.Glyph = info.Glyph;
        RunningAction.Text = info.Name;
        RemainingText.Text = Timing.Countdown(schedule.Remaining(now));
        TargetText.Text = MainWindow.When(schedule.Target);
        DrawRing(schedule.Left(now));
    }

    /// <summary>The repeating schedule in words, with the next time it happens, when there is one.</summary>
    private void UpdateRepeatCard()
    {
        if (_window.Repeat.Current is not { } repeat)
        {
            RepeatCard.Visibility = Visibility.Collapsed;
            return;
        }

        var title = $"{Actions.Get(repeat.Action).Name} {repeat.DaysInWords(CultureInfo.CurrentCulture)} at {Clock(repeat.Time)}";
        var next = $"Next: {MainWindow.When(repeat.NextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local))}";
        if (RepeatTitle.Text != title)
            RepeatTitle.Text = title;
        if (RepeatNext.Text != next)
            RepeatNext.Text = next;
        RepeatCard.Visibility = Visibility.Visible;
    }

    /// <summary>The rule the chosen condition follows, and the reading it would watch.</summary>
    private void UpdateConditionText()
    {
        DownloadRule.Text = $"When your PC has downloaded less than {Thresholds[ThresholdBox.SelectedIndex].Label} for {Minutes(QuietPeriods[QuietBox.SelectedIndex])} in a row.";
        IdleRule.Text = $"When there has been no mouse or keyboard input for {Minutes(IdlePeriods[IdleBox.SelectedIndex])}.";
    }

    /// <summary>The rate downloads are coming in at, kept live so the choice shows what it would watch.</summary>
    private void UpdateReading(DateTimeOffset now)
    {
        if (!ForSomething || DownloadsChoice.IsChecked != true)
            return;
        if (now - _reading < TimeSpan.FromSeconds(1))
            return;

        _reading = now;
        DownloadReading.Text = ConditionWords.DownloadReading(_network.Read());
    }

    /// <summary>The ring empties counterclockwise towards twelve o'clock as the time runs out.</summary>
    private void DrawRing(double left)
    {
        const double radius = (RingSize - RingThickness) / 2;
        const double center = RingSize / 2;
        if (left >= 0.9999)
        {
            RingArc.Data = new EllipseGeometry { Center = new Point(center, center), RadiusX = radius, RadiusY = radius };
            return;
        }

        var angle = left * 2 * Math.PI;
        var figure = new PathFigure { StartPoint = new Point(center, center - radius) };
        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(center + radius * Math.Sin(angle), center - radius * Math.Cos(angle)),
            Size = new Size(radius, radius),
            IsLargeArc = angle > Math.PI,
            SweepDirection = SweepDirection.Clockwise,
        });
        RingArc.Data = new PathGeometry { Figures = { figure } };
    }

    // What the form says, in words

    /// <summary>The words that finish "Your PC shuts down when ...", or null when nothing is chosen yet.</summary>
    private string? Clause() =>
        DownloadsChoice.IsChecked == true ? ConditionWords.DownloadsClause
        : AppChoice.IsChecked == true ? SelectedApp is { } app ? ConditionWords.AppClause(app.Name) : null
        : ConditionWords.IdleClause;

    private RunningApp? SelectedApp => AppBox.SelectedItem as RunningApp;

    private IReadOnlySet<DayOfWeek> SelectedDays() =>
        _days.Where(toggle => toggle.IsChecked == true).Select(toggle => (DayOfWeek)toggle.Tag!).ToHashSet();

    private TimeOnly RepeatTime() => AtPicker.SelectedTime is { } time ? TimeOnly.FromTimeSpan(time) : TimeOnly.MinValue;

    private static string Minutes(int minutes) => minutes == 1 ? "1 minute" : $"{minutes} minutes";

    private static string Clock(TimeOnly time) => time.ToString("t", CultureInfo.CurrentCulture);

    // The buttons and the choices on the form

    private void OnTicked(object? sender, EventArgs e) => Update();

    private void OnTimerChanged(object? sender, EventArgs e) => Show();

    private void OnConditionsChanged(object? sender, EventArgs e) => Show();

    private void OnChoiceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading)
            Update();
    }

    private void OnWhenChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        InPanel.Visibility = AtTime || ForSomething ? Visibility.Collapsed : Visibility.Visible;
        AtPanel.Visibility = AtTime ? Visibility.Visible : Visibility.Collapsed;
        ConditionPanel.Visibility = ForSomething ? Visibility.Visible : Visibility.Collapsed;
        if (!_loading)
            Update();
    }

    private void OnDelayChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_loading)
            Update();
    }

    private void OnTimeChanged(TimePicker sender, TimePickerSelectedValueChangedEventArgs args)
    {
        if (!_loading)
            Update();
    }

    private void OnPresetClick(object sender, RoutedEventArgs e)
    {
        var delay = TimeSpan.FromMinutes(int.Parse((string)((Button)sender).Tag));
        _loading = true;
        HoursBox.Value = delay.Hours;
        MinutesBox.Value = delay.Minutes;
        _loading = false;
        Update();
    }

    private void OnRepeatToggled(object sender, RoutedEventArgs e)
    {
        DaysPanel.Visibility = RepeatToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        if (!_loading)
            Update();
    }

    private void OnDayToggled(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            Update();
    }

    private void OnConditionChecked(object sender, RoutedEventArgs e)
    {
        if (AppChoice.IsChecked == true)
            ShowApps(_runningApps.Read());
        if (!_loading)
            Update();
    }

    private void OnConditionChoiceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading)
            Update();
    }

    private void OnRefreshAppsClick(object sender, RoutedEventArgs e) => ShowApps(_runningApps.Read());

    /// <summary>The apps running now, by the name Windows gives them, with the file under it dimmed.</summary>
    private void ShowApps(IReadOnlyList<RunningApp> apps)
    {
        var selected = SelectedApp?.Path;
        AppBox.ItemsSource = apps;
        AppBox.SelectedIndex = apps.ToList().FindIndex(app => string.Equals(app.Path, selected, StringComparison.OrdinalIgnoreCase));
        if (AppBox.SelectedIndex < 0 && apps.Count > 0)
            AppBox.SelectedIndex = 0;
    }

    private void OnRepeatEditClick(object sender, RoutedEventArgs e)
    {
        if (_window.Repeat.Current is not { } repeat)
            return;

        _loading = true;
        ActionList.SelectedIndex = (int)repeat.Action;
        WhenBar.SelectedItem = AtItem;
        AtPicker.SelectedTime = repeat.Time.ToTimeSpan();
        RepeatToggle.IsOn = true;
        DaysPanel.Visibility = Visibility.Visible;
        foreach (var toggle in _days)
            toggle.IsChecked = repeat.Days.Contains((DayOfWeek)toggle.Tag!);
        _loading = false;
        Update();
    }

    private async void OnRepeatRemoveClick(object sender, RoutedEventArgs e)
    {
        await _window.Repeat.RemoveAsync();
        Update();
    }

    private void OnCancelWaitingClick(object sender, RoutedEventArgs e) => _window.Conditions.Stop();

    private async void OnStartClick(object sender, RoutedEventArgs e)
    {
        if (ForSomething)
        {
            StartCondition();
            return;
        }

        if (Repeats)
        {
            await SaveRepeatAsync();
            return;
        }

        if (Target(DateTimeOffset.Now) is not { } target)
            return;

        AppSettings.LastAction = SelectedAction;
        AppSettings.LastWasAtTime = AtTime;
        if (AtTime)
            AppSettings.LastTime = TimeOnly.FromTimeSpan(AtPicker.SelectedTime!.Value);
        else
            AppSettings.LastDelay = Delay;

        StartButton.IsEnabled = false;
        _window.HideStatus();
        if (!await _timer.StartAsync(SelectedAction, target))
        {
            _window.ShowSchedulerError("Couldn't start the timer", "the timer");
            StartButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Starts watching the chosen condition. An app that closed between choosing it and pressing Start can't be
    /// waited for, so Start says so rather than starting.
    /// </summary>
    private void StartCondition()
    {
        AppSettings.LastAction = SelectedAction;
        var condition = BuildCondition();
        if (condition is null || condition is AppClosedCondition && !IsRunning(SelectedApp))
        {
            _window.ShowStatus(
                InfoBarSeverity.Warning,
                "That app has closed",
                "Pick an app that is still running, or choose another condition.");
            return;
        }

        StartButton.IsEnabled = false;
        _window.HideStatus();
        _window.Conditions.Start(condition, SelectedAction);
    }

    private ICondition? BuildCondition() =>
        DownloadsChoice.IsChecked == true
            ? new QuietDownloadCondition(_network, Thresholds[ThresholdBox.SelectedIndex].BytesPerSecond, TimeSpan.FromMinutes(QuietPeriods[QuietBox.SelectedIndex]))
            : AppChoice.IsChecked == true
                ? SelectedApp is { } app ? new AppClosedCondition(_runningApps, app) : null
                : new IdleInputCondition(_input, TimeSpan.FromMinutes(IdlePeriods[IdleBox.SelectedIndex]));

    /// <summary>Whether an app with that executable's path is still running, read now rather than from the list.</summary>
    private bool IsRunning(RunningApp? app) =>
        app is not null && _runningApps.Read().Any(running => string.Equals(running.Path, app.Path, StringComparison.OrdinalIgnoreCase));

    /// <summary>Saves the chosen repeating schedule, replacing the one there is after asking.</summary>
    private async Task SaveRepeatAsync()
    {
        var time = RepeatTime();
        var days = SelectedDays();
        if (days.Count == 0)
            return;

        var repeat = Repeat.Create(SelectedAction, time, days);
        if (_window.Repeat.Current is { } saved && !Same(saved, repeat) && !await _window.Dialogs.ConfirmReplaceAsync(Describe(saved)))
            return;

        AppSettings.LastAction = SelectedAction;
        AppSettings.LastWasAtTime = true;
        AppSettings.LastTime = time;
        StartButton.IsEnabled = false;
        _window.HideStatus();
        if (!await _window.Repeat.SaveAsync(repeat))
        {
            _window.ShowSchedulerError("Couldn't save the schedule", "the schedule");
            StartButton.IsEnabled = true;
        }
    }

    private static bool Same(Repeat one, Repeat other) =>
        one.Action == other.Action && one.Time == other.Time && one.Days.SetEquals(other.Days);

    /// <summary>The schedule in words for the replace dialog: "shut down on weekdays at 23:30".</summary>
    private static string Describe(Repeat repeat) =>
        $"{Actions.Get(repeat.Action).Name.ToLowerInvariant()} {repeat.DaysInWords(CultureInfo.CurrentCulture)} at {Clock(repeat.Time)}";

    private async void OnCancelClick(object sender, RoutedEventArgs e) => await _timer.CancelAsync();

    private async void OnPostponeClick(object sender, RoutedEventArgs e)
    {
        if (!await _timer.PostponeAsync(MainWindow.Postponement))
            _window.ShowSchedulerError("Couldn't postpone the timer", "the timer");
    }
}
