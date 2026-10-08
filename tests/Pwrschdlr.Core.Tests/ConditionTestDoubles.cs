using Pwrschdlr.Core.Conditions;

namespace Pwrschdlr.Core.Tests;

/// <summary>A rate the test sets, instead of reading a network.</summary>
internal sealed class FakeNetwork : INetworkReader
{
    public double? Rate { get; set; }

    public double? Read() => Rate;
}

/// <summary>The apps the test says are running, instead of reading the process list.</summary>
internal sealed class FakeApps : IRunningApps
{
    public IReadOnlyList<RunningApp> Apps { get; set; } = [];

    public IReadOnlyList<RunningApp> Read() => Apps;
}

/// <summary>The idle time the test sets, instead of asking Windows.</summary>
internal sealed class FakeInput : IInputIdle
{
    public TimeSpan Idle { get; set; }

    public TimeSpan Read() => Idle;
}
