// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryRateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// An <see cref="IRateCache" /> that stores a single provider's rates in memory for the lifetime of the
/// instance, expiring them through the same freshness mechanism as the file caches.
/// </summary>
/// <remarks>
/// <para>
/// Rates expire by caching duration rather than by storage: the shared <see cref="RateCacheBase{TOptions}" />
/// filters stale rows on read and prunes them on write, so a cached rate is served only while it remains fresh under
/// the duration supplied by the caching provider (see <see cref="CachingRateOptions.DefaultExpiry" /> and
/// <see cref="CachingRateOptions.ProviderExpiry" />). Nothing is persisted, so the cache starts empty on each
/// process and is a drop-in alternative to <see cref="TomlFileRateCache" /> when on-disk files are unwanted.
/// </para>
/// <para>
/// Reads and writes are individually thread-safe. As with every <see cref="IRateCache" />, a concurrent
/// store-after-store for the same pair resolves last-write-wins, which is acceptable for a best-effort cache.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // An in-memory caching provider: read-through caching with no on-disk files.
/// var options = new CachingRateOptions { DefaultExpiry = TimeSpan.FromMinutes(15) };
/// IDatedRateProvider cached = new CachingRateProvider(rba, new InMemoryRateCache("RBA"), options);
///]]>
/// </code>
/// </example>
public sealed class InMemoryRateCache
    : RateCacheBase<RateCacheOptions>
{
    /// <summary>The in-memory store of per-pair state, keyed by currency pair. Each value is an immutable snapshot owned by the cache, carrying both the cached rows and the coverage windows.</summary>
    private readonly ConcurrentDictionary<CurrencyPair, CachePairState> _store = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRateCache" /> class.
    /// </summary>
    /// <param name="options">The options carrying the bound provider.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public InMemoryRateCache(RateCacheOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRateCache" /> class bound to a provider name.
    /// </summary>
    /// <param name="provider">The provider the cache stores rates for.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="provider" /> is empty or white space.
    /// </exception>
    public InMemoryRateCache(string provider)
        : this(new RateCacheOptions { Provider = provider })
    {
    }

    /// <inheritdoc />
    private protected override CachePairState ReadState(CurrencyPair pair) =>
        _store.TryGetValue(pair, out CachePairState? state) ? state : CachePairState.Empty;

    /// <inheritdoc />
    private protected override bool WriteState(CurrencyPair pair, CachePairState state)
    {
        // Drop the pair entirely only when nothing remains to retain, so an empty state does not linger in the map.
        if (state.Entries.Count == 0 && state.Coverage.Count == 0)
        {
            _store.TryRemove(pair, out _);
            return true;
        }

        var entries = new CachedRate[state.Entries.Count];
        for (int i = 0; i < state.Entries.Count; i++)
            entries[i] = state.Entries[i];

        var coverage = new CoverageWindow[state.Coverage.Count];
        for (int i = 0; i < state.Coverage.Count; i++)
            coverage[i] = state.Coverage[i];

        _store[pair] = new CachePairState(entries, coverage);

        // An in-memory dictionary write cannot fail, so the write is always durable for the instance lifetime.
        return true;
    }
}
