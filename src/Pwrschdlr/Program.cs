using System.Runtime.InteropServices;
using Microsoft.Windows.AppLifecycle;
using Pwrschdlr.Core;
using Pwrschdlr.Services;

namespace Pwrschdlr;

public static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Run by the uninstaller: removes the tasks and the settings of the current user.
        if (args is ["--uninstall"])
        {
            TimerTask.DeleteAsync(TimerService.TaskName).GetAwaiter().GetResult();
            RepeatTask.DeleteAsync(RepeatService.TaskName).GetAwaiter().GetResult();
            AppSettings.Clear();
            return 0;
        }

        // One window: a second start hands its activation to the first, which comes to the front.
        var instance = AppInstance.FindOrRegisterForKey("Pwrschdlr");
        if (!instance.IsCurrent)
        {
            AllowSetForegroundWindow(instance.ProcessId);
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            Task.Run(() => instance.RedirectActivationToAsync(activation).AsTask()).Wait();
            return 0;
        }

        XamlGeneratedProgram.XamlGeneratedMain();
        return 0;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllowSetForegroundWindow(uint processId);
}
