namespace Pwrschdlr.Core;

/// <summary>What happens to the PC when a timer runs out.</summary>
public enum PowerAction
{
    ShutDown,
    Restart,
    Sleep,
    Hibernate,
    SignOut,
}

/// <summary>How the app names an action in each place it appears.</summary>
/// <param name="Name">Tiles and buttons: "Shut down".</param>
/// <param name="Future">Sentences about the timer: "Your PC shuts down".</param>
/// <param name="Ongoing">The warning before it happens: "Shutting down".</param>
/// <param name="GlyphCode">The Segoe Fluent Icons code point.</param>
public sealed record ActionInfo(PowerAction Action, string Name, string Future, string Ongoing, int GlyphCode)
{
    public string Glyph => char.ConvertFromUtf32(GlyphCode);

    /// <summary>Shut down, restart and sign out close your apps; sleep and hibernate keep them open.</summary>
    public bool ClosesApps => Action is PowerAction.ShutDown or PowerAction.Restart or PowerAction.SignOut;

    public override string ToString() => Name;
}

public static class Actions
{
    /// <summary>Every action, in the order of <see cref="PowerAction"/>.</summary>
    public static IReadOnlyList<ActionInfo> All { get; } =
    [
        new(PowerAction.ShutDown, "Shut down", "Your PC shuts down", "Shutting down", 0xE7E8),
        new(PowerAction.Restart, "Restart", "Your PC restarts", "Restarting", 0xE777),
        new(PowerAction.Sleep, "Sleep", "Your PC goes to sleep", "Going to sleep", 0xE708),
        // Hibernation saves what's open to the disk before the PC turns off.
        new(PowerAction.Hibernate, "Hibernate", "Your PC hibernates", "Hibernating", 0xEDA2),
        new(PowerAction.SignOut, "Sign out", "Windows signs you out", "Signing out", 0xF3B1),
    ];

    public static ActionInfo Get(PowerAction action) => All[(int)action];
}
