// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheRulesBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Bodu.Financial.ExchangeRates.Caching.Benchmarks;

/// <summary>
/// Measures the shared cache-policy primitives every backend routes through: freshness selection over all-fresh and
/// half-stale row sets, the single-row and bulk merges, and the coverage fold.
/// </summary>
[MemoryDiagnoser]
public class RateCacheRulesBenchmarks
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    private CachedRate[] _allFresh = null!;
    private CachedRate[] _halfStale = null!;
    private CachedRate[] _singleIncoming = null!;
    private (DateOnly Start, DateOnly End, DateTimeOffset FetchedAtUtc)[] _windows = null!;

    /// <summary>
    /// Gets or sets the number of rows the policy primitives operate over.
    /// </summary>
    [Params(30, 365, 3650)]
    public int RowCount { get; set; }

    /// <summary>
    /// Builds the all-fresh and half-stale row sets, the single incoming row, and the coverage windows.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        DateOnly first = new DateOnly(2026, 5, 31).AddDays(-(RowCount - 1));
        _allFresh = new CachedRate[RowCount];
        _halfStale = new CachedRate[RowCount];
        for (int i = 0; i < RowCount; i++)
        {
            DateOnly date = first.AddDays(i);
            _allFresh[i] = new CachedRate(date, 0.5m, Now);

            // Alternate rows are stamped two durations ago, so half the set fails the freshness check.
            _halfStale[i] = new CachedRate(date, 0.5m, i % 2 == 0 ? Now : Now - Duration - Duration);
        }

        _singleIncoming = new[] { new CachedRate(new DateOnly(2026, 5, 31), 0.51m, Now) };

        _windows = new (DateOnly, DateOnly, DateTimeOffset)[12];
        for (int i = 0; i < _windows.Length; i++)
        {
            DateOnly start = first.AddDays(i * 30);
            _windows[i] = (start, start.AddDays(29), Now);
        }
    }

    /// <summary>
    /// Benchmarks freshness selection when every row survives, the common warm-cache read.
    /// </summary>
    /// <returns>The selected rows.</returns>
    [Benchmark(Baseline = true)]
    public List<CachedRate> SelectFresh_AllFresh() =>
        RateCacheRules.SelectFresh(_allFresh, Duration, Now);

    /// <summary>
    /// Benchmarks freshness selection when half the rows are stale and must be filtered out.
    /// </summary>
    /// <returns>The selected rows.</returns>
    [Benchmark]
    public List<CachedRate> SelectFresh_HalfStale() =>
        RateCacheRules.SelectFresh(_halfStale, Duration, Now);

    /// <summary>
    /// Benchmarks merging a single observation into the sorted stored set, the dominant store shape.
    /// </summary>
    /// <returns>The merged rows.</returns>
    [Benchmark]
    public List<CachedRate> MergeRows_SingleIncoming() =>
        RateCacheRules.MergeRows(_allFresh, _singleIncoming, Duration, Now);

    /// <summary>
    /// Benchmarks merging a full same-size batch, the range refetch's store shape.
    /// </summary>
    /// <returns>The merged rows.</returns>
    [Benchmark]
    public List<CachedRate> MergeRows_FullBatch() =>
        RateCacheRules.MergeRows(_allFresh, _allFresh, Duration, Now);

    /// <summary>
    /// Benchmarks folding recorded coverage windows into a coverage set.
    /// </summary>
    /// <returns>The folded coverage.</returns>
    [Benchmark]
    public DateRangeCoverage BuildCoverage_TwelveWindows() =>
        RateCacheRules.BuildCoverage(_windows, Duration, Now);
}
