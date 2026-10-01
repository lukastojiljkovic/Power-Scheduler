using System.Security.Principal;
using System.Xml.Linq;

namespace Pwrschdlr.Core.Tests;

public class TimerTaskTests
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
    private static readonly SecurityIdentifier User = WindowsIdentity.GetCurrent().User!;

    [Fact]
    public void The_task_starts_the_app_for_this_timer_at_the_UTC_time()
    {
        var id = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 10, 1, 23, 29, 0, TimeSpan.FromHours(2));

        var task = XDocument.Parse(TimerTask.Xml(@"C:\Program Files\R&D\Pwrschdlr.exe", id, start, User));

        Assert.Equal("2026-10-01T21:29:00Z", task.Descendants(Ns + "StartBoundary").Single().Value);
        Assert.Equal(@"C:\Program Files\R&D\Pwrschdlr.exe", task.Descendants(Ns + "Command").Single().Value);
        Assert.Equal($"--due {id:D}", task.Descendants(Ns + "Arguments").Single().Value);
        Assert.Equal(User.Value, task.Descendants(Ns + "UserId").Single().Value);
        Assert.Equal("LeastPrivilege", task.Descendants(Ns + "RunLevel").Single().Value);
    }

    [Fact]
    public void A_missed_trigger_doesnt_run_later_and_the_counting_window_isnt_stopped()
    {
        var settings = XDocument.Parse(TimerTask.Xml("Pwrschdlr.exe", Guid.NewGuid(), DateTimeOffset.Now, User)).Descendants(Ns + "Settings").Single();

        Assert.Equal("false", settings.Element(Ns + "StartWhenAvailable")!.Value);
        Assert.Equal("false", settings.Element(Ns + "StopIfGoingOnBatteries")!.Value);
        Assert.Equal("PT0S", settings.Element(Ns + "ExecutionTimeLimit")!.Value);
        Assert.Equal("Parallel", settings.Element(Ns + "MultipleInstancesPolicy")!.Value);
    }

    [Fact]
    public void The_task_name_belongs_to_the_user() => Assert.Equal($"Pwrschdlr timer-{User}", TimerTask.NameFor(User));

    [Fact]
    public async Task Task_Scheduler_accepts_replaces_and_deletes_the_task()
    {
        var name = $"Pwrschdlr test-{Guid.NewGuid():N}";
        var exe = Path.Combine(Environment.SystemDirectory, "whoami.exe");
        try
        {
            Assert.Equal(0, await TimerTask.RegisterAsync(name, exe, Guid.NewGuid(), DateTimeOffset.Now.AddDays(1), User));
            Assert.Equal(0, await TimerTask.RegisterAsync(name, exe, Guid.NewGuid(), DateTimeOffset.Now.AddDays(2), User));
            Assert.True(await TimerTask.ExistsAsync(name));
        }
        finally
        {
            await TimerTask.DeleteAsync(name);
        }

        Assert.False(await TimerTask.ExistsAsync(name));
    }
}
