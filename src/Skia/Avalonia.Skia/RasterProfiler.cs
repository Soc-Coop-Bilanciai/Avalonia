using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Avalonia.Skia;

internal static class RasterProfiler
{
    private static readonly bool Enabled = Environment.GetEnvironmentVariable("AVALONIA_RASTER_PROFILE") == "1";
    private static readonly ConcurrentDictionary<string, Entry> Entries = new();
    private sealed class Entry { public long Ticks, Count; }
    static RasterProfiler()
    {
        if (Enabled)
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.WriteLine("RASTER_PROFILE " +
                JsonSerializer.Serialize(Entries.OrderByDescending(x => x.Value.Ticks).Select(x => new
                {
                    Operation = x.Key, Calls = x.Value.Count,
                    TotalMs = x.Value.Ticks * 1000.0 / Stopwatch.Frequency
                })));
    }
    public static Scope Measure(string name) => Enabled ? new Scope(name) : default;
    public readonly struct Scope : IDisposable
    {
        private readonly string? _name;
        private readonly long _start;
        public Scope(string name) { _name = name; _start = Stopwatch.GetTimestamp(); }
        public void Dispose()
        {
            if (_name == null) return;
            var elapsed = Stopwatch.GetTimestamp() - _start;
            var entry = Entries.GetOrAdd(_name, _ => new Entry());
            Interlocked.Add(ref entry.Ticks, elapsed);
            Interlocked.Increment(ref entry.Count);
        }
    }
}
