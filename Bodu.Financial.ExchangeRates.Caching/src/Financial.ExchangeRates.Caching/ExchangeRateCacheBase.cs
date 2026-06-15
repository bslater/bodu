// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic mechanism for an <see cref="IExchangeRateCache" />: read-time freshness filtering and
/// write-time merge-and-prune for a single provider. Derived types implement only the persistence of raw entries; this
/// base prescribes no physical storage structure.
/// </summary>
/// <typeparam name="TOptions">The options type carrying the bound provider and any storage settings.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ReadEntries" /> returns the raw stored rows without filtering; this base applies the freshness policy in
/// <see cref="GetRates" /> and prunes stale rows in <see cref="Store" /> before handing the surviving set to
/// <see cref="WriteEntries" />, so the backing store self-cleans on every write.
/// </para>
/// </remarks>
public abstract class ExchangeRateCacheBase<TOptions>
    : IExchangeRateCache
    where TOptions : ExchangeRateCacheOptions
{
    /// <summary>
    /// The validated options carrying the bound provider and any storage settings.
    /// </summary>
    private readonly TOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateCacheBase{TOptions}" /> class.
    /// </summary>
    /// <param name="options">The options carrying the bound provider and any storage settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected ExchangeRateCacheBase(TOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _options = options;
    }

    /// <inheritdoc />
    public string Provider => _options.Provider;

    /// <summary>
    /// Gets the validated options the cache was constructed with.
    /// </summary>
    /// <returns>The cache options.</returns>
    protected TOptions Options => _options;

    /// <inheritdoc />
    public IReadOnlyList<CachedExchangeRate> GetRates(ExchangeRatePair pair, TimeSpan duration, DateTimeOffset asOf)
    {
        IReadOnlyList<CachedExchangeRate> entries = ReadEntries(pair);
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
    public void Store(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> rates, TimeSpan duration, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(rates);

        if (rates.Count == 0)
            return;

        // Merge with any existing entry so the most recently cached rate wins per date.
        Dictionary<DateOnly, CachedExchangeRate> merged = new();
        foreach (CachedExchangeRate existing in ReadEntries(pair))
            merged[existing.Date] = existing;

        foreach (CachedExchangeRate rate in rates)
        {
            if (!merged.TryGetValue(rate.Date, out CachedExchangeRate current) || rate.CachedAtUtc >= current.CachedAtUtc)
                merged[rate.Date] = rate;
        }

        // Prune rows that are no longer fresh, then order by date so the store is stable and self-cleaning.
        List<CachedExchangeRate> ordered = new(merged.Count);
        foreach (CachedExchangeRate entry in merged.Values)
        {
            if (entry.IsFresh(asOf, duration))
                ordered.Add(entry);
        }

        ordered.Sort(static (left, right) => left.Date.CompareTo(right.Date));

        WriteEntries(pair, ordered);
    }

    /// <summary>
    /// Reads the raw, unfiltered cached entries for a pair.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The stored entries, or an empty list when none are available or the read fails.</returns>
    protected abstract IReadOnlyList<CachedExchangeRate> ReadEntries(ExchangeRatePair pair);

    /// <summary>
    /// Writes the supplied entries for a pair, replacing any existing entry.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <param name="entries">The ordered entries to persist.</param>
    protected abstract void WriteEntries(ExchangeRatePair pair, IReadOnlyList<CachedExchangeRate> entries);
}
