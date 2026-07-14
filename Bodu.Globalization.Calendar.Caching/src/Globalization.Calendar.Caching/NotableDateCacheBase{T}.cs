// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheBase{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Caching;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the storage-agnostic mechanism for an <see cref="INotableDateCache" />: territory normalization, read-time
/// freshness and version filtering, and write-time merge-and-prune of per-year entries. Derived types implement only
/// the persistence of a territory's entry list; this base prescribes no physical storage structure.
/// </summary>
/// <typeparam name="TOptions">The options type carrying any storage settings.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ReadEntries" /> returns the raw stored entries without filtering; this base applies the freshness and
/// version policy in <see cref="GetYear" /> and prunes stale and superseded entries in <see cref="StoreYear" />, so the
/// backing store self-cleans on every write. The read-modify-write sequence in <see cref="StoreYear" /> runs under a
/// per-territory lock so concurrent writes to the same territory cannot interleave and lose an entry.
/// </para>
/// <para>
/// The freshness, validity, version-matching, and merge rules are delegated to the shared
/// <see cref="NotableDateCacheRules" /> so every backend applies one authoritative policy. This base contributes only
/// the per-territory locking and the read-modify-write sequencing over the entry list.
/// </para>
/// </remarks>
public abstract class NotableDateCacheBase<TOptions>
    : INotableDateCache, INotableDateCacheBatchReader
    where TOptions : NotableDateCacheOptions
{
    /// <summary>The validated options carrying any storage settings.</summary>
    private readonly TOptions _options;

    /// <summary>The striped per-territory locks guarding the read-modify-write sequence in <see cref="StoreYear" />.</summary>
    private readonly StripedLockSet<string> _territoryLocks = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateCacheBase{TOptions}" /> class.
    /// </summary>
    /// <param name="options">The options carrying any storage settings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    protected NotableDateCacheBase(TOptions options)
    {
        ThrowHelper.ThrowIfNull(options);
        options.Validate();

        _options = options;
    }

    /// <summary>
    /// Gets the validated options the cache was constructed with.
    /// </summary>
    /// <value>The cache options.</value>
    protected TOptions Options => _options;

    /// <inheritdoc />
    /// <remarks>
    /// Declared <see langword="virtual" /> so a backend whose storage can answer a single year cheaper than reading the
    /// whole territory — for example a keyed database row — can override the read while inheriting the write mechanism.
    /// An override must apply the same freshness, validity, and version policy through
    /// <see cref="NotableDateCacheRules" />.
    /// </remarks>
    public virtual NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(resourceVersion);

        string key = NotableDateCacheRules.NormalizeTerritory(territory);

        IReadOnlyList<NotableDateCacheEntry> entries = ReadEntries(key);
        if (entries.Count == 0)
            return null;

        return NotableDateCacheRules.SelectFresh(entries, year, resourceVersion, ttl, asOf);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The whole span is answered from one <see cref="ReadEntries" />, so a multi-year range lookup that would
    /// otherwise call <see cref="GetYear" /> once per year reads the territory's persisted state once.
    /// </remarks>
    NotableDateCacheEntry?[] INotableDateCacheBatchReader.GetYears(string territory, int firstYear, int lastYear, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(resourceVersion);
        ThrowHelper.ThrowIfGreaterThan(firstYear, lastYear);

        string key = NotableDateCacheRules.NormalizeTerritory(territory);
        IReadOnlyList<NotableDateCacheEntry> entries = ReadEntries(key);

        var years = new NotableDateCacheEntry?[lastYear - firstYear + 1];
        if (entries.Count == 0)
            return years;

        for (int year = firstYear; year <= lastYear; year++)
            years[year - firstYear] = NotableDateCacheRules.SelectFresh(entries, year, resourceVersion, ttl, asOf);

        return years;
    }

    /// <inheritdoc />
    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(entry);

        string key = NotableDateCacheRules.NormalizeTerritory(entry.Territory);

        // Rewrite the entry onto the normalized key so persisted state always self-describes with the canonical
        // territory, regardless of the casing the caller supplied.
        NotableDateCacheEntry normalized = string.Equals(entry.Territory, key, StringComparison.Ordinal)
            ? entry
            : entry with { Territory = key };

        lock (LockFor(key))
        {
            List<NotableDateCacheEntry> merged = NotableDateCacheRules.Merge(ReadEntries(key), normalized, ttl, asOf);

            return WriteEntries(key, merged)
                ? NotableDateCacheWriteStatus.Stored
                : NotableDateCacheWriteStatus.Failed;
        }
    }

    /// <inheritdoc />
    public abstract void Clear();

    /// <summary>
    /// Reads the raw, unfiltered persisted entries for a normalized territory.
    /// </summary>
    /// <param name="territory">The normalized territory key.</param>
    /// <returns>The stored entries, or an empty list when none are available or the read fails.</returns>
    /// <remarks>
    /// Declared <see langword="protected internal" /> so the storage seam is open to the backends in this assembly and
    /// in the companion SQLite and distributed packages, which derive from this base. An unrelated third-party backend
    /// implements the public <see cref="INotableDateCache" /> contract directly instead, as
    /// <see cref="NullNotableDateCache" /> does.
    /// </remarks>
    protected internal abstract IReadOnlyList<NotableDateCacheEntry> ReadEntries(string territory);

    /// <summary>
    /// Writes the supplied entries for a normalized territory, replacing any existing state.
    /// </summary>
    /// <param name="territory">The normalized territory key.</param>
    /// <param name="entries">
    /// The entries to persist. The list is freshly allocated by the caller for this write, so a backend may store the
    /// reference directly without a defensive copy.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the entries were persisted, including the deliberate deletion of an empty state;
    /// <see langword="false" /> when a storage failure was swallowed and nothing was persisted.
    /// </returns>
    /// <remarks>
    /// Declared <see langword="protected internal" /> for the same reason as <see cref="ReadEntries" />. The
    /// <see cref="bool" /> result lets <see cref="StoreYear" /> distinguish a durable write from a best-effort backend
    /// that swallowed a fault, so a failed write is reported as <see cref="NotableDateCacheWriteStatus.Failed" />
    /// rather than falsely as <see cref="NotableDateCacheWriteStatus.Stored" />.
    /// </remarks>
    protected internal abstract bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries);

    /// <summary>
    /// Returns the lock object guarding writes for the supplied normalized territory, creating it on first use.
    /// </summary>
    /// <param name="territory">The normalized territory whose write lock is required.</param>
    /// <returns>The per-territory lock object.</returns>
    private object LockFor(string territory) =>
        _territoryLocks.For(territory);
}
