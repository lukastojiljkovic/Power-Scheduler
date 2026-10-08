using System.ComponentModel;
using System.Diagnostics;

namespace Pwrschdlr.Core.Conditions;

/// <summary>An app running with a visible window.</summary>
/// <param name="Path">Its executable's full path, which is what the condition matches on.</param>
/// <param name="Name">What to show: the app name Windows gives the file, or the process name when it has none.</param>
/// <param name="FileName">The executable's name, shown dimmed under the app name.</param>
public sealed record RunningApp(string Path, string Name, string FileName);

/// <summary>Reads the apps running with a visible window, for the choice of app and for watching one of them.</summary>
public interface IRunningApps
{
    IReadOnlyList<RunningApp> Read();
}

/// <summary>Reads the apps with a visible main window, skipping Pwrschdlr itself and processes it cannot read.</summary>
public sealed class RunningAppsReader : IRunningApps
{
    public IReadOnlyList<RunningApp> Read()
    {
        var self = Environment.ProcessPath;
        var apps = new List<RunningApp>();
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                if (Read(process, self) is { } app)
                    apps.Add(app);
            }
        }

        return apps.OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>A process gone since the list was made, a system process, or a file not ours to look at, is no app.</summary>
    private static RunningApp? Read(Process process, string? self)
    {
        try
        {
            if (process.MainWindowHandle == 0)
                return null;
            var path = process.MainModule?.FileName;
            if (path is null || string.Equals(path, self, StringComparison.OrdinalIgnoreCase))
                return null;
            return new RunningApp(path, Name(path), Path.GetFileName(path));
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>The name people know the app by: what Windows calls the file, or the process name otherwise.</summary>
    private static string Name(string path)
    {
        try
        {
            var description = FileVersionInfo.GetVersionInfo(path).FileDescription;
            if (!string.IsNullOrWhiteSpace(description))
                return description.Trim();
        }
        catch (Exception exception) when (exception is FileNotFoundException or Win32Exception or ArgumentException or NotSupportedException)
        {
        }

        return Path.GetFileNameWithoutExtension(path);
    }
}

/// <summary>
/// Met when no process with the chosen executable's path is left. Installers and launchers that start themselves
/// again run the same file, so the path, rather than the process, is what is watched.
/// </summary>
public sealed class AppClosedCondition(IRunningApps apps, RunningApp app) : ICondition
{
    public string Clause => ConditionWords.AppClause(app.Name);

    public ConditionStatus Sample(DateTimeOffset now)
    {
        var running = apps.Read().Any(candidate => string.Equals(candidate.Path, app.Path, StringComparison.OrdinalIgnoreCase));
        return new ConditionStatus(
            !running,
            $"Waiting for {app.Name} to close",
            running ? $"{app.Name} is still running" : $"{app.Name} has closed",
            null);
    }
}
