// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NullNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> that stores nothing, used when caching is disabled.
/// </summary>
public sealed class NullNotableDateCache
    : INotableDateCache
{
    /// <summary>The shared singleton, since the cache holds no state.</summary>
    private static readonly NullNotableDateCache s_instance = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NullNotableDateCache" /> class.
    /// </summary>
    private NullNotableDateCache()
    {
    }

    /// <summary>
    /// Gets the shared no-op cache instance.
    /// </summary>
    /// <value>A cache that stores nothing.</value>
    public static NullNotableDateCache Instance => s_instance;

    /// <inheritdoc />
    public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfNull(resourceVersion);

        // Intentionally never serves anything after validation: this cache stores nothing, but it still enforces the
        // same argument contract as every other backend.
        return null;
    }

    /// <inheritdoc />
    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf)
    {
        ThrowHelper.ThrowIfNull(entry);

        // Intentionally no-op after validation: this cache persists nothing, so it reports the write as skipped while
        // still enforcing the same argument contract as every other backend.
        return NotableDateCacheWriteStatus.Skipped;
    }

    /// <inheritdoc />
    public void Clear()
    {
        // Nothing is stored, so there is nothing to clear.
    }
}
