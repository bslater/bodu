// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryExchangeRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Contracts;

/// <summary>
/// An in-memory <see cref="IExchangeRateCache" /> that reuses the <see cref="ExchangeRateCacheBase" /> merge, prune, and
/// freshness mechanism over a dictionary, so it exercises the shared logic without touching the file system.
/// </summary>
public sealed class InMemoryExchangeRateCache
    : ExchangeRateCacheBase
{
    /// <summary>
    /// The raw stored rows, keyed by provider and pair.
    /// </summary>
    private readonly Dictionary<string, IReadOnlyList<CachedExchangeRate>> _store = new(StringComparer.Ordinal);

    /// <inheritdoc />
    protected override IReadOnlyList<CachedExchangeRate> ReadEntries(string provider, ExchangeRatePair pair) =>
        _store.TryGetValue(Key(provider, pair), out IReadOnlyList<CachedExchangeRate>? rows)
            ? rows
            : Array.Empty<CachedExchangeRate>();

    /// <inheritdoc />
    protected override void WriteEntries(string provider, ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> entries) =>
        _store[Key(provider, pair)] = entries;

    /// <summary>
    /// Builds the store key for a provider and pair.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The store key.</returns>
    private static string Key(string provider, ExchangeRatePair pair) =>
        $"{provider}_{pair.FromIsoCode}{pair.ToIsoCode}";
}
