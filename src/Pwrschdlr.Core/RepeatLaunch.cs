namespace Pwrschdlr.Core;

/// <summary>What to do when a repeating schedule's occurrence comes up.</summary>
public enum RepeatDecision
{
    /// <summary>The warning for the next occurrence hasn't started yet.</summary>
    NotYet,

    /// <summary>Start the ordinary timer for the occurrence.</summary>
    Start,

    /// <summary>A timer or a condition is already running, so this occurrence is skipped.</summary>
    SkipBusy,

    /// <summary>The occurrence came too long ago, so it is skipped, exactly as a missed one-time timer is.</summary>
    Missed,
}

/// <summary>Turns the nearest occurrence of a repeating schedule into what to do about it.</summary>
public static class RepeatLaunch
{
    public static RepeatDecision Decide(DateTimeOffset occurrence, DateTimeOffset now, TimeSpan warning, bool somethingRunning)
    {
        if (now < occurrence - warning)
            return RepeatDecision.NotYet;
        if (somethingRunning)
            return RepeatDecision.SkipBusy;
        // Never late: one the PC slept through is reported, never run.
        return now - occurrence > Schedule.Lateness ? RepeatDecision.Missed : RepeatDecision.Start;
    }
}
