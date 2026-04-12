using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sora.Tests.Helpers;

/// <summary>
///     Centralized test execution timing tracker.
///     Each collection fixture calls StartTimer/StopTimer with its category and name.
///     Durations are summed per category (Unit / Func) for reporting.
/// </summary>
public static class TestTimingStore
{
    private static readonly ConcurrentDictionary<string, TimeSpan>  _unitDurations = new();
    private static readonly ConcurrentDictionary<string, TimeSpan>  _funcDurations = new();
    private static readonly ConcurrentDictionary<string, Stopwatch> _activeTimers  = new();

    /// <summary>Gets total functional test duration (sum of all functional collections).</summary>
    public static TimeSpan TotalFuncDuration
    {
        get
        {
            TimeSpan total = TimeSpan.Zero;
            foreach (KeyValuePair<string, TimeSpan> kv in _funcDurations)
                total += kv.Value;
            return total;
        }
    }

    /// <summary>Gets total unit test duration (sum of all unit collections).</summary>
    public static TimeSpan TotalUnitDuration
    {
        get
        {
            TimeSpan total = TimeSpan.Zero;
            foreach (KeyValuePair<string, TimeSpan> kv in _unitDurations)
                total += kv.Value;
            return total;
        }
    }

    /// <summary>Gets per-collection breakdown for a category.</summary>
    public static IReadOnlyDictionary<string, TimeSpan> GetDurations(string category) =>
        category == "Unit" ? _unitDurations : _funcDurations;

    /// <summary>Starts a timer for a collection.</summary>
    public static void StartTimer(string category, string collection)
    {
        string    key = $"{category}:{collection}";
        Stopwatch sw  = new();
        _activeTimers[key] = sw;
        sw.Start();
    }

    /// <summary>Stops a timer and records its duration.</summary>
    public static TimeSpan StopTimer(string category, string collection)
    {
        string key = $"{category}:{collection}";
        if (!_activeTimers.TryRemove(key, out Stopwatch? sw))
            return TimeSpan.Zero;

        sw.Stop();
        TimeSpan elapsed = sw.Elapsed;

        ConcurrentDictionary<string, TimeSpan> target = category == "Unit" ? _unitDurations : _funcDurations;
        target[collection] = elapsed;
        return elapsed;
    }
}