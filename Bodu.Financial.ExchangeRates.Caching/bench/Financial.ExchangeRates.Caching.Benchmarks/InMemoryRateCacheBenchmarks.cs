// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryRateCacheBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching.Benchmarks;

/// <summary>
/// Measures the in-memory cache's write and read primitives against a pre-populated pair: the single-row store (the
/// dominant read-through write shape), the year-window atomic store, the all-fresh read, and the coverage fold.
/// </summary>
[MemoryDiagnoser]
public class InMemoryRateCacheBenchmarks
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);
    private static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    private InMemoryRateCache _cache = null!;
    private CachedRate[] _singleRow = null!;
    private CachedRate[] _yearRows = null!;
    private DateOnly _yearStart;
    private DateOnly _yearEnd;

    /// <summary>
    /// Gets or sets the number of daily rows already stored for the pair.
    /// </summary>
    [Params(30, 365, 3650)]
    public int RowCount { get; set; }

    /// <summary>
    /// Populates the cache with the configured number of consecutive daily rows and prepares the incoming payloads.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        DateOnly first = new DateOnly(2026, 5, 31).AddDays(-(RowCount - 1));
        var rows = new CachedRate[RowCount];
        for (int i = 0; i < RowCount; i++)
            rows[i] = new CachedRate(first.AddDays(i), 0.5m + (i % 100) * 0.001m, Now);

        _cache = new InMemoryRateCache("Bench");
        _cache.Store(Pair, rows, Duration, Now);
        _cache.RecordCoverage(Pair, first, new DateOnly(2026, 5, 31), Duration, Now);

        // The single incoming row re-stores the newest date, the read-through path's dominant write.
        _singleRow = new[] { new CachedRate(new DateOnly(2026, 5, 31), 0.51m, Now) };

        _yearEnd = new DateOnly(2026, 5, 31);
        _yearStart = _yearEnd.AddDays(-364);
        _yearRows = new CachedRate[365];
        for (int i = 0; i < 365; i++)
            _yearRows[i] = new CachedRate(_yearStart.AddDays(i), 0.6m, Now);
    }

    /// <summary>
    /// Benchmarks merging a single observation into the populated pair.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Store_SingleRowMerge() =>
        _cache.Store(Pair, _singleRow, Duration, Now);

    /// <summary>
    /// Benchmarks the atomic rows-and-coverage store of a whole year window.
    /// </summary>
    /// <returns>The write status.</returns>
    [Benchmark]
    public RateCacheWriteStatus StoreFetchedRange_Year() =>
        _cache.StoreFetchedRange(Pair, _yearRows, _yearStart, _yearEnd, Duration, Now);

    /// <summary>
    /// Benchmarks reading the pair's fresh rows, the all-fresh zero-copy path.
    /// </summary>
    /// <returns>The fresh rows.</returns>
    [Benchmark]
    public IReadOnlyList<CachedRate> GetRates_AllFresh() =>
        _cache.GetRates(Pair, Duration, Now);

    /// <summary>
    /// Benchmarks folding the pair's recorded coverage windows.
    /// </summary>
    /// <returns>The folded coverage.</returns>
    [Benchmark]
    public DateRangeCoverage GetCoverage() =>
        _cache.GetCoverage(Pair, Duration, Now);
}
