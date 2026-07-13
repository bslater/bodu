// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheBase{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the storage-agnostic mechanism for an <see cref="INotableDateCache" />: territory normalization, read-time
/// freshness and version filtering, and write-time merge-and-prune of per-year entries. Derived types implement only
/// the persistence of a <see cref="TerritoryCacheState" />; this base prescribes no physical storage structure.
/// </summary>
/// <typeparam name="TOptions">The options type carrying any storage settings.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ReadState" /> returns the raw stored entries without filtering; this base applies the freshness and
/// version policy in <see cref="GetYear" /> and prunes stale and superseded entries in <see cref="StoreYear" />, so the
/// backing store self-cleans on every write. The read-modify-write sequence in <see cref="StoreYear" /> runs under a
/// per-territory lock so concurrent writes to the same territory cannot interleave and lose an entry.
/// </para>
/// <para>
/// The freshness, validity, version-matching, and merge rules are delegated to the shared
/// <see cref="NotableDateCacheRules" /> so this base and the SQLite and distributed backends apply one authoritative
/// policy. This base contributes only the per-territory locking and the read-modify-write sequencing over a
/// <see cref="TerritoryCacheState" />.
/// </para>
/// </remarks>
public abstract class NotableDateCacheBase<TOptions>
    : INotableDateCache
    where TOptions : NotableDateCacheOptions
{
    /// <summary>The validated options carrying any storage settings.</summary>
    private readonly TOptions _options;

    /// <summary>The striped per-territory locks guarding the read-modify-write sequence in <see cref="StoreYear" />. One lock object is created per territory on first use and reused thereafter.</summary>
    private readonly ConcurrentDictionary<string, object> _territoryLocks = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateCacheBase{TOptions}" /> class.
    /// </summary>
    /// <param name="options">The options carrying any storage settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
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
    public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(resourceVersion);

        string key = NotableDateCacheRules.NormalizeTerritory(territory);

        IReadOnlyList<NotableDateCacheEntry> entries = ReadState(key).Entries;
        if (entries.Count == 0)
            return null;

        return NotableDateCacheRules.SelectFresh(entries, year, resourceVersion, ttl, asOf);
    }

    /// <inheritdoc />
    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(entry);

        string key = NotableDateCacheRules.NormalizeTerritory(entry.Territory);

        lock (LockFor(key))
        {
            TerritoryCacheState state = ReadState(key);

            List<NotableDateCacheEntry> merged = NotableDateCacheRules.Merge(state.Entries, entry, ttl, asOf);

            return WriteState(key, new TerritoryCacheState(merged))
                ? NotableDateCacheWriteStatus.Stored
                : NotableDateCacheWriteStatus.Failed;
        }
    }

    /// <inheritdoc />
    public abstract void Clear();

    /// <summary>
    /// Reads the raw, unfiltered persisted state for a normalized territory.
    /// </summary>
    /// <param name="territory">The normalized territory key.</param>
    /// <returns>The stored state, or <see cref="TerritoryCacheState.Empty" /> when none is available or the read fails.</returns>
    /// <remarks>
    /// Declared <see langword="private protected" /> because <see cref="TerritoryCacheState" /> is an internal storage
    /// detail: the seam is open only to backends within this assembly. An out-of-assembly backend implements the public
    /// <see cref="INotableDateCache" /> contract directly instead, as <see cref="NullNotableDateCache" /> does.
    /// </remarks>
    private protected abstract TerritoryCacheState ReadState(string territory);

    /// <summary>
    /// Writes the supplied state for a normalized territory, replacing any existing state.
    /// </summary>
    /// <param name="territory">The normalized territory key.</param>
    /// <param name="state">The state to persist.</param>
    /// <returns>
    /// <see langword="true" /> when the state was persisted, including the deliberate deletion of an empty state;
    /// <see langword="false" /> when a storage failure was swallowed and nothing was persisted.
    /// </returns>
    /// <remarks>
    /// Declared <see langword="private protected" /> for the same reason as <see cref="ReadState" />. The
    /// <see cref="bool" /> result lets <see cref="StoreYear" /> distinguish a durable write from a best-effort backend
    /// that swallowed a fault, so a failed write is reported as <see cref="NotableDateCacheWriteStatus.Failed" /> rather
    /// than falsely as <see cref="NotableDateCacheWriteStatus.Stored" />.
    /// </remarks>
    private protected abstract bool WriteState(string territory, TerritoryCacheState state);

    /// <summary>
    /// Returns the lock object guarding writes for the supplied normalized territory, creating it on first use.
    /// </summary>
    /// <param name="territory">The normalized territory whose write lock is required.</param>
    /// <returns>The per-territory lock object.</returns>
    private object LockFor(string territory) =>
        _territoryLocks.GetOrAdd(territory, static _ => new object());
}
