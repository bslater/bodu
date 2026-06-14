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
    /// Initializes a new instance of the <see cref="NullExchangeRateCache" /> class.
    /// </summary>
    private NullExchangeRateCache()
    {
    }

    /// <summary>
    /// Gets the shared instance of the no-op cache.
    /// </summary>
    /// <returns>The singleton <see cref="NullExchangeRateCache" />.</returns>
    public static NullExchangeRateCache Instance { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(string provider, ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf) =>
        Array.Empty<CachedExchangeRate>();

    /// <inheritdoc />
    public void Store(string provider, ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        // Intentionally no-op: this cache never stores anything.
    }
}
