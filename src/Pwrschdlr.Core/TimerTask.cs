using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace Pwrschdlr.Core;

/// <summary>
/// The per-user scheduled task that starts <c>Pwrschdlr.exe --due &lt;id&gt;</c> when the warning before a timer runs out
/// should appear. It lets the timer outlive the window: Pwrschdlr can be closed, and it comes back to count down.
/// </summary>
public static class TimerTask
{
    /// <summary>Every user on a PC shares the task namespace, so each user's task carries their SID.</summary>
    public static string NameFor(SecurityIdentifier user) => $"Pwrschdlr timer-{user}";

    /// <summary>Creates the task, or replaces the one there is.</summary>
    /// <returns>schtasks.exe's exit code: 0 when Task Scheduler accepted the task.</returns>
    public static async Task<int> RegisterAsync(string name, string exePath, Guid id, DateTimeOffset start, SecurityIdentifier user)
    {
        var xml = Path.Combine(Path.GetTempPath(), $"pwrschdlr-{Guid.NewGuid():N}.xml");
        await File.WriteAllTextAsync(xml, Xml(exePath, id, start, user), Encoding.Unicode);
        try
        {
            return await SchtasksAsync("/Create", "/TN", name, "/XML", xml, "/F");
        }
        finally
        {
            File.Delete(xml);
        }
    }

    /// <returns>schtasks.exe's exit code, which isn't 0 when there was no task to delete.</returns>
    public static Task<int> DeleteAsync(string name) => SchtasksAsync("/Delete", "/TN", name, "/F");

    public static async Task<bool> ExistsAsync(string name) => await SchtasksAsync("/Query", "/TN", name) == 0;

    /// <summary>
    /// A one-time trigger in UTC, so changing the time zone doesn't move it. A trigger missed while the PC was off,
    /// asleep or signed out doesn't run later: nobody wants their PC to shut down right after they turn it on. The
    /// instance runs at normal priority without a time limit, because it is the window counting down.
    /// </summary>
    internal static string Xml(string exePath, Guid id, DateTimeOffset start, SecurityIdentifier user) => $"""
        <?xml version="1.0" encoding="UTF-16"?>
        <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
          <RegistrationInfo>
            <Author>Pwrschdlr</Author>
            <Description>Opens Pwrschdlr shortly before the timer you set runs out, so you can still cancel it. Cancelling the timer removes this task.</Description>
          </RegistrationInfo>
          <Triggers>
            <TimeTrigger>
              <StartBoundary>{start.UtcDateTime:yyyy-MM-ddTHH:mm:ss}Z</StartBoundary>
            </TimeTrigger>
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
              <Arguments>--due {id:D}</Arguments>
            </Exec>
          </Actions>
        </Task>
        """;

    private static async Task<int> SchtasksAsync(params string[] arguments)
    {
        var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = Process.Start(info)!;
        // Drain both pipes, so a full one can't block schtasks.
        await Task.WhenAll(process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync(), process.WaitForExitAsync());
        return process.ExitCode;
    }
}
