using System.Diagnostics;

namespace Pwrschdlr.Core;

/// <summary>Runs schtasks.exe for the tasks Pwrschdlr registers, without a window and without hanging on output.</summary>
internal static class Schtasks
{
    public static async Task<int> RunAsync(params string[] arguments)
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
