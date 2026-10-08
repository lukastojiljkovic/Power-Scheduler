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

    [Fact]
    public void The_task_starts_the_app_for_the_next_occurrence_less_the_warning()
    {
        var id = Guid.NewGuid();
        // 2026-10-01 is a Thursday; the schedule runs every weekday at 23:30.
        var start = new DateTimeOffset(2026, 10, 1, 23, 29, 0, TimeSpan.FromHours(2));

        var task = XDocument.Parse(RepeatTask.Xml(@"C:\Program Files\R&D\Pwrschdlr.exe", id, start, Weekdays, User));

        Assert.Equal("2026-10-01T23:29:00", task.Descendants(Ns + "StartBoundary").Single().Value);
        Assert.Equal(@"C:\Program Files\R&D\Pwrschdlr.exe", task.Descendants(Ns + "Command").Single().Value);
        Assert.Equal($"--repeat {id:D}", task.Descendants(Ns + "Arguments").Single().Value);
        Assert.Equal(User.Value, task.Descendants(Ns + "UserId").Single().Value);
        Assert.Equal("LeastPrivilege", task.Descendants(Ns + "RunLevel").Single().Value);
    }

    [Fact]
    public void The_trigger_runs_weekly_on_the_chosen_days()
    {
        var days = XDocument.Parse(RepeatTask.Xml("Pwrschdlr.exe", Guid.NewGuid(), DateTimeOffset.Now, new HashSet<DayOfWeek> { DayOfWeek.Wednesday, DayOfWeek.Monday }, User))
            .Descendants(Ns + "DaysOfWeek").Single();

        Assert.Equal(["Monday", "Wednesday"], days.Elements().Select(day => day.Name.LocalName));
        Assert.Equal("1", days.Parent!.Element(Ns + "WeeksInterval")!.Value);
    }

    [Fact]
    public void A_missed_trigger_doesnt_run_later()
    {
        var settings = XDocument.Parse(RepeatTask.Xml("Pwrschdlr.exe", Guid.NewGuid(), DateTimeOffset.Now, Weekdays, User)).Descendants(Ns + "Settings").Single();

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
            Assert.Equal(0, await RepeatTask.RegisterAsync(name, exe, Guid.NewGuid(), DateTimeOffset.Now.AddDays(1), Weekdays, User));
            Assert.Equal(0, await RepeatTask.RegisterAsync(name, exe, Guid.NewGuid(), DateTimeOffset.Now.AddDays(1), new HashSet<DayOfWeek> { DayOfWeek.Sunday }, User));
            Assert.True(await RepeatTask.ExistsAsync(name));
        }
        finally
        {
            await RepeatTask.DeleteAsync(name);
        }

        Assert.False(await RepeatTask.ExistsAsync(name));
    }
}
