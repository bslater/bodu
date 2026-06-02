// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeProviderBenchmarks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Bodu.Financial.Benchmarks;

/// <summary>
/// Measures the throughput of <see cref="CompositeDatedExchangeRateProvider" /> as the number of inner providers
/// increases, including a miss-then-hit pattern that forces the composite to traverse every provider before the last.
/// </summary>
[MemoryDiagnoser]
public class CompositeProviderBenchmarks
{
    private CompositeDatedExchangeRateProvider _composite = null!;
    private DateOnly _date;

    /// <summary>
    /// Gets or sets the number of inner providers configured on the composite.
    /// </summary>
    [Params(2, 5, 10)]
    public int ProviderCount { get; set; }

    /// <summary>
    /// Builds <see cref="ProviderCount" /> empty providers followed by a single provider that actually carries the
    /// rate, so the lookup traverses every provider in order before resolving on the last.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _date = new DateOnly(2024, 1, 3);

        List<IDatedExchangeRateProvider> providers = new(ProviderCount);
        for (int i = 0; i < ProviderCount - 1; i++)
            providers.Add(new FixedDatedExchangeRateProvider(System.Array.Empty<ExchangeRate>()));

        providers.Add(new FixedDatedExchangeRateProvider(new[]
        {
            new ExchangeRate("USD", "AUD", _date, 1.50m, "Last"),
        }));

        _composite = new CompositeDatedExchangeRateProvider(providers);
    }

    /// <summary>
    /// Benchmarks a lookup that requires the composite to traverse every inner provider before resolving on the last.
    /// </summary>
    /// <returns>The resolved rate value.</returns>
    [Benchmark]
    public decimal Composite_LastProviderHit()
    {
        _composite.TryGetRate("USD", "AUD", _date, ExchangeRateLookupOptions.Exact, out ExchangeRateLookupResult result);
        return result.Rate.Rate;
    }
}
