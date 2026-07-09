// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IExchangeRateCache" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
public sealed class NullExchangeRateCache
    : IExchangeRateCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullExchangeRateCache" /> class bound to a provider.
    /// </summary>
    /// <param name="provider">The provider identifier the cache reports.</param>
    private NullExchangeRateCache(string provider)
    {
        Provider = provider;
    }

    /// <inheritdoc />
    public string Provider { get; }

    /// <summary>
    /// Creates a no-op cache bound to the supplied provider.
    /// </summary>
    /// <param name="provider">The provider identifier the cache reports.</param>
    /// <returns>A new no-op cache bound to <paramref name="provider" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="provider" /> is empty or white space.
    /// </exception>
    public static NullExchangeRateCache Create(string provider)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        return new NullExchangeRateCache(provider);
    }

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        Array.Empty<CachedExchangeRate>();

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        // Intentionally no-op after validation: this cache never stores anything, but it still enforces the same
        // argument contract as every other backend so a caller cannot pass a null collection undetected.
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        new();

    /// <inheritdoc />
    public void RecordCoverage(CurrencyPair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfGreaterThan(start, end);

        // Intentionally no-op after validation: this cache records no coverage, but it still rejects an inverted window
        // so the argument contract matches every other backend.
    }

    /// <inheritdoc />
    public ExchangeRateCacheWriteStatus StoreFetchedRange(
        CurrencyPair pair,
        IReadOnlyList<CachedExchangeRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);
        ThrowHelper.ThrowIfGreaterThan(start, end);

        // Intentionally no-op after validation: this cache persists neither rows nor coverage, so it reports the write
        // as skipped while still enforcing the same argument contract as every other backend.
        return ExchangeRateCacheWriteStatus.Skipped;
    }
}
