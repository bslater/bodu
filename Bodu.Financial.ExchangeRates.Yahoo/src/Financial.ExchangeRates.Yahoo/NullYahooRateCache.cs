// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullYahooRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// An <see cref="IYahooRateCache" /> that stores nothing, used when on-disk caching is disabled.
/// </summary>
public sealed class NullYahooRateCache
    : IYahooRateCache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullYahooRateCache" /> class.
    /// </summary>
    private NullYahooRateCache()
    {
    }

    /// <summary>
    /// Gets the shared instance of the no-op cache.
    /// </summary>
    /// <returns>The singleton <see cref="NullYahooRateCache" />.</returns>
    public static NullYahooRateCache Instance { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, string provider) => Array.Empty<CachedExchangeRate>();

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, string provider, IReadOnlyList<CachedExchangeRate> rates)
    {
        // Intentionally no-op: this cache never stores anything.
    }
}
