using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Pwrschdlr.Core;
using Pwrschdlr.Services;
using Windows.Foundation;

namespace Pwrschdlr.Views;

/// <summary>Sets a timer, or counts down the one that's running.</summary>
public sealed partial class TimerView : UserControl
{
    private const double RingSize = 320;
    private const double RingThickness = 14;

    private readonly MainWindow _window;
    private readonly TimerService _timer;
    private bool _loading = true;

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

        // Start from the last timer's choices.
        var last = Power.IsAvailable(AppSettings.LastAction) ? AppSettings.LastAction : PowerAction.ShutDown;
        ActionList.SelectedIndex = (int)last;
        HoursBox.Value = AppSettings.LastDelay.Hours;
        MinutesBox.Value = AppSettings.LastDelay.Minutes;
        AtPicker.SelectedTime = AppSettings.LastTime.ToTimeSpan();
        WhenBar.SelectedItem = AppSettings.LastWasAtTime ? AtItem : InItem;
        PostponeButton.Content = MainWindow.PostponeLabel;
        _loading = false;

        Loaded += (_, _) =>
        {
            _timer.Changed += OnTimerChanged;
            _timer.Ticked += OnTicked;
            Show();
        };
        Unloaded += (_, _) =>
        {
            _timer.Changed -= OnTimerChanged;
            _timer.Ticked -= OnTicked;
        };
    }

    private PowerAction SelectedAction => ActionList.SelectedItem is GridViewItem { Tag: PowerAction action } ? action : PowerAction.ShutDown;

    private bool AtTime => WhenBar.SelectedItem == AtItem;

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

    /// <summary>When the timer would run out if it started now, or <see langword="null"/> while nothing is set.</summary>
    private DateTimeOffset? Target(DateTimeOffset now) =>
        AtTime
            ? AtPicker.SelectedTime is { } time ? Timing.NextAt(TimeOnly.FromTimeSpan(time), now, TimeZoneInfo.Local) : null
            : Delay > TimeSpan.Zero ? now + Delay : null;

    private void Show()
    {
        var running = _timer.Current is not null;
        SetupPanel.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        RunningPanel.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        KeepsRunningBar.Message = $"The timer keeps running. Pwrschdlr opens again {AppSettings.WarningLabel} before it runs out, so you can still cancel.";
        Update();
    }

    private void Update()
    {
        var now = DateTimeOffset.Now;
        if (_timer.Current is { } schedule)
            UpdateRunning(schedule, now);
        else
            UpdateSummary(now);
    }

    private void UpdateSummary(DateTimeOffset now)
    {
        var info = Actions.Get(SelectedAction);
        SummaryGlyph.Glyph = info.Glyph;
        var target = Target(now);
        StartButton.IsEnabled = target is not null;
        if (target is { } time)
        {
            SummaryText.Text = $"{info.Future} {MainWindow.When(time)}";
            SummaryDetail.Text = $"In {Timing.Words(time - now)}";
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

    private void OnTicked(object? sender, EventArgs e) => Update();

    private void OnTimerChanged(object? sender, EventArgs e) => Show();

    private void OnChoiceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading)
            Update();
    }

    private void OnWhenChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        InPanel.Visibility = AtTime ? Visibility.Collapsed : Visibility.Visible;
        AtPicker.Visibility = AtTime ? Visibility.Visible : Visibility.Collapsed;
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

    private async void OnStartClick(object sender, RoutedEventArgs e)
    {
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
            _window.ShowSchedulerError("Couldn't start the timer");
            StartButton.IsEnabled = true;
        }
    }

    private async void OnCancelClick(object sender, RoutedEventArgs e) => await _timer.CancelAsync();

    private async void OnPostponeClick(object sender, RoutedEventArgs e)
    {
        if (!await _timer.PostponeAsync(MainWindow.Postponement))
            _window.ShowSchedulerError("Couldn't postpone the timer");
    }
}
