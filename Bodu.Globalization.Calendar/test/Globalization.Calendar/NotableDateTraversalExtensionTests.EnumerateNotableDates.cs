// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTraversalExtensionTests.EnumerateNotableDates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateTraversalExtensionTests
{
    /// <summary>
    /// Verifies that the enumeration over a range yields every notable date intersecting the inclusive bounds, ordered
    /// by date.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenRangeContainsRules_ShouldYieldMatchingDatesInOrder()
    {
        NotableDate[] result = new DateOnly(2026, 4, 1).EnumerateNotableDates(new DateOnly(2026, 12, 31), CalendarService, "XX").ToArray();

        // April 1 (festival), April 25 (anzac), December 25 (christmas).
        CollectionAssert.AreEqual(
            new[] { "festival", "anzac-day", "christmas-day" },
            result.Select(r => r.NotableDateId).ToArray());
    }

    /// <summary>
    /// Verifies that a category filter restricts the range enumeration to matching notable dates only.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenFilterApplied_ShouldYieldOnlyMatchingDates()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);

        NotableDate[] result = new DateOnly(2026, 1, 1).EnumerateNotableDates(new DateOnly(2026, 12, 31), CalendarService, "XX", filter).ToArray();

        CollectionAssert.AreEqual(new[] { "festival" }, result.Select(r => r.NotableDateId).ToArray());
    }

    /// <summary>
    /// Verifies that a reversed range throws <see cref="ArgumentOutOfRangeException" /> under the v2 strict-range policy.
    /// </summary>
    [TestMethod]
    public void EnumerateNotableDates_WhenBoundariesReversed_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new DateOnly(2026, 12, 31).EnumerateNotableDates(new DateOnly(2026, 1, 1), CalendarService, "XX");
        });
    }
}
