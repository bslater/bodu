// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateProviderBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching.Benchmarks;

/// <summary>
/// Measures the decorator's cache-hit serve paths over a seeded in-memory cache: the exact and nearest single-date
/// resolutions, the fully covered range serve (the zero-copy all-fresh path), and the inverse-pair single-date serve.
/// The inner provider is never consulted, so the numbers isolate the caching layer itself.
/// </summary>
[MemoryDiagnoser]
public class CachingRateProviderBenchmarks
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);
    private static readonly CurrencyPair Pair = new(CurrencyCode.AUD, CurrencyCode.USD);

    private CachingRateProvider _provider = null!;
    private DateOnly _firstDate;
    private DateOnly _lastDate;
    private DateOnly _gapDate;

    /// <summary>
    /// Gets or sets the number of daily rows seeded into the cache.
    /// </summary>
    [Params(30, 365, 3650)]
    public int RowCount { get; set; }

    /// <summary>
    /// Seeds the cache with the configured number of consecutive daily rows for the direct pair plus matching
    /// coverage, and builds the decorator over an inner provider that is never consulted on the hit paths. Only the
    /// direct orientation is seeded, so the inverse bench exercises the genuine reciprocal fallback.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _firstDate = new DateOnly(2026, 5, 31).AddDays(-(RowCount - 1));
        _lastDate = new DateOnly(2026, 5, 31);

        // A one-day hole two thirds in gives the nearest-resolution bench a genuine tolerance walk.
        _gapDate = _firstDate.AddDays((RowCount * 2) / 3);

        var cache = new InMemoryRateCache("Bench");
        cache.Store(Pair, BuildRows(), Duration, Now);
        cache.RecordCoverage(Pair, _firstDate, _lastDate, Duration, Now);

        _provider = new CachingRateProvider(
            new FixedDatedRateProvider(Array.Empty<ExchangeRate>()),
            cache,
            new CachingRateOptions { DefaultExpiry = Duration },
            new FixedTimeProvider(Now));
    }

    /// <summary>
    /// Benchmarks an exact-date single lookup served from the cache.
    /// </summary>
    /// <returns>The served result.</returns>
    [Benchmark(Baseline = true)]
    public RateLookupResult SingleDate_ExactHit() =>
        _provider.GetRate("AUD", "USD", _lastDate, RateLookupOptions.Exact);

    /// <summary>
    /// Benchmarks a nearest-within-tolerance single lookup that resolves across a one-day hole, exercising the cached
    /// row resolver's tolerance walk.
    /// </summary>
    /// <returns>The served result.</returns>
    [Benchmark]
    public RateLookupResult SingleDate_NearestHit() =>
        _provider.GetRate("AUD", "USD", _gapDate, RateLookupOptions.NearestWithin(7));

    /// <summary>
    /// Benchmarks a single lookup served by inverting the inverse pair's cached row.
    /// </summary>
    /// <returns>The served result.</returns>
    [Benchmark]
    public RateLookupResult SingleDate_InverseHit() =>
        _provider.GetRate("USD", "AUD", _lastDate, RateLookupOptions.Exact);

    /// <summary>
    /// Benchmarks a fully covered range lookup, the snapshot-read serve that takes the zero-copy all-fresh path.
    /// </summary>
    /// <returns>The served range.</returns>
    [Benchmark]
    public RateRangeResult Range_CoveredHit() =>
        _provider.GetRates("AUD", "USD", _firstDate, _lastDate);

    /// <summary>
    /// Builds the seeded daily rows, leaving the gap date unfilled.
    /// </summary>
    /// <returns>The rows to seed.</returns>
    private CachedRate[] BuildRows()
    {
        var rows = new List<CachedRate>(RowCount);
        for (int i = 0; i < RowCount; i++)
        {
            DateOnly date = _firstDate.AddDays(i);
            if (date == _gapDate)
                continue;

            decimal rate = 0.5m + (i % 100) * 0.001m;
            rows.Add(new CachedRate(date, rate, Now));
        }

        return rows.ToArray();
    }
}
