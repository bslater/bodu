// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Benchmarks;

/// <summary>
/// Measures the throughput of <see cref="Bodu.Financial.ExchangeRates.RateSeries" /> against a
/// <see cref="SortedDictionary{TKey, TValue}" /> baseline and against an array-of-structs baseline over a synthetic
/// ten-year daily-rate series.
/// </summary>
[MemoryDiagnoser]
public class RateSeriesBenchmarks
{
    private static readonly CurrencyPair s_pair = new(CurrencyCode.USD, CurrencyCode.AUD);

    private Bodu.Financial.ExchangeRates.RateSeries _series = null!;
    private SortedDictionary<DateOnly, decimal> _sortedDict = null!;
    private (DateOnly Date, decimal Rate)[] _arrayOfStructs = null!;
    private DateOnly _hitDate;
    private DateOnly _previousMissDate;
    private DateOnly _nearestMissDate;

    /// <summary>
    /// Gets or sets the number of dated rate observations in the synthetic series.
    /// </summary>
    [Params(365, 3650)]
    public int Count { get; set; }

    /// <summary>
    /// Builds the synthetic daily-rate series shared across the benchmark methods.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        DateOnly start = new(2000, 1, 1);
        List<(DateOnly Date, decimal Rate)> rates = new(Count);

        for (int i = 0; i < Count; i++)
            rates.Add((start.AddDays(i * 2), 1.50m + (i * 0.0001m)));

        _series = new Bodu.Financial.ExchangeRates.RateSeries(s_pair, "Bench", rates);

        _sortedDict = new SortedDictionary<DateOnly, decimal>();
        foreach ((DateOnly Date, decimal Rate) entry in rates)
            _sortedDict[entry.Date] = entry.Rate;

        _arrayOfStructs = rates.ToArray();
        _hitDate = rates[Count / 2].Date;
        _previousMissDate = _hitDate.AddDays(1);
        _nearestMissDate = _hitDate.AddDays(1);
    }

    /// <summary>
    /// Benchmarks an exact-date lookup against <see cref="RateSeries" />.
    /// </summary>
    /// <returns>The resolved rate value.</returns>
    [Benchmark(Baseline = true)]
    public decimal Series_ExactHit()
    {
        _series.TryGetRate(_hitDate, RateLookupOptions.Exact, out _, out decimal rate);
        return rate;
    }

    /// <summary>
    /// Benchmarks a previous-on-or-before lookup against <see cref="RateSeries" />.
    /// </summary>
    /// <returns>The resolved rate value.</returns>
    [Benchmark]
    public decimal Series_PreviousMiss()
    {
        _series.TryGetRate(_previousMissDate, RateLookupOptions.PreviousWithin(2), out _, out decimal rate);
        return rate;
    }

    /// <summary>
    /// Benchmarks a nearest-date lookup against <see cref="RateSeries" />.
    /// </summary>
    /// <returns>The resolved rate value.</returns>
    [Benchmark]
    public decimal Series_NearestMiss()
    {
        _series.TryGetRate(_nearestMissDate, RateLookupOptions.NearestWithin(2), out _, out decimal rate);
        return rate;
    }

    /// <summary>
    /// Benchmarks an exact-date lookup against a <see cref="SortedDictionary{TKey, TValue}" /> baseline.
    /// </summary>
    /// <returns>The resolved rate value.</returns>
    [Benchmark]
    public decimal SortedDictionary_ExactHit() =>
        _sortedDict.TryGetValue(_hitDate, out decimal rate) ? rate : 0m;

    /// <summary>
    /// Benchmarks an exact-date lookup over a flat array-of-structs using
    /// <see cref="System.Array.BinarySearch{T}(T[], T)" /> with a struct comparer.
    /// </summary>
    /// <returns>The resolved rate value.</returns>
    [Benchmark]
    public decimal ArrayOfStructs_ExactHit()
    {
        int index = System.Array.BinarySearch(_arrayOfStructs, (_hitDate, 0m), StructDateComparer.Instance);
        return index >= 0 ? _arrayOfStructs[index].Rate : 0m;
    }

    /// <summary>
    /// Provides a comparer over (date, rate) tuples that orders by date only, used for the array-of-structs baseline.
    /// </summary>
    private sealed class StructDateComparer
        : IComparer<(DateOnly Date, decimal Rate)>
    {
        /// <summary>
        /// Gets the shared comparer instance.
        /// </summary>
        public static StructDateComparer Instance { get; } = new();

        /// <inheritdoc />
        public int Compare((DateOnly Date, decimal Rate) x, (DateOnly Date, decimal Rate) y) =>
            x.Date.CompareTo(y.Date);
    }
}
