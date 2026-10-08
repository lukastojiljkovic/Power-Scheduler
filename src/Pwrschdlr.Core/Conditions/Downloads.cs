using System.Net.NetworkInformation;

namespace Pwrschdlr.Core.Conditions;

/// <summary>How many bytes a second the PC is receiving now, or null before there is a rate to read.</summary>
public interface INetworkReader
{
    double? Read();
}

/// <summary>The received counters of every interface, read at one moment.</summary>
public sealed record NetworkSample(DateTimeOffset At, IReadOnlyDictionary<string, long> Received);

/// <summary>Turns two readings of the received counters into a rate.</summary>
public static class NetworkRate
{
    /// <summary>
    /// Bytes a second between two readings, or null before the first one. An interface that appeared or went away
    /// is left out of the sum, and a counter that started over counts as no traffic, so neither looks like a spike.
    /// </summary>
    public static double? Between(NetworkSample? previous, NetworkSample current)
    {
        if (previous is null)
            return null;
        var seconds = (current.At - previous.At).TotalSeconds;
        if (seconds <= 0)
            return null;

        var received = 0L;
        foreach (var (id, bytes) in current.Received)
            if (previous.Received.TryGetValue(id, out var before) && bytes > before)
                received += bytes - before;
        return received / seconds;
    }
}

/// <summary>Reads the received counters of the interfaces that are up, not loopback and not tunnels.</summary>
public sealed class NetworkReader : INetworkReader
{
    private NetworkSample? _previous;

    public double? Read()
    {
        var current = new NetworkSample(DateTimeOffset.Now, Counters());
        var rate = NetworkRate.Between(_previous, current);
        _previous = current;
        return rate;
    }

    private static IReadOnlyDictionary<string, long> Counters()
    {
        var counters = new Dictionary<string, long>();
        try
        {
            foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (network.OperationalStatus != OperationalStatus.Up
                    || network.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;
                counters[network.Id] = network.GetIPStatistics().BytesReceived;
            }
        }
        // An interface that went away between the two calls, or a machine with no usable interfaces.
        catch (NetworkInformationException)
        {
        }

        return counters;
    }
}

/// <summary>
/// Met when the PC has received less than a threshold for a stretch of time in a row: a quiet-period accumulator
/// that starts over as soon as the rate goes above the threshold.
/// </summary>
public sealed class QuietDownloadCondition(INetworkReader network, double thresholdBytesPerSecond, TimeSpan quietFor) : ICondition
{
    private DateTimeOffset? _quietSince;

    public string Clause => ConditionWords.DownloadsClause;

    public ConditionStatus Sample(DateTimeOffset now)
    {
        var rate = network.Read();
        if (rate > thresholdBytesPerSecond)
            _quietSince = null;
        else
            _quietSince ??= now;

        var quiet = _quietSince is { } since ? now - since : TimeSpan.Zero;
        return new ConditionStatus(
            quiet >= quietFor,
            "Waiting for downloads to finish",
            ConditionWords.DownloadReading(rate),
            ConditionWords.Quiet(quiet, quietFor));
    }
}
