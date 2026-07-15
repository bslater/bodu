// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryNotableDateCacheBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Bodu.Globalization.Calendar.Caching.Benchmarks;

/// <summary>
/// Measures the in-memory cache's primitives against a territory populated with a decade of years: the single-year
/// read and the year store that merges into the existing territory state.
/// </summary>
[MemoryDiagnoser]
public class InMemoryNotableDateCacheBenchmarks
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);

    private InMemoryNotableDateCache _cache = null!;
    private NotableDateCacheEntry _restoreEntry = null!;

    /// <summary>
    /// Gets or sets the number of occurrences each cached year carries.
    /// </summary>
    [Params(20, 200)]
    public int OccurrencesPerYear { get; set; }

    /// <summary>
    /// Populates one territory with a decade of computed years and prepares the entry the store benchmark rewrites.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var computer = new SyntheticNotableDateService(OccurrencesPerYear);
        _cache = new InMemoryNotableDateCache();

        for (int year = 2020; year <= 2029; year++)
        {
            IReadOnlyList<NotableDate> occurrences = computer.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "US");
            _ = _cache.StoreYear(new NotableDateCacheEntry("US", year, "bench", occurrences, Now), Ttl, Now);
        }

        _restoreEntry = new NotableDateCacheEntry(
            "US",
            2026,
            "bench",
            computer.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "US"),
            Now);
    }

    /// <summary>
    /// Benchmarks reading one fresh year from the populated territory.
    /// </summary>
    /// <returns>The cached entry.</returns>
    [Benchmark(Baseline = true)]
    public NotableDateCacheEntry? GetYear_Hit() =>
        _cache.GetYear("US", 2026, "bench", Ttl, Now);

    /// <summary>
    /// Benchmarks storing one year into the populated territory, replacing the existing entry.
    /// </summary>
    /// <returns>The write status.</returns>
    [Benchmark]
    public NotableDateCacheWriteStatus StoreYear_Merge() =>
        _cache.StoreYear(_restoreEntry, Ttl, Now);
}
