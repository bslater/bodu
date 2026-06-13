// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryYahooRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Yahoo;

/// <summary>
/// An in-memory <see cref="IYahooRateCache" /> used by tests to exercise the provider's cache hydration and persistence
/// without touching the file system.
/// </summary>
internal sealed class InMemoryYahooRateCache
    : IYahooRateCache
{
    /// <summary>
    /// The stored rates, keyed by provider and pair.
    /// </summary>
    private readonly Dictionary<string, List<CachedExchangeRate>> _store = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, string provider) =>
        _store.TryGetValue(Key(pair, provider), out List<CachedExchangeRate>? rates)
            ? rates
            : Array.Empty<CachedExchangeRate>();

    /// <inheritdoc />
    public void Store(ExchangeRatePair pair, string provider, IReadOnlyList<CachedExchangeRate> rates) =>
        _store[Key(pair, provider)] = rates.ToList();

    /// <summary>
    /// Builds the store key for a provider and pair.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="provider">The provider identifier.</param>
    /// <returns>The store key.</returns>
    private static string Key(ExchangeRatePair pair, string provider) =>
        $"{provider}_{pair.FromIsoCode}{pair.ToIsoCode}";
}
