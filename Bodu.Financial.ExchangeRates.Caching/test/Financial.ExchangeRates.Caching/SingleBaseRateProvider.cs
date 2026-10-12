// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleBaseRateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IDatedRateProvider" /> test double shaped like a single-base bulk feed (for example the Reserve Bank of
/// Australia's AUD-based tables): it resolves from a fixed in-memory book, and its range reads throw
/// <see cref="RateSeriesNotFoundException" /> for a pair that does not include its base currency, as the shipped bulk
/// providers do.
/// </summary>
internal sealed class SingleBaseRateProvider
    : IDatedRateProvider
{
    /// <summary>The only currency every pair this feed carries must include.</summary>
    private readonly CurrencyCode _baseCurrency;

    /// <summary>The fixed provider backing the lookups.</summary>
    private readonly FixedDatedRateProvider _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleBaseRateProvider" /> class.
    /// </summary>
    /// <param name="baseCurrency">The base currency every carried pair includes.</param>
    /// <param name="rates">The observations the provider resolves from.</param>
    public SingleBaseRateProvider(CurrencyCode baseCurrency, IEnumerable<ExchangeRate> rates)
    {
        _baseCurrency = baseCurrency;
        _inner = new FixedDatedRateProvider(rates);
    }

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null) =>
        _inner.GetRate(fromIsoCode, toIsoCode, options);

    /// <inheritdoc />
    public RateLookupResult GetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null) =>
        _inner.GetRate(fromIsoCode, toIsoCode, date, options);

    /// <inheritdoc />
    public bool TryGetRate(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options, out RateLookupResult result) =>
        _inner.TryGetRate(fromIsoCode, toIsoCode, date, options, out result);

    /// <inheritdoc />
    public RateRangeResult GetRates(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate)
    {
        ThrowIfPairNotCarried(fromIsoCode, toIsoCode);

        return _inner.GetRates(fromIsoCode, toIsoCode, startDate, endDate);
    }

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
        _inner.GetRateAsync(fromIsoCode, toIsoCode, options, cancellationToken);

    /// <inheritdoc />
    public ValueTask<RateLookupResult> GetRateAsync(string fromIsoCode, string toIsoCode, DateOnly date, RateLookupOptions? options = null, CancellationToken cancellationToken = default) =>
        _inner.GetRateAsync(fromIsoCode, toIsoCode, date, options, cancellationToken);

    /// <inheritdoc />
    public ValueTask<RateRangeResult> GetRatesAsync(string fromIsoCode, string toIsoCode, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        ThrowIfPairNotCarried(fromIsoCode, toIsoCode);

        return _inner.GetRatesAsync(fromIsoCode, toIsoCode, startDate, endDate, cancellationToken);
    }

    /// <summary>
    /// Throws <see cref="RateSeriesNotFoundException" /> when neither side of the pair is the base currency.
    /// </summary>
    /// <param name="fromIsoCode">The source currency ISO code.</param>
    /// <param name="toIsoCode">The destination currency ISO code.</param>
    /// <exception cref="RateSeriesNotFoundException">The pair does not include the base currency.</exception>
    private void ThrowIfPairNotCarried(string fromIsoCode, string toIsoCode)
    {
        string baseIsoCode = _baseCurrency.ToString();
        if (fromIsoCode != baseIsoCode && toIsoCode != baseIsoCode)
            throw new RateSeriesNotFoundException($"{fromIsoCode}/{toIsoCode} is not carried by a {baseIsoCode}-based feed.");
    }
}
