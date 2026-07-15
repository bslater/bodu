// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheTestFactory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Builds deterministic <see cref="NotableDate" /> occurrences and <see cref="NotableDateCacheEntry" /> instances for
/// the caching tests.
/// </summary>
public static class NotableDateCacheTestFactory
{
    /// <summary>
    /// Builds a New Year's Day occurrence for the supplied year.
    /// </summary>
    /// <param name="year">The civil year.</param>
    /// <returns>A New Year's Day occurrence dated 1 January of the year.</returns>
    public static NotableDate NewYearsDay(int year) =>
        new(
            new DateOnly(year, 1, 1),
            ActualDate: null,
            IsObserved: false,
            new NotableDateRuleIdentity("resource.core", "new-year", "default"),
            "New Year's Day",
            "US",
            NotableDateCategory.PublicHoliday,
            Priority: 100,
            DurationDays: 1,
            IsNonWorkingDay: true,
            Tags: new[] { "public", "federal" },
            AdjustmentPolicyId: null,
            AdjustmentReason: null);

    /// <summary>
    /// Builds an occurrence exercising every field, including an observed adjustment and tags, for serialization
    /// round-trip coverage.
    /// </summary>
    /// <param name="year">The civil year.</param>
    /// <returns>A fully populated occurrence.</returns>
    public static NotableDate ObservedHoliday(int year) =>
        new(
            new DateOnly(year, 7, 5),
            new DateOnly(year, 7, 4),
            IsObserved: true,
            new NotableDateRuleIdentity("resource.us", "independence-day", "observed"),
            "Independence Day (observed)",
            "US",
            NotableDateCategory.BankHoliday,
            Priority: 50,
            DurationDays: 1,
            IsNonWorkingDay: true,
            Tags: Array.Empty<string>(),
            AdjustmentPolicyId: "weekend-substitution",
            AdjustmentReason: "Fell on a Saturday");

    /// <summary>
    /// Builds a cache entry for a territory and year with a single New Year's Day occurrence.
    /// </summary>
    /// <param name="territory">The territory.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="version">The resource version token.</param>
    /// <param name="computedAtUtc">The instant the year was computed.</param>
    /// <returns>A cache entry.</returns>
    public static NotableDateCacheEntry Entry(string territory, int year, string version, DateTimeOffset computedAtUtc) =>
        new(territory, year, version, new[] { NewYearsDay(year) }, computedAtUtc);

    /// <summary>
    /// Builds an empty cache entry for a territory and year, representing a computed year with no occurrences.
    /// </summary>
    /// <param name="territory">The territory.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="version">The resource version token.</param>
    /// <param name="computedAtUtc">The instant the year was computed.</param>
    /// <returns>An empty cache entry.</returns>
    public static NotableDateCacheEntry EmptyEntry(string territory, int year, string version, DateTimeOffset computedAtUtc) =>
        new(territory, year, version, Array.Empty<NotableDate>(), computedAtUtc);
}
