using System.Security;
using System.Security.Principal;
using System.Text;

namespace Pwrschdlr.Core;

/// <summary>
/// The per-user scheduled task that starts <c>Pwrschdlr.exe --repeat &lt;id&gt;</c> shortly before a repeating
/// schedule's occurrence, so the warning appears whether or not the window is open.
/// </summary>
public static class RepeatTask
{
    /// <summary>Every user on a PC shares the task namespace, so each user's task carries their SID.</summary>
    public static string NameFor(SecurityIdentifier user) => $"Pwrschdlr repeat-{user}";

    /// <summary>Creates the task, or replaces the one there is.</summary>
    /// <param name="repeat">The schedule whose occurrence the task opens the window for.</param>
    /// <param name="start">The first time the task should run: the next occurrence, less the warning.</param>
    /// <param name="warning">How long before an occurrence the warning starts; the task runs at this time of day.</param>
    /// <returns>schtasks.exe's exit code: 0 when Task Scheduler accepted the task.</returns>
    public static async Task<int> RegisterAsync(string name, string exePath, Repeat repeat, DateTimeOffset start, TimeSpan warning, SecurityIdentifier user)
    {
        var xml = Path.Combine(Path.GetTempPath(), $"pwrschdlr-{Guid.NewGuid():N}.xml");
        await File.WriteAllTextAsync(xml, Xml(exePath, repeat, start, warning, user), Encoding.Unicode);
        try
        {
            return await Schtasks.RunAsync("/Create", "/TN", name, "/XML", xml, "/F");
        }
        finally
        {
            File.Delete(xml);
        }
    }

    /// <returns>schtasks.exe's exit code, which isn't 0 when there was no task to delete.</returns>
    public static Task<int> DeleteAsync(string name) => Schtasks.RunAsync("/Delete", "/TN", name, "/F");

    public static async Task<bool> ExistsAsync(string name) => await Schtasks.RunAsync("/Query", "/TN", name) == 0;

    /// <summary>
    /// A weekly trigger at the local time of day, so the schedule keeps its time when the clocks change. A trigger
    /// missed while the PC was off, asleep or signed out doesn't run later, so a skipped occurrence stays skipped.
    /// The trigger runs on the warning's days, not the schedule's, since it fires the warning early: a schedule just
    /// after midnight starts the task the day before, exactly where <paramref name="start"/> already falls.
    /// </summary>
    internal static string Xml(string exePath, Repeat repeat, DateTimeOffset start, TimeSpan warning, SecurityIdentifier user)
    {
        var days = repeat.TriggerDays(warning);
        var week = string.Join("\n", Enum.GetValues<DayOfWeek>().Where(days.Contains).OrderBy(day => (int)day).Select(day => $"          <{day} />"));
        return $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Author>Pwrschdlr</Author>
            <Description>Opens Pwrschdlr shortly before your repeating schedule runs, so you can still cancel it. Removing the schedule removes this task.</Description>
          </RegistrationInfo>
          <Triggers>
            <CalendarTrigger>
              <StartBoundary>{start.DateTime:yyyy-MM-ddTHH:mm:ss}</StartBoundary>
              <Enabled>true</Enabled>
              <ScheduleByWeek>
                <DaysOfWeek>
        {week}
                </DaysOfWeek>
                <WeeksInterval>1</WeeksInterval>
              </ScheduleByWeek>
            </CalendarTrigger>
          </Triggers>
          <Principals>
            <Principal id="Author">
              <UserId>{user}</UserId>
              <LogonType>InteractiveToken</LogonType>
              <RunLevel>LeastPrivilege</RunLevel>
            </Principal>
          </Principals>
          <Settings>
            <MultipleInstancesPolicy>Parallel</MultipleInstancesPolicy>
            <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
            <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
            <StartWhenAvailable>false</StartWhenAvailable>
            <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
            <Priority>5</Priority>
          </Settings>
          <Actions Context="Author">
            <Exec>
              <Command>{SecurityElement.Escape(exePath)}</Command>
              <Arguments>--repeat {repeat.Id:D}</Arguments>
            </Exec>
          </Actions>
        </Task>
        """;
    }
}
