// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> test double that delegates to an in-memory cache while counting per-year and
/// batch reads. The batch seam can be disabled at construction so the caching service's per-year fallback path can be
/// exercised against the same stored state.
/// </summary>
internal sealed class CountingNotableDateCache
    : INotableDateCache
    , INotableDateCacheBatchReader
{
    /// <summary>The in-memory cache that stores the actual state.</summary>
    private readonly InMemoryNotableDateCache _inner = new();

    /// <summary>Whether the batch seam is exposed through a wrapping reader.</summary>
    private readonly bool _supportsBatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingNotableDateCache" /> class.
    /// </summary>
    /// <param name="supportsBatch">
    /// <see langword="true" /> to expose the batch seam; <see langword="false" /> to expose only the per-year surface,
    /// exercising the caller's fallback path.
    /// </param>
    public CountingNotableDateCache(bool supportsBatch)
    {
        _supportsBatch = supportsBatch;
    }

    /// <summary>
    /// Gets the number of <see cref="GetYear" /> calls observed.
    /// </summary>
    /// <value>The per-year read count.</value>
    public int GetYearCount { get; private set; }

    /// <summary>
    /// Gets the number of batch reads observed.
    /// </summary>
    /// <value>The batch read count.</value>
    public int GetYearsCount { get; private set; }

    /// <summary>
    /// Creates the cache surface the service under test should consume: the double itself when the batch seam is
    /// enabled, or a plain per-year wrapper otherwise.
    /// </summary>
    /// <returns>The cache to hand to the caching service.</returns>
    public INotableDateCache AsCache() =>
        _supportsBatch ? this : new PerYearOnly(this);

    /// <inheritdoc />
    public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        GetYearCount++;
        return _inner.GetYear(territory, year, resourceVersion, ttl, asOf);
    }

    /// <inheritdoc />
    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf) =>
        _inner.StoreYear(entry, ttl, asOf);

    /// <inheritdoc />
    public void Clear() =>
        _inner.Clear();

    /// <inheritdoc />
    NotableDateCacheEntry?[] INotableDateCacheBatchReader.GetYears(string territory, int firstYear, int lastYear, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf)
    {
        GetYearsCount++;
        return ((INotableDateCacheBatchReader)_inner).GetYears(territory, firstYear, lastYear, resourceVersion, ttl, asOf);
    }

    /// <summary>
    /// A wrapper exposing only the per-year <see cref="INotableDateCache" /> surface of a counting double, so the
    /// consumer cannot discover the batch seam.
    /// </summary>
    private sealed class PerYearOnly
        : INotableDateCache
    {
        /// <summary>The counting double the calls are forwarded to.</summary>
        private readonly CountingNotableDateCache _owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerYearOnly" /> class.
        /// </summary>
        /// <param name="owner">The counting double the calls are forwarded to.</param>
        public PerYearOnly(CountingNotableDateCache owner)
        {
            _owner = owner;
        }

        /// <inheritdoc />
        public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf) =>
            _owner.GetYear(territory, year, resourceVersion, ttl, asOf);

        /// <inheritdoc />
        public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf) =>
            _owner.StoreYear(entry, ttl, asOf);

        /// <inheritdoc />
        public void Clear() =>
            _owner.Clear();
    }
}
