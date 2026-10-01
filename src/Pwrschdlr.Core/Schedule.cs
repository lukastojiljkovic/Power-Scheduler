namespace Pwrschdlr.Core;

public enum TimerPhase
{
    /// <summary>Counting down, before the warning.</summary>
    Waiting,

    /// <summary>The warning counts down the last stretch, so the user can still cancel.</summary>
    Warning,

    /// <summary>The time is up: act now.</summary>
    Up,

    /// <summary>The PC was off or asleep when the time ran out. Acting now would surprise whoever just woke it.</summary>
    Missed,
}

/// <summary>A running timer: what happens, and when.</summary>
/// <param name="Id">Ties the scheduled task's launch to this timer, so a task left over from an older one does nothing.</param>
/// <param name="Start">When the timer was set or last postponed, where the progress ring starts.</param>
public sealed record Schedule(Guid Id, PowerAction Action, DateTimeOffset Start, DateTimeOffset Target)
{
    /// <summary>How late the end may be noticed and still acted on. The window checks several times a second.</summary>
    public static readonly TimeSpan Lateness = TimeSpan.FromSeconds(10);

    public static Schedule Create(PowerAction action, DateTimeOffset now, DateTimeOffset target) => new(Guid.NewGuid(), action, now, target);

    public TimerPhase PhaseAt(DateTimeOffset now, TimeSpan warning) =>
        now < Target - warning ? TimerPhase.Waiting
        : now < Target ? TimerPhase.Warning
        : now < Target + Lateness ? TimerPhase.Up
        : TimerPhase.Missed;

    public TimeSpan Remaining(DateTimeOffset now) => Target > now ? Target - now : TimeSpan.Zero;

    /// <summary>The share of the time still to go: 1 when the timer is set, 0 when it runs out.</summary>
    public double Left(DateTimeOffset now)
    {
        var total = Target - Start;
        return total > TimeSpan.Zero ? Math.Clamp(Remaining(now) / total, 0, 1) : 0;
    }

    /// <summary>Moves the timer later, counting from what was left, and fills the ring again.</summary>
    public Schedule Postpone(TimeSpan by, DateTimeOffset now) => this with { Start = now, Target = now + Remaining(now) + by };
}
