// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetBenchmarks.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Bodu.Collections.Generic.Concurrent.Benchmarks;

/// <summary>
/// Measures the throughput and allocation profile of the core single-element and snapshot operations of
/// <see cref="ConcurrentHashSet{T}" />.
/// </summary>
[MemoryDiagnoser]
public class ConcurrentHashSetBenchmarks
{
    private int[] _keys = [];
    private int[] _absentKeys = [];
    private ConcurrentHashSet<int> _populated = new();

    /// <summary>
    /// Gets or sets the number of elements each benchmark operates on.
    /// </summary>
    [Params(1_000, 100_000)]
    public int ElementCount { get; set; }

    /// <summary>
    /// Builds the shared key arrays and the pre-populated set before each parameterized run.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _keys = Enumerable.Range(0, ElementCount).ToArray();
        _absentKeys = Enumerable.Range(ElementCount, ElementCount).ToArray();
        _populated = new ConcurrentHashSet<int>(_keys);
    }

    /// <summary>
    /// Measures populating a freshly created set with every key.
    /// </summary>
    /// <returns>The populated set, returned so the work cannot be optimized away.</returns>
    [Benchmark]
    public ConcurrentHashSet<int> Add()
    {
        var set = new ConcurrentHashSet<int>();
        foreach (int key in _keys)
            set.Add(key);

        return set;
    }

    /// <summary>
    /// Measures a lock-free containment probe for keys that are present.
    /// </summary>
    /// <returns>The number of probes that found their key.</returns>
    [Benchmark]
    public int ContainsHit()
    {
        var hits = 0;
        foreach (int key in _keys)
        {
            if (_populated.Contains(key))
                hits++;
        }

        return hits;
    }

    /// <summary>
    /// Measures a lock-free containment probe for keys that are absent.
    /// </summary>
    /// <returns>The number of probes that did not find their key.</returns>
    [Benchmark]
    public int ContainsMiss()
    {
        var misses = 0;
        foreach (int key in _absentKeys)
        {
            if (!_populated.Contains(key))
                misses++;
        }

        return misses;
    }

    /// <summary>
    /// Measures the round trip of populating a fresh set and then removing every key from it.
    /// </summary>
    /// <returns>The emptied set, returned so the work cannot be optimized away.</returns>
    [Benchmark]
    public ConcurrentHashSet<int> AddThenRemove()
    {
        var set = new ConcurrentHashSet<int>(_keys);
        foreach (int key in _keys)
            set.Remove(key);

        return set;
    }

    /// <summary>
    /// Measures capturing a full point-in-time snapshot of the populated set.
    /// </summary>
    /// <returns>The snapshot array.</returns>
    [Benchmark]
    public int[] ToArraySnapshot() =>
        _populated.ToArray();
}
