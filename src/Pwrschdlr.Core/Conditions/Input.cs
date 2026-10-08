using System.Runtime.InteropServices;

namespace Pwrschdlr.Core.Conditions;

/// <summary>How long ago the last mouse or keyboard input was.</summary>
public interface IInputIdle
{
    TimeSpan Read();
}

/// <summary>Reads the idle time Windows keeps for the session.</summary>
public sealed partial class InputIdle : IInputIdle
{
    /// <summary>
    /// The idle time from the two tick counts. Both are milliseconds the same clock counts, and both wrap after
    /// 49.7 days, so the difference is taken in 32 bits, where the two wraps cancel each other out.
    /// </summary>
    public static TimeSpan IdleFor(long tickCount, uint lastInputTick) => TimeSpan.FromMilliseconds((uint)(tickCount - lastInputTick));

    public TimeSpan Read()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info) ? IdleFor(Environment.TickCount64, info.Tick) : TimeSpan.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LastInputInfo
    {
        public uint Size;
        public uint Tick;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLastInputInfo(ref LastInputInfo info);
}

/// <summary>Met when nothing has been touched for the chosen stretch of time.</summary>
public sealed class IdleInputCondition(IInputIdle input, TimeSpan idleFor) : ICondition
{
    public string Clause => ConditionWords.IdleClause;

    public ConditionStatus Sample(DateTimeOffset now)
    {
        var idle = input.Read();
        return new ConditionStatus(
            idle >= idleFor,
            "Waiting until nobody uses the PC",
            $"Last input {Timing.Words(TimeSpan.FromMinutes(Math.Floor(idle.TotalMinutes)))} ago",
            ConditionWords.Quiet(idle, idleFor));
    }
}
