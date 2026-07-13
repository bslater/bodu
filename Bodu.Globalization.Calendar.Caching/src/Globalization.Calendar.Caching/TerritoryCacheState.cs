// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TerritoryCacheState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// The complete persisted state for a single territory: the per-year cache entries recorded for it, carried as one
/// immutable unit so a backend reads and writes them atomically.
/// </summary>
/// <remarks>
/// Each entry is a distinct civil year; the collection is the set of years a territory has computed and cached. The
/// shared <see cref="NotableDateCacheRules" /> apply freshness, version-matching, and merge policy over the entries, so
/// this type carries no policy of its own.
/// </remarks>
internal sealed class TerritoryCacheState
{
    /// <summary>The shared empty state, returned by a backend when a territory has no cached entries.</summary>
    private static readonly TerritoryCacheState s_empty = new(Array.Empty<NotableDateCacheEntry>());

    /// <summary>
    /// Initializes a new instance of the <see cref="TerritoryCacheState" /> class.
    /// </summary>
    /// <param name="entries">The per-year cache entries recorded for the territory.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries" /> is <see langword="null" />.</exception>
    public TerritoryCacheState(IReadOnlyList<NotableDateCacheEntry> entries)
    {
        ThrowHelper.ThrowIfNull(entries);

        Entries = entries;
    }

    /// <summary>
    /// Gets the shared empty state, carrying no cache entries.
    /// </summary>
    /// <value>An immutable state with an empty <see cref="Entries" /> list.</value>
    public static TerritoryCacheState Empty => s_empty;

    /// <summary>
    /// Gets the per-year cache entries recorded for the territory.
    /// </summary>
    /// <value>The cache entries, possibly empty.</value>
    public IReadOnlyList<NotableDateCacheEntry> Entries { get; }
}
