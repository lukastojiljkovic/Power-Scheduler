using System.Security.Principal;
using System.Xml.Linq;

namespace Pwrschdlr.Core.Tests;

public class RepeatTaskTests
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
    private static readonly SecurityIdentifier User = WindowsIdentity.GetCurrent().User!;

    private static readonly IReadOnlySet<DayOfWeek> Weekdays = new HashSet<DayOfWeek>
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
    };

    private static readonly TimeSpan Warning = TimeSpan.FromMinutes(1);

    private static Repeat On(TimeOnly time, params DayOfWeek[] days) =>
        Repeat.Create(PowerAction.ShutDown, time, days);

    [Fact]
    public void The_task_starts_the_app_for_the_next_occurrence_less_the_warning()
    {
        var id = Guid.NewGuid();
        // 2026-10-01 is a Thursday; the schedule runs every weekday at 23:30, warned a minute earlier.
        var repeat = new Repeat(id, PowerAction.ShutDown, new TimeOnly(23, 30), Weekdays);
        var start = new DateTimeOffset(2026, 10, 1, 23, 29, 0, TimeSpan.FromHours(2));

        var task = XDocument.Parse(RepeatTask.Xml(@"C:\Program Files\R&D\Pwrschdlr.exe", repeat, start, Warning, User));

        Assert.Equal("2026-10-01T23:29:00", task.Descendants(Ns + "StartBoundary").Single().Value);
        Assert.Equal(@"C:\Program Files\R&D\Pwrschdlr.exe", task.Descendants(Ns + "Command").Single().Value);
        Assert.Equal($"--repeat {id:D}", task.Descendants(Ns + "Arguments").Single().Value);
        Assert.Equal(User.Value, task.Descendants(Ns + "UserId").Single().Value);
        Assert.Equal("LeastPrivilege", task.Descendants(Ns + "RunLevel").Single().Value);
    }

    [Fact]
    public void The_trigger_runs_weekly_on_the_chosen_days()
    {
        var days = XDocument.Parse(RepeatTask.Xml("Pwrschdlr.exe", On(new TimeOnly(23, 30), DayOfWeek.Wednesday, DayOfWeek.Monday), DateTimeOffset.Now, Warning, User))
            .Descendants(Ns + "DaysOfWeek").Single();

        Assert.Equal(["Monday", "Wednesday"], days.Elements().Select(day => day.Name.LocalName));
        Assert.Equal("1", days.Parent!.Element(Ns + "WeeksInterval")!.Value);
    }

    [Fact]
    public void A_schedule_just_after_midnight_opens_the_task_the_day_before()
    {
        // Monday 2026-10-05 at 00:05, warned ten minutes earlier: Sunday 2026-10-04 at 23:55.
        var repeat = On(new TimeOnly(0, 5), DayOfWeek.Monday);
        var start = new DateTimeOffset(2026, 10, 4, 23, 55, 0, TimeSpan.FromHours(2));

        var task = XDocument.Parse(RepeatTask.Xml("Pwrschdlr.exe", repeat, start, TimeSpan.FromMinutes(10), User));

        Assert.Equal("2026-10-04T23:55:00", task.Descendants(Ns + "StartBoundary").Single().Value);
        Assert.Equal(["Sunday"], task.Descendants(Ns + "DaysOfWeek").Single().Elements().Select(day => day.Name.LocalName));
    }

    [Fact]
    public void A_missed_trigger_doesnt_run_later()
    {
        var settings = XDocument.Parse(RepeatTask.Xml("Pwrschdlr.exe", On(new TimeOnly(23, 30), [.. Weekdays]), DateTimeOffset.Now, Warning, User)).Descendants(Ns + "Settings").Single();

        Assert.Equal("false", settings.Element(Ns + "StartWhenAvailable")!.Value);
        Assert.Equal("false", settings.Element(Ns + "StopIfGoingOnBatteries")!.Value);
        Assert.Equal("PT0S", settings.Element(Ns + "ExecutionTimeLimit")!.Value);
        Assert.Equal("Parallel", settings.Element(Ns + "MultipleInstancesPolicy")!.Value);
    }

    [Fact]
    public void The_task_name_belongs_to_the_user() => Assert.Equal($"Pwrschdlr repeat-{User}", RepeatTask.NameFor(User));

    [Fact]
    public async Task Task_Scheduler_accepts_replaces_and_deletes_the_task()
    {
        var name = $"Pwrschdlr test-{Guid.NewGuid():N}";
        var exe = Path.Combine(Environment.SystemDirectory, "whoami.exe");
        try
        {
            Assert.Equal(0, await RepeatTask.RegisterAsync(name, exe, On(new TimeOnly(23, 30), [.. Weekdays]), DateTimeOffset.Now.AddDays(1), Warning, User));
            Assert.Equal(0, await RepeatTask.RegisterAsync(name, exe, On(new TimeOnly(23, 30), DayOfWeek.Sunday), DateTimeOffset.Now.AddDays(1), Warning, User));
            Assert.True(await RepeatTask.ExistsAsync(name));
        }
        finally
        {
            await RepeatTask.DeleteAsync(name);
        }

        Assert.False(await RepeatTask.ExistsAsync(name));
    }
}
