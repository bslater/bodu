// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangeResolutionCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Verifies the public surface of the chronological range-resolution cache, including key-based lookup, anchor
/// resolution, observed-date queries with context, and non-working day probing.
/// </summary>
[TestClass]
public sealed class NotableDateRangeResolutionCacheTests
{
    /// <summary>
    /// Verifies that a new cache is empty.
    /// </summary>
    [TestMethod]
    public void Count_WhenNewlyConstructed_ShouldBeZero()
    {
        NotableDateRangeResolutionCache cache = new();

        Assert.AreEqual(0, cache.Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.Add" /> rejects a <see langword="null" /> entry.
    /// </summary>
    [TestMethod]
    public void Add_WhenEntryIsNull_ShouldThrowExactly()
    {
        NotableDateRangeResolutionCache cache = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cache.Add(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.TryGet" /> returns <see langword="true" /> and the
    /// stored entry when the key matches.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenKeyMatches_ShouldReturnStoredEntry()
    {
        NotableDateCacheEntry entry = BuildEntry("New Year's Day", 2026, new DateTime(2026, 1, 1));
        NotableDateRangeResolutionCache cache = new();
        cache.Add(entry);

        NotableDateCacheKey key = new(entry.Rule.Name, entry.AnchorYear, entry.BaseNotable.TerritoryCode, entry.BaseNotable.CalendarType);

        Assert.IsTrue(cache.TryGet(key, out NotableDateCacheEntry resolved));
        Assert.AreSame(entry, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.TryGet" /> returns <see langword="false" /> when no entry
    /// matches the supplied key.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenKeyDoesNotMatch_ShouldReturnFalse()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("New Year's Day", 2026, new DateTime(2026, 1, 1)));

        NotableDateCacheKey missingKey = new("Other", 2026, null, null);

        Assert.IsFalse(cache.TryGet(missingKey, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.TryGet" /> distinguishes entries by territory in the key.
    /// </summary>
    [TestMethod]
    public void TryGet_WhenKeyTerritoryDiffers_ShouldReturnFalse()
    {
        NotableDateCacheEntry stored = BuildEntry("Labour Day", 2026, new DateTime(2026, 3, 9), territory: "AU-VIC");
        NotableDateRangeResolutionCache cache = new();
        cache.Add(stored);

        NotableDateCacheKey otherKey = new("Labour Day", 2026, "AU-NSW", null);

        Assert.IsFalse(cache.TryGet(otherKey, out _));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.Contains" /> mirrors <see cref="NotableDateRangeResolutionCache.TryGet" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenKeyMatches_ShouldReturnTrue()
    {
        NotableDateCacheEntry entry = BuildEntry("Anzac Day", 2026, new DateTime(2026, 4, 25));
        NotableDateRangeResolutionCache cache = new();
        cache.Add(entry);

        NotableDateCacheKey key = new(entry.Rule.Name, entry.AnchorYear, entry.BaseNotable.TerritoryCode, entry.BaseNotable.CalendarType);

        Assert.IsTrue(cache.Contains(key));
    }

    /// <summary>
    /// Verifies that adding an entry with the same key replaces the previous entry rather than appending a duplicate.
    /// </summary>
    [TestMethod]
    public void Add_WhenKeyAlreadyExists_ShouldReplaceExistingEntry()
    {
        NotableDateRangeResolutionCache cache = new();
        NotableDateCacheEntry first = BuildEntry("Holiday", 2026, new DateTime(2026, 6, 1));
        NotableDateCacheEntry second = BuildEntry("Holiday", 2026, new DateTime(2026, 6, 1), state: NotableDateCacheState.InWindow);

        cache.Add(first);
        cache.Add(second);

        Assert.AreEqual(1, cache.Count);

        NotableDateCacheKey key = new("Holiday", 2026, null, null);
        Assert.IsTrue(cache.TryGet(key, out NotableDateCacheEntry resolved));
        Assert.AreSame(second, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveAnchor" /> returns the anchor date for a matching
    /// rule name and year regardless of state.
    /// </summary>
    [TestMethod]
    public void ResolveAnchor_WhenMatchingRuleAndYear_ShouldReturnAnchorDate()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Easter Sunday", 2026, new DateTime(2026, 4, 5)));

        DateTime? anchor = cache.ResolveAnchor("Easter Sunday", 2026);

        Assert.AreEqual(new DateTime(2026, 4, 5), anchor);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveAnchor" /> ignores case when matching rule names.
    /// </summary>
    [TestMethod]
    public void ResolveAnchor_WhenRuleNameDiffersInCase_ShouldReturnAnchorDate()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Easter Sunday", 2026, new DateTime(2026, 4, 5)));

        Assert.AreEqual(new DateTime(2026, 4, 5), cache.ResolveAnchor("EASTER SUNDAY", 2026));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveAnchor" /> returns <see langword="null" /> when no
    /// entry matches the supplied year.
    /// </summary>
    [TestMethod]
    public void ResolveAnchor_WhenYearDoesNotMatch_ShouldReturnNull()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Easter Sunday", 2026, new DateTime(2026, 4, 5)));

        Assert.IsNull(cache.ResolveAnchor("Easter Sunday", 2025));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveObservedByName" /> returns the base date when no
    /// adjustment has been applied.
    /// </summary>
    [TestMethod]
    public void ResolveObservedByName_WhenNoAdjustment_ShouldReturnBaseDate()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("New Year's Day", 2026, new DateTime(2026, 1, 1)));

        Assert.AreEqual(new DateTime(2026, 1, 1), cache.ResolveObservedByName("New Year's Day", 2026, null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveObservedByName" /> prefers the adjusted date when
    /// one has been recorded.
    /// </summary>
    [TestMethod]
    public void ResolveObservedByName_WhenAdjustmentPresent_ShouldPreferAdjustedDate()
    {
        NotableDateCacheEntry entry = BuildEntry("New Year's Day", 2026, new DateTime(2026, 1, 1));
        entry.Adjusted = entry.BaseNotable with { Date = new DateTime(2026, 1, 2) };

        NotableDateRangeResolutionCache cache = new();
        cache.Add(entry);

        Assert.AreEqual(new DateTime(2026, 1, 2), cache.ResolveObservedByName("New Year's Day", 2026, null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveObservedByName" /> rejects entries whose territory
    /// does not match the requested context.
    /// </summary>
    [TestMethod]
    public void ResolveObservedByName_WhenRequestedTerritoryDoesNotMatchOwned_ShouldReturnNull()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Labour Day", 2026, new DateTime(2026, 3, 9), territory: "AU-VIC"));

        Assert.IsNull(cache.ResolveObservedByName("Labour Day", 2026, "AU-NSW", null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveObservedByName" /> matches territory-neutral entries
    /// against an explicit requested territory (the entry's null territory acts as a wildcard).
    /// </summary>
    [TestMethod]
    public void ResolveObservedByName_WhenOwnedTerritoryIsNull_ShouldMatchAnyRequestedTerritory()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Christmas Day", 2026, new DateTime(2026, 12, 25)));

        Assert.AreEqual(new DateTime(2026, 12, 25), cache.ResolveObservedByName("Christmas Day", 2026, "AU", null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.ResolveObservedByName" /> rejects entries whose calendar
    /// type does not match the requested calendar.
    /// </summary>
    [TestMethod]
    public void ResolveObservedByName_WhenRequestedCalendarDiffersFromOwned_ShouldReturnNull()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Rosh Hashanah", 2026, new DateTime(2026, 9, 12), calendarType: typeof(System.Globalization.HebrewCalendar)));

        Assert.IsNull(cache.ResolveObservedByName("Rosh Hashanah", 2026, null, typeof(System.Globalization.GregorianCalendar)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.IsNonWorkingDay" /> returns <see langword="true" /> when
    /// at least one cached entry covers the day and is flagged non-working.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenEntryCoversDay_ShouldReturnTrue()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Christmas Day", 2026, new DateTime(2026, 12, 25), isNonWorking: true));

        Assert.IsTrue(cache.IsNonWorkingDay(new DateTime(2026, 12, 25), null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.IsNonWorkingDay" /> returns <see langword="false" /> when
    /// the entry is flagged non-working but the requested date falls outside the span.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenDayOutsideEntrySpan_ShouldReturnFalse()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Christmas Day", 2026, new DateTime(2026, 12, 25), isNonWorking: true));

        Assert.IsFalse(cache.IsNonWorkingDay(new DateTime(2026, 12, 24), null, null));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangeResolutionCache.IsNonWorkingDay" /> returns <see langword="false" /> when
    /// the covered entry is not flagged non-working.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenEntryIsWorkingDay_ShouldReturnFalse()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("Casual Observance", 2026, new DateTime(2026, 5, 1), isNonWorking: false));

        Assert.IsFalse(cache.IsNonWorkingDay(new DateTime(2026, 5, 1), null, null));
    }

    /// <summary>
    /// Verifies that the cache <see cref="NotableDateRangeResolutionCache.EmissableEntries" /> yields only entries whose
    /// state qualifies them for emission.
    /// </summary>
    [TestMethod]
    public void EmissableEntries_WhenSomeEntriesAreComputed_ShouldOnlyYieldInWindowEntries()
    {
        NotableDateRangeResolutionCache cache = new();
        cache.Add(BuildEntry("InWindow", 2026, new DateTime(2026, 1, 1), state: NotableDateCacheState.InWindow));
        cache.Add(BuildEntry("Computed", 2026, new DateTime(2026, 1, 1), state: NotableDateCacheState.Computed));

        IEnumerable<NotableDateCacheEntry> emissable = cache.EmissableEntries();
        var names = emissable.Select(e => e.Rule.Name).ToList();

        Assert.AreEqual(1, names.Count);
        Assert.AreEqual("InWindow", names[0]);
    }

    /// <summary>
    /// Builds a minimal <see cref="NotableDateCacheEntry" /> for a fixed-date rule used by these tests.
    /// </summary>
    private static NotableDateCacheEntry BuildEntry(
        string name,
        int year,
        DateTime date,
        string? territory = null,
        Type? calendarType = null,
        bool isNonWorking = false,
        NotableDateCacheState state = NotableDateCacheState.Computed)
    {
        NotableDateRule rule = new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = date.Month,
            Day = date.Day,
            TerritoryCode = territory,
            CalendarType = calendarType,
            IsNonWorkingDay = isNonWorking,
        };

        RuleStaticProfile profile = new(rule, RuleTier.Fixed, null, 0, 0, 0);

        NotableDate notable = new()
        {
            Name = name,
            Date = date,
            Category = NotableDateCategory.Holiday,
            IsNonWorkingDay = isNonWorking,
            TerritoryCode = territory,
            CalendarType = calendarType,
            Tags = [],
        };

        return new NotableDateCacheEntry(profile, year, notable, state);
    }
}
