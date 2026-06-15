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
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf) =>
        Array.Empty<CachedExchangeRate>();

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        // Intentionally no-op: this cache never stores anything.
    }

    /// <inheritdoc />
    public DateRangeCoverage GetCoverage(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf) =>
        new();

    /// <inheritdoc />
    public void RecordCoverage(ExchangeRatePair pair, DateOnly start, DateOnly end, TimeSpan duration, DateTimeOffset asOf)
    {
        // Intentionally no-op: this cache records no coverage.
    }

    /// <inheritdoc />
    public ExchangeRateCacheWriteStatus StoreFetchedRange(
        ExchangeRatePair pair,
        IReadOnlyList<CachedExchangeRate> rows,
        DateOnly start,
        DateOnly end,
        TimeSpan duration,
        DateTimeOffset asOf) =>

        // Intentionally no-op: this cache persists neither rows nor coverage, so it reports the write as skipped.
        ExchangeRateCacheWriteStatus.Skipped;
}
