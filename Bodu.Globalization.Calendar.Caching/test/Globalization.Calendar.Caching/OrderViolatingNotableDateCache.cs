// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderViolatingNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> test double that always serves a year whose occurrences are deliberately out of
/// date order, violating the ordering contract, so the caching service's sort fallback can be exercised.
/// </summary>
internal sealed class OrderViolatingNotableDateCache : INotableDateCache
{
    /// <inheritdoc />
    public NotableDateCacheEntry? GetYear(string territory, int year, string resourceVersion, TimeSpan ttl, DateTimeOffset asOf) =>
        new(
            territory,
            year,
            resourceVersion,
            new[] { NotableDateCacheTestFactory.ObservedHoliday(year), NotableDateCacheTestFactory.NewYearsDay(year) },
            asOf);

    /// <inheritdoc />
    public NotableDateCacheWriteStatus StoreYear(NotableDateCacheEntry entry, TimeSpan ttl, DateTimeOffset asOf) =>
        NotableDateCacheWriteStatus.Skipped;

    /// <inheritdoc />
    public void Clear()
    {
    }
}
