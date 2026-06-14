// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the shared mechanism for an <see cref="IExchangeRateCache" />: read-time freshness filtering, write-time
/// merge-and-prune, and cache-file naming. Derived types implement only the persistence of raw entries.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReadEntries" /> returns the raw stored rows without filtering; this base applies the freshness policy in
/// <see cref="GetRates" /> and prunes stale rows in <see cref="Store" /> before handing the surviving set to
/// <see cref="WriteEntries" />, so the backing store self-cleans on every write.
/// </para>
/// </remarks>
public abstract class ExchangeRateCacheBase
    : IExchangeRateCache
{
    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(string provider, ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);

        IReadOnlyList<CachedExchangeRate> entries = ReadEntries(provider, pair);
        if (entries.Count == 0)
            return Array.Empty<CachedExchangeRate>();

        List<CachedExchangeRate> fresh = new(entries.Count);
        foreach (CachedExchangeRate entry in entries)
        {
            if (entry.IsFresh(asOf, duration))
                fresh.Add(entry);
        }

        fresh.Sort(static (left, right) => left.Date.CompareTo(right.Date));
        return fresh;
    }

    /// <inheritdoc />
    public void Store(string provider, ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(provider);
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        // Merge with any existing entry so the most recently cached rate wins per date.
        Dictionary<DateOnly, CachedExchangeRate> merged = new();
        foreach (CachedExchangeRate existing in ReadEntries(provider, pair))
            merged[existing.Date] = existing;

        foreach (CachedExchangeRate rate in rates)
        {
            if (!merged.TryGetValue(rate.Date, out CachedExchangeRate current) || rate.CachedAtUtc >= current.CachedAtUtc)
                merged[rate.Date] = rate;
        }

        // Prune rows that are no longer fresh, then order by date so the file is stable and self-cleaning.
        List<CachedExchangeRate> ordered = new(merged.Count);
        foreach (CachedExchangeRate entry in merged.Values)
        {
            if (entry.IsFresh(asOf, duration))
                ordered.Add(entry);
        }

        ordered.Sort(static (left, right) => left.Date.CompareTo(right.Date));

        WriteEntries(provider, pair, ordered);
    }

    /// <summary>
    /// Builds the cache-file name for a provider and pair.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The file name, for example <c>Yahoo_AUDUSD.toml</c>.</returns>
    protected static string BuildFileName(string provider, ExchangeRatePair pair) =>
        $"{provider}_{pair.FromIsoCode}{pair.ToIsoCode}.toml";

    /// <summary>
    /// Reads the raw, unfiltered cached entries for a provider and pair.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored entries, or an empty list when none are available or the read fails.</returns>
    protected abstract IReadOnlyList<CachedExchangeRate> ReadEntries(string provider, ExchangeRatePair pair);

    /// <summary>
    /// Writes the supplied entries for a provider and pair, replacing any existing entry.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <param name="pair">The currency pair.</param>
    /// <param name="entries">The ordered entries to persist.</param>
    protected abstract void WriteEntries(string provider, ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> entries);
}
