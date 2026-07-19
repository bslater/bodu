// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingRateProvider.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial.Samples.CachedRates;

/// <summary>
/// A delegating <see cref="IDatedRateProvider" /> that records every call reaching the inner source.
/// The caching scenarios wrap their "upstream" in this so the console output can prove which lookups
/// were served from the cache and which had to touch the source — with a live web provider, each
/// recorded call would be an HTTP fetch.
/// </summary>
public sealed class CountingRateProvider
    : IDatedRateProvider
{
    private readonly IDatedRateProvider _inner;
    private readonly List<string> _calls = [];

    /// <summary>
    /// Initializes a new instance wrapping <paramref name="inner" />.
    /// </summary>
    /// <param name="inner">The provider that receives the delegated calls.</param>
    public CountingRateProvider(IDatedRateProvider inner) =>
        _inner = inner;

    /// <summary>
    /// Gets the description of every call that reached the inner source, in order.
    /// </summary>
    public IReadOnlyList<string> Calls => _calls;

    /// <summary>
    /// Gets the number of calls that reached the inner source.
    /// </summary>
    public int CallCount => _calls.Count;

    /// <summary>
    /// Clears the recorded calls.
    /// </summary>
    public void Reset() => _calls.Clear();

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null)
    {
        _calls.Add($"GetRate {fromIsoCode}/{toIsoCode} (latest)");
        return _inner.GetRate(fromIsoCode, toIsoCode, options);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null)
    {
        _calls.Add($"GetRate {fromIsoCode}/{toIsoCode} {date:yyyy-MM-dd}");
        return _inner.GetRate(fromIsoCode, toIsoCode, date, options);
    }

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result)
    {
        _calls.Add($"TryGetRate {fromIsoCode}/{toIsoCode} {date:yyyy-MM-dd}");
        return _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);
    }

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        _calls.Add($"GetRates {fromIsoCode}/{toIsoCode} {startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}");
        return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        _calls.Add($"GetRateAsync {fromIsoCode}/{toIsoCode} (latest)");
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        _calls.Add($"GetRateAsync {fromIsoCode}/{toIsoCode} {date:yyyy-MM-dd}");
        return _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        _calls.Add($"GetRatesAsync {fromIsoCode}/{toIsoCode} {startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}");
        return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
    }
}
