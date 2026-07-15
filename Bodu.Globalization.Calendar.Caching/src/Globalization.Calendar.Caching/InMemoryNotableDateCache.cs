// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> that stores computed years in memory for the lifetime of the instance, expiring
/// them through the same freshness and version mechanism as the file caches.
/// </summary>
/// <remarks>
/// <para>
/// Entries expire by time-to-live and resource version rather than by storage: the shared
/// <see cref="NotableDateCacheBase{TOptions}" /> filters stale and superseded entries on read and prunes them on write.
/// Nothing is persisted, so the cache starts empty on each process and is a drop-in alternative to the file caches when
/// on-disk files are unwanted.
/// </para>
/// <para>
/// Reads and writes are individually thread-safe. As with every <see cref="INotableDateCache" />, a concurrent
/// store-after-store for the same territory resolves last-write-wins, which is acceptable for a best-effort cache.
/// </para>
/// </remarks>
public sealed class InMemoryNotableDateCache
    : NotableDateCacheBase<NotableDateCacheOptions>
{
    /// <summary>The in-memory store of per-territory entries, keyed by the normalized territory. Each value is the freshly merged list the base handed to <see cref="WriteEntries" />, owned by the cache from that point and never mutated.</summary>
    private readonly ConcurrentDictionary<string, IReadOnlyList<NotableDateCacheEntry>> _store = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryNotableDateCache" /> class.
    /// </summary>
    /// <param name="options">The cache options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    public InMemoryNotableDateCache(NotableDateCacheOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryNotableDateCache" /> class with default options.
    /// </summary>
    public InMemoryNotableDateCache()
        : this(new NotableDateCacheOptions())
    {
    }

    /// <inheritdoc />
    public override void Clear() =>
        _store.Clear();

    /// <inheritdoc />
    protected internal override IReadOnlyList<NotableDateCacheEntry> ReadEntries(string territory) =>
        _store.TryGetValue(territory, out IReadOnlyList<NotableDateCacheEntry>? entries) ? entries : Array.Empty<NotableDateCacheEntry>();

    /// <inheritdoc />
    protected internal override bool WriteEntries(string territory, IReadOnlyList<NotableDateCacheEntry> entries)
    {
        // Drop the territory entirely when nothing remains to retain, so an empty state does not linger in the map.
        if (entries.Count == 0)
        {
            _store.TryRemove(territory, out _);
            return true;
        }

        // The base hands over a freshly merged list per write, so it is stored directly without a defensive copy.
        _store[territory] = entries;

        // An in-memory dictionary write cannot fail, so the write is always durable for the instance lifetime.
        return true;
    }
}
