namespace Pwrschdlr.Core.Conditions;

/// <summary>What a condition being watched shows, and whether the wait is over.</summary>
/// <param name="Met">The condition is met, so the ordinary timer starts from here.</param>
/// <param name="Waiting">What is being waited for: "Waiting for downloads to finish".</param>
/// <param name="Reading">The reading now: "Downloading now: 12.4 MB/s".</param>
/// <param name="Progress">How far towards the quiet period it is: "Quiet for 2 of 5 minutes", or null when there is none.</param>
public sealed record ConditionStatus(bool Met, string Waiting, string Reading, string? Progress);

/// <summary>
/// Something Pwrschdlr watches while it is open, one reading a second. The decision is pure: readings come from a
/// seam and the clock is handed in, so a condition can be tested without a network, a process list or a keyboard.
/// </summary>
public interface ICondition
{
    /// <summary>Takes one reading, and answers whether the condition is met and what the waiting screen shows.</summary>
    ConditionStatus Sample(DateTimeOffset now);

    /// <summary>The words that finish "nothing will happen when ...": "your downloads finish".</summary>
    string Clause { get; }
}
