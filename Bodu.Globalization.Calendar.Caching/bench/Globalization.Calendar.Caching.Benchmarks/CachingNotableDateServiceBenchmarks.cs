// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingNotableDateServiceBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Bodu.Globalization.Calendar.Caching.Benchmarks;

/// <summary>
/// Measures the caching decorator's warm serve paths over a pre-populated in-memory cache: the whole-civil-year
/// zero-copy hit, the sub-range clip, the multi-year batch serve, and the filtered serves. Every benchmark is a cache
/// hit, so the numbers isolate the caching layer's assembly and clipping work.
/// </summary>
[MemoryDiagnoser]
public class CachingNotableDateServiceBenchmarks
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private CachingNotableDateService _service = null!;
    private NotableDateFilter _allMatch = null!;
    private NotableDateFilter _halfMatch = null!;

    /// <summary>
    /// Gets or sets the number of occurrences each cached year carries.
    /// </summary>
    [Params(20, 200)]
    public int OccurrencesPerYear { get; set; }

    /// <summary>
    /// Builds the caching service over the synthetic computer and warms every year the benchmarks read, so each
    /// benchmark iteration is a pure cache hit.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _service = new CachingNotableDateService(
            new SyntheticNotableDateService(OccurrencesPerYear),
            new InMemoryNotableDateCache(),
            new NotableDateCachingOptions { Ttl = TimeSpan.FromDays(30), ResourceVersion = "bench" },
            timeProvider: new FixedTimeProvider(Now));

        _ = _service.Warm(new[] { "US" }, 2020, 2029);

        // Every synthetic occurrence is one of these two categories, so AnyOf both matches everything (the
        // zero-copy filtered serve) while PublicHoliday alone selects half the payload.
        _allMatch = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday),
            NotableDateFilter.ForCategory(NotableDateCategory.Observance));
        _halfMatch = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);
    }

    /// <summary>
    /// Benchmarks a whole-civil-year resolve, the zero-copy serve of the cached year list itself.
    /// </summary>
    /// <returns>The served occurrences.</returns>
    [Benchmark(Baseline = true)]
    public IReadOnlyList<NotableDate> Resolve_WholeYearHit() =>
        _service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "US");

    /// <summary>
    /// Benchmarks a one-month sub-range resolve, which clips the cached year by emitted date.
    /// </summary>
    /// <returns>The served occurrences.</returns>
    [Benchmark]
    public IReadOnlyList<NotableDate> Resolve_MonthClip() =>
        _service.Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), "US");

    /// <summary>
    /// Benchmarks a ten-year resolve, served through the batch read seam in one territory state read.
    /// </summary>
    /// <returns>The served occurrences.</returns>
    [Benchmark]
    public IReadOnlyList<NotableDate> Resolve_TenYearSpan() =>
        _service.Resolve(new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31)), "US");

    /// <summary>
    /// Benchmarks a filtered whole-year resolve where every occurrence matches, the zero-copy filtered serve.
    /// </summary>
    /// <returns>The served occurrences.</returns>
    [Benchmark]
    public IReadOnlyList<NotableDate> ResolveFiltered_AllMatch() =>
        _service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "US", _allMatch);

    /// <summary>
    /// Benchmarks a filtered whole-year resolve where half the occurrences match and must be copied out.
    /// </summary>
    /// <returns>The served occurrences.</returns>
    [Benchmark]
    public IReadOnlyList<NotableDate> ResolveFiltered_HalfMatch() =>
        _service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "US", _halfMatch);
}
