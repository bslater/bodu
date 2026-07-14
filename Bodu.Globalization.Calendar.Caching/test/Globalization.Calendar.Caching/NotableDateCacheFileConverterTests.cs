// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheFileConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the <see cref="NotableDateCacheFileConverter" /> round-trips a territory's cache state through the persisted
/// file shape without loss.
/// </summary>
[TestClass]
public sealed partial class NotableDateCacheFileConverterTests
{
    /// <summary>The fixed instant used as the computed timestamp.</summary>
    private static readonly DateTimeOffset ComputedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    /// <summary>
    /// Asserts that two occurrences are equal field by field, comparing tags by sequence because record equality treats
    /// the tag list by reference.
    /// </summary>
    /// <param name="expected">The expected occurrence.</param>
    /// <param name="actual">The actual occurrence.</param>
    internal static void AssertOccurrenceEqual(NotableDate expected, NotableDate actual)
    {
        Assert.AreEqual(expected.Date, actual.Date);
        Assert.AreEqual(expected.ActualDate, actual.ActualDate);
        Assert.AreEqual(expected.IsObserved, actual.IsObserved);
        Assert.AreEqual(expected.Identity, actual.Identity);
        Assert.AreEqual(expected.DisplayName, actual.DisplayName);
        Assert.AreEqual(expected.TerritoryCode, actual.TerritoryCode);
        Assert.AreEqual(expected.Category, actual.Category);
        Assert.AreEqual(expected.Priority, actual.Priority);
        Assert.AreEqual(expected.DurationDays, actual.DurationDays);
        Assert.AreEqual(expected.IsNonWorkingDay, actual.IsNonWorkingDay);
        CollectionAssert.AreEqual(expected.Tags.ToArray(), actual.Tags.ToArray());
        Assert.AreEqual(expected.AdjustmentPolicyId, actual.AdjustmentPolicyId);
        Assert.AreEqual(expected.AdjustmentReason, actual.AdjustmentReason);
    }

    /// <summary>
    /// Verifies that converting populated entries to a file and back preserves every entry's territory, year, version,
    /// computed instant, and occurrence fields.
    /// </summary>
    [TestMethod]
    public void ToEntriesOfToFile_WhenEntriesPopulated_ShouldRoundTripEveryField()
    {
        NotableDateCacheEntry populated = new(
            "US",
            2026,
            "v1",
            new[] { NotableDateCacheTestFactory.NewYearsDay(2026), NotableDateCacheTestFactory.ObservedHoliday(2026) },
            ComputedAt);
        NotableDateCacheEntry empty = NotableDateCacheTestFactory.EmptyEntry("US", 2027, "v1", ComputedAt);

        IReadOnlyList<NotableDateCacheEntry> result =
            NotableDateCacheFileConverter.ToEntries(NotableDateCacheFileConverter.ToFile("US", new[] { populated, empty }));

        Assert.AreEqual(2, result.Count);

        NotableDateCacheEntry roundTripped = result.Single(e => e.Year == 2026);
        Assert.AreEqual("US", roundTripped.Territory);
        Assert.AreEqual("v1", roundTripped.ResourceVersion);
        Assert.AreEqual(ComputedAt, roundTripped.ComputedAtUtc);
        Assert.AreEqual(2, roundTripped.Occurrences.Count);
        AssertOccurrenceEqual(NotableDateCacheTestFactory.NewYearsDay(2026), roundTripped.Occurrences.Single(o => o.NotableDateId == "new-year"));
        AssertOccurrenceEqual(NotableDateCacheTestFactory.ObservedHoliday(2026), roundTripped.Occurrences.Single(o => o.NotableDateId == "independence-day"));

        NotableDateCacheEntry emptyRoundTripped = result.Single(e => e.Year == 2027);
        Assert.AreEqual(0, emptyRoundTripped.Occurrences.Count);
    }

    /// <summary>
    /// Verifies that converting an empty entry list produces a file that reconstructs to an empty list.
    /// </summary>
    [TestMethod]
    public void ToEntriesOfToFile_WhenEntriesEmpty_ShouldRoundTripToEmpty()
    {
        IReadOnlyList<NotableDateCacheEntry> result =
            NotableDateCacheFileConverter.ToEntries(NotableDateCacheFileConverter.ToFile("US", Array.Empty<NotableDateCacheEntry>()));

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that occurrences round-trip in stored order, the ordering contract the caching service relies on.
    /// </summary>
    [TestMethod]
    public void ToEntriesOfToFile_WhenMultipleOccurrences_ShouldPreserveStoredOrder()
    {
        // Deliberately not date-sorted: order preservation, not sortedness, is the contract under test.
        NotableDate july = NotableDateCacheTestFactory.ObservedHoliday(2026);
        NotableDate january = NotableDateCacheTestFactory.NewYearsDay(2026);
        var entry = new NotableDateCacheEntry("US", 2026, "v1", new[] { july, january }, ComputedAt);

        IReadOnlyList<NotableDateCacheEntry> result =
            NotableDateCacheFileConverter.ToEntries(NotableDateCacheFileConverter.ToFile("US", new[] { entry }));

        Assert.AreEqual("independence-day", result[0].Occurrences[0].NotableDateId);
        Assert.AreEqual("new-year", result[0].Occurrences[1].NotableDateId);
    }
}
