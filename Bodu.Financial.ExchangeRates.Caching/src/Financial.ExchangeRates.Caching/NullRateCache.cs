// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
public sealed class NullRateCache
    : IRateCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullRateCache" /> class bound to a provider.
    /// </summary>
    /// <param name="provider">The provider identifier the cache reports.</param>
    private NullRateCache(string provider)
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
    public static NullRateCache Create(string provider)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        return new NullRateCache(provider);
    }

    /// <inheritdoc />
    public IReadOnlyList<CachedRate> GetRates(CurrencyPair pair, TimeSpan duration, DateTimeOffset asOf) =>
        Array.Empty<CachedRate>();

    /// <inheritdoc />
    public void Store(CurrencyPair pair, IReadOnlyList<CachedRate> rates, TimeSpan duration, DateTimeOffset asOf)
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
    public RateCacheWriteStatus StoreFetchedRange(
        CurrencyPair pair,
        IReadOnlyList<CachedRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rows);
        ThrowHelper.ThrowIfGreaterThan(start, end);

        // Intentionally no-op after validation: this cache persists neither rows nor coverage, so it reports the write
        // as skipped while still enforcing the same argument contract as every other backend.
        return RateCacheWriteStatus.Skipped;
    }
}
