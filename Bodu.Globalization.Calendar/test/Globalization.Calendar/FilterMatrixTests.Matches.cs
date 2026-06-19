// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.Matches.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> matches an occurrence of every category when the
    /// category is an exact match.
    /// </summary>
    /// <param name="category">The category under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(NotableDateCategory.PublicHoliday)]
    [DataRow(NotableDateCategory.BankHoliday)]
    [DataRow(NotableDateCategory.Observance)]
    [DataRow(NotableDateCategory.Remembrance)]
    [DataRow(NotableDateCategory.Cultural)]
    [DataRow(NotableDateCategory.Religious)]
    [DataRow(NotableDateCategory.Seasonal)]
    [DataRow(NotableDateCategory.Civic)]
    [DataRow(NotableDateCategory.School)]
    [DataRow(NotableDateCategory.Regional)]
    [DataRow(NotableDateCategory.Other)]
    public void Matches_WhenForCategoryAndCategoryEquals_ShouldReturnTrue(NotableDateCategory category)
    {
        var filter = NotableDateFilter.ForCategory(category);

        Assert.IsTrue(filter.Matches(Occurrence(category: category)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> rejects an occurrence whose category differs from the
    /// requested one.
    /// </summary>
    /// <param name="filterCategory">The category requested by the filter.</param>
    /// <param name="occurrenceCategory">The category of the occurrence under test.</param>
    [TestMethod]
    [DataRow(NotableDateCategory.PublicHoliday, NotableDateCategory.Observance)]
    [DataRow(NotableDateCategory.Cultural, NotableDateCategory.Seasonal)]
    [DataRow(NotableDateCategory.Religious, NotableDateCategory.PublicHoliday)]
    public void Matches_WhenForCategoryAndCategoryDiffers_ShouldReturnFalse(NotableDateCategory filterCategory, NotableDateCategory occurrenceCategory)
    {
        var filter = NotableDateFilter.ForCategory(filterCategory);

        Assert.IsFalse(filter.Matches(Occurrence(category: occurrenceCategory)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> matches an occurrence whose category is one of the
    /// accepted values.
    /// </summary>
    [TestMethod]
    public void Matches_WhenForAnyCategoryAndCategoryAccepted_ShouldReturnTrue()
    {
        var filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.PublicHoliday, NotableDateCategory.Observance);

        Assert.IsTrue(filter.Matches(Occurrence(category: NotableDateCategory.Observance)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> rejects an occurrence whose category is not one of
    /// the accepted values.
    /// </summary>
    [TestMethod]
    public void Matches_WhenForAnyCategoryAndCategoryNotAccepted_ShouldReturnFalse()
    {
        var filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.PublicHoliday, NotableDateCategory.Observance);

        Assert.IsFalse(filter.Matches(Occurrence(category: NotableDateCategory.Cultural)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> with no categories produces a filter that matches
    /// nothing rather than throwing, since the v2 factory does not reject an empty set.
    /// </summary>
    [TestMethod]
    public void Matches_WhenForAnyCategoryIsEmpty_ShouldMatchNothing()
    {
        var filter = NotableDateFilter.ForAnyCategory();

        Assert.IsFalse(filter.Matches(Occurrence(category: NotableDateCategory.PublicHoliday)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> matches an occurrence carrying the exact tag and rejects
    /// an occurrence without it.
    /// </summary>
    /// <param name="tag">The tag requested by the filter.</param>
    /// <param name="expected">Whether the occurrence is expected to match.</param>
    [TestMethod]
    [DataRow("Public", true)]
    [DataRow("Christian", true)]
    [DataRow("Jewish", false)]
    [DataRow("Nonexistent", false)]
    public void Matches_WhenWithTag_ShouldReflectTagPresence(string tag, bool expected)
    {
        var filter = NotableDateFilter.WithTag(tag);

        Assert.AreEqual(expected, filter.Matches(Occurrence(tags: ["Christian", "Public"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> is case-sensitive, rejecting a tag that differs only in
    /// case — a deliberate change from the v1 case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithTagCaseDiffers_ShouldReturnFalse()
    {
        var filter = NotableDateFilter.WithTag("PUBLIC");

        Assert.IsFalse(filter.Matches(Occurrence(tags: ["Public"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> matches an occurrence carrying at least one accepted tag.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAnyTagAndTagIntersects_ShouldReturnTrue()
    {
        var filter = NotableDateFilter.WithAnyTag("Public", "Federal");

        Assert.IsTrue(filter.Matches(Occurrence(tags: ["Regional", "Federal"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> rejects an occurrence carrying none of the accepted tags.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAnyTagAndNoTagIntersects_ShouldReturnFalse()
    {
        var filter = NotableDateFilter.WithAnyTag("Public", "Federal");

        Assert.IsFalse(filter.Matches(Occurrence(tags: ["Christian"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> matches an occurrence carrying every required tag.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAllTagsAndEveryTagPresent_ShouldReturnTrue()
    {
        var filter = NotableDateFilter.WithAllTags("Public", "Christian");

        Assert.IsTrue(filter.Matches(Occurrence(tags: ["Christian", "Public", "Federal"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> rejects an occurrence missing any required tag.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAllTagsAndATagMissing_ShouldReturnFalse()
    {
        var filter = NotableDateFilter.WithAllTags("Public", "Christian");

        Assert.IsFalse(filter.Matches(Occurrence(tags: ["Public"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> with no required tags matches every occurrence, since
    /// the empty-set conjunction is vacuously satisfied.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAllTagsIsEmpty_ShouldMatchEveryOccurrence()
    {
        var filter = NotableDateFilter.WithAllTags();

        Assert.IsTrue(filter.Matches(Occurrence(tags: [])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> matches an occurrence whose display name equals the
    /// requested name and rejects a differing name.
    /// </summary>
    /// <param name="displayName">The display name carried by the occurrence under test.</param>
    /// <param name="expected">Whether the occurrence is expected to match the requested name.</param>
    [TestMethod]
    [DataRow("Christmas Day", true)]   // exact match
    [DataRow("Easter Sunday", false)]  // differing name
    public void Matches_WhenWithName_ShouldReflectNameEquality(string displayName, bool expected)
    {
        var filter = NotableDateFilter.WithName("Christmas Day");

        Assert.AreEqual(expected, filter.Matches(Occurrence(displayName: displayName)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> is case-sensitive, rejecting a name that differs only in
    /// case — a deliberate change from the v1 case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithNameCaseDiffers_ShouldReturnFalse()
    {
        var filter = NotableDateFilter.WithName("christmas day");

        Assert.IsFalse(filter.Matches(Occurrence(displayName: "Christmas Day")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> matches an occurrence whose display name is one of the
    /// accepted values and rejects one that is not.
    /// </summary>
    /// <param name="displayName">The display name carried by the occurrence under test.</param>
    /// <param name="expected">Whether the occurrence is expected to match one of the accepted names.</param>
    [TestMethod]
    [DataRow("Easter Sunday", true)]  // accepted member
    [DataRow("Anzac Day", false)]     // not a member
    public void Matches_WhenWithAnyName_ShouldReflectMembership(string displayName, bool expected)
    {
        var filter = NotableDateFilter.WithAnyName("Christmas Day", "Easter Sunday");

        Assert.AreEqual(expected, filter.Matches(Occurrence(displayName: displayName)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithId" /> matches an occurrence produced by the requested concept id
    /// and rejects one produced by a different concept.
    /// </summary>
    /// <param name="notableDateId">The concept id carried by the occurrence under test.</param>
    /// <param name="expected">Whether the occurrence is expected to match the requested id.</param>
    [TestMethod]
    [DataRow("christmas-day", true)]  // requested concept
    [DataRow("boxing-day", false)]    // different concept
    public void Matches_WhenWithId_ShouldReflectConceptIdentity(string notableDateId, bool expected)
    {
        var filter = NotableDateFilter.WithId("christmas-day");

        Assert.AreEqual(expected, filter.Matches(Occurrence(notableDateId: notableDateId)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithId" /> is case-sensitive, rejecting an id that differs only in
    /// case.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithIdCaseDiffers_ShouldReturnFalse()
    {
        var filter = NotableDateFilter.WithId("Christmas-Day");

        Assert.IsFalse(filter.Matches(Occurrence(notableDateId: "christmas-day")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> matches a non-working occurrence and rejects a
    /// working one.
    /// </summary>
    /// <param name="isNonWorkingDay">The non-working flag on the occurrence.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Matches_WhenIsNonWorkingDay_ShouldReflectFlag(bool isNonWorkingDay)
    {
        var filter = NotableDateFilter.IsNonWorkingDay();

        Assert.AreEqual(isNonWorkingDay, filter.Matches(Occurrence(isNonWorkingDay: isNonWorkingDay)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> matches an observed (adjusted) occurrence and rejects
    /// an unadjusted one.
    /// </summary>
    /// <param name="isObserved">The observed flag on the occurrence.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Matches_WhenWasAdjusted_ShouldReflectObservedFlag(bool isObserved)
    {
        var filter = NotableDateFilter.WasAdjusted();

        Assert.AreEqual(isObserved, filter.Matches(Occurrence(isObserved: isObserved)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithMinDuration" /> matches an occurrence whose span meets or exceeds
    /// the minimum and rejects a shorter one.
    /// </summary>
    /// <param name="minimumDays">The minimum inclusive duration requested.</param>
    /// <param name="durationDays">The duration of the occurrence under test.</param>
    /// <param name="expected">Whether the occurrence is expected to match.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1, 1, true)]
    [DataRow(1, 5, true)]
    [DataRow(2, 1, false)]
    [DataRow(3, 3, true)]
    [DataRow(3, 2, false)]
    [DataRow(7, 10, true)]
    [DataRow(7, 6, false)]
    public void Matches_WhenWithMinDuration_ShouldReflectThreshold(int minimumDays, int durationDays, bool expected)
    {
        var filter = NotableDateFilter.WithMinDuration(minimumDays);

        Assert.AreEqual(expected, filter.Matches(Occurrence(durationDays: durationDays)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> matches an occurrence whose emitted date falls within
    /// the inclusive range, including the boundaries, and rejects one outside it.
    /// </summary>
    /// <param name="year">The emitted year.</param>
    /// <param name="month">The emitted month.</param>
    /// <param name="day">The emitted day.</param>
    /// <param name="expected">Whether the occurrence is expected to match the 1–30 June 2024 range.</param>
    [TestMethod]
    [DataRow(2024, 6, 1, true)]   // inclusive start boundary
    [DataRow(2024, 6, 30, true)]  // inclusive end boundary
    [DataRow(2024, 6, 15, true)]  // interior
    [DataRow(2024, 5, 31, false)] // day before start
    [DataRow(2024, 7, 1, false)]  // day after end
    public void Matches_WhenInDateRange_ShouldReflectMembership(int year, int month, int day, bool expected)
    {
        var filter = NotableDateFilter.InDateRange(new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 30));

        Assert.AreEqual(expected, filter.Matches(Occurrence(date: new DateOnly(year, month, day))));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> matches when the start and end coincide and the
    /// occurrence falls on that single day, and rejects an adjacent day.
    /// </summary>
    /// <param name="year">The emitted year.</param>
    /// <param name="month">The emitted month.</param>
    /// <param name="day">The emitted day.</param>
    /// <param name="expected">Whether the occurrence is expected to match the single-day 15 June 2024 range.</param>
    [TestMethod]
    [DataRow(2024, 6, 15, true)]   // the single day
    [DataRow(2024, 6, 16, false)]  // adjacent day
    public void Matches_WhenInDateRangeStartEqualsEnd_ShouldMatchOnlyThatDay(int year, int month, int day, bool expected)
    {
        DateOnly single = new(2024, 6, 15);
        var filter = NotableDateFilter.InDateRange(single, single);

        Assert.AreEqual(expected, filter.Matches(Occurrence(date: new DateOnly(year, month, day))));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Matches" /> throws <see cref="ArgumentNullException" /> when the
    /// occurrence is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Matches_WhenNotableDateIsNull_ShouldThrowExactly()
    {
        var filter = NotableDateFilter.IsNonWorkingDay();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.Matches(null!);
        });
    }
}
