// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies every single-factory <see cref="NotableDateFilter" /> predicate against directly constructed
/// <see cref="NotableDate" /> occurrences using the v2 single-stage <see cref="NotableDateFilter.Matches" /> surface,
/// together with the factory argument validation.
/// </summary>
/// <remarks>
/// <para>
/// The v1 filter exposed a two-stage gate (rule-level <c>IsRuleEligible</c> and date-level <c>IsMatch</c>); the v2
/// filter is single-stage and is evaluated against a resolved occurrence, so the v1 rule-gate and date-gate rows
/// collapse into a single <see cref="NotableDateFilter.Matches" /> assertion. The v2 string predicates
/// (<see cref="NotableDateFilter.WithTag" />, <see cref="NotableDateFilter.WithName" />,
/// <see cref="NotableDateFilter.WithId" />, and their multi-valued variants) match with
/// <see cref="System.StringComparison.Ordinal" /> rather than the v1 case-insensitive comparison.
/// </para>
/// </remarks>
[TestClass]
public sealed class FilterMatrixTests
{
    /// <summary>
    /// Builds a resolved occurrence with the supplied characteristics for direct filter evaluation.
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="notableDateId">The notable-date concept id.</param>
    /// <param name="category">The category.</param>
    /// <param name="isNonWorkingDay">Whether the occurrence is a non-working day.</param>
    /// <param name="isObserved">Whether the emitted date was adjusted from the calculated date.</param>
    /// <param name="tags">The tags carried by the occurrence.</param>
    /// <param name="date">The emitted date.</param>
    /// <param name="durationDays">The span length in days.</param>
    /// <returns>The constructed occurrence.</returns>
    private static NotableDate Occurrence(
        string displayName = "Test",
        string notableDateId = "test",
        NotableDateCategory category = NotableDateCategory.PublicHoliday,
        bool isNonWorkingDay = false,
        bool isObserved = false,
        string[]? tags = null,
        DateOnly? date = null,
        int durationDays = 1) =>
        new(
            Date: date ?? new DateOnly(2024, 1, 1),
            ActualDate: date ?? new DateOnly(2024, 1, 1),
            IsObserved: isObserved,
            Identity: new NotableDateRuleIdentity("res", notableDateId, "rule"),
            DisplayName: displayName,
            TerritoryCode: "XX",
            Category: category,
            Priority: 0,
            DurationDays: durationDays,
            IsNonWorkingDay: isNonWorkingDay,
            Tags: tags ?? [],
            AdjustmentPolicyId: null,
            AdjustmentReason: null);

    // -----------------------------------------------------------------------------------------------------------
    // ForCategory / ForAnyCategory
    // -----------------------------------------------------------------------------------------------------------

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
        NotableDateFilter filter = NotableDateFilter.ForCategory(category);

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
        NotableDateFilter filter = NotableDateFilter.ForCategory(filterCategory);

        Assert.IsFalse(filter.Matches(Occurrence(category: occurrenceCategory)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> matches an occurrence whose category is one of the
    /// accepted values.
    /// </summary>
    [TestMethod]
    public void Matches_WhenForAnyCategoryAndCategoryAccepted_ShouldReturnTrue()
    {
        NotableDateFilter filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.PublicHoliday, NotableDateCategory.Observance);

        Assert.IsTrue(filter.Matches(Occurrence(category: NotableDateCategory.Observance)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> rejects an occurrence whose category is not one of
    /// the accepted values.
    /// </summary>
    [TestMethod]
    public void Matches_WhenForAnyCategoryAndCategoryNotAccepted_ShouldReturnFalse()
    {
        NotableDateFilter filter = NotableDateFilter.ForAnyCategory(NotableDateCategory.PublicHoliday, NotableDateCategory.Observance);

        Assert.IsFalse(filter.Matches(Occurrence(category: NotableDateCategory.Cultural)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> throws <see cref="ArgumentNullException" /> when the
    /// categories array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ForAnyCategory_WhenCategoriesIsNull_ShouldThrowExactly()
    {
        NotableDateCategory[] categories = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.ForAnyCategory(categories);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForAnyCategory" /> with no categories produces a filter that matches
    /// nothing rather than throwing, since the v2 factory does not reject an empty set.
    /// </summary>
    [TestMethod]
    public void Matches_WhenForAnyCategoryIsEmpty_ShouldMatchNothing()
    {
        NotableDateFilter filter = NotableDateFilter.ForAnyCategory();

        Assert.IsFalse(filter.Matches(Occurrence(category: NotableDateCategory.PublicHoliday)));
    }

    // -----------------------------------------------------------------------------------------------------------
    // WithTag / WithAnyTag / WithAllTags
    // -----------------------------------------------------------------------------------------------------------

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
        NotableDateFilter filter = NotableDateFilter.WithTag(tag);

        Assert.AreEqual(expected, filter.Matches(Occurrence(tags: ["Christian", "Public"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> is case-sensitive, rejecting a tag that differs only in
    /// case — a deliberate change from the v1 case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithTagCaseDiffers_ShouldReturnFalse()
    {
        NotableDateFilter filter = NotableDateFilter.WithTag("PUBLIC");

        Assert.IsFalse(filter.Matches(Occurrence(tags: ["Public"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> throws <see cref="ArgumentNullException" /> when the tag is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithTag_WhenTagIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithTag(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> matches an occurrence carrying at least one accepted tag
    /// and rejects one carrying none.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAnyTag_ShouldReflectIntersection()
    {
        NotableDateFilter filter = NotableDateFilter.WithAnyTag("Public", "Federal");

        Assert.IsTrue(filter.Matches(Occurrence(tags: ["Regional", "Federal"])));
        Assert.IsFalse(filter.Matches(Occurrence(tags: ["Christian"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyTag" /> throws <see cref="ArgumentNullException" /> when the tags
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAnyTag_WhenTagsIsNull_ShouldThrowExactly()
    {
        string[] tags = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAnyTag(tags);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> matches an occurrence carrying every required tag and
    /// rejects one missing any of them.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAllTags_ShouldRequireEveryTag()
    {
        NotableDateFilter filter = NotableDateFilter.WithAllTags("Public", "Christian");

        Assert.IsTrue(filter.Matches(Occurrence(tags: ["Christian", "Public", "Federal"])));
        Assert.IsFalse(filter.Matches(Occurrence(tags: ["Public"])));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> throws <see cref="ArgumentNullException" /> when the
    /// tags array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAllTags_WhenTagsIsNull_ShouldThrowExactly()
    {
        string[] tags = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAllTags(tags);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAllTags" /> with no required tags matches every occurrence, since
    /// the empty-set conjunction is vacuously satisfied.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAllTagsIsEmpty_ShouldMatchEveryOccurrence()
    {
        NotableDateFilter filter = NotableDateFilter.WithAllTags();

        Assert.IsTrue(filter.Matches(Occurrence(tags: [])));
    }

    // -----------------------------------------------------------------------------------------------------------
    // WithName / WithAnyName
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> matches an occurrence whose display name equals the
    /// requested name and rejects a differing name.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithName_ShouldReflectNameEquality()
    {
        NotableDateFilter filter = NotableDateFilter.WithName("Christmas Day");

        Assert.IsTrue(filter.Matches(Occurrence(displayName: "Christmas Day")));
        Assert.IsFalse(filter.Matches(Occurrence(displayName: "Easter Sunday")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> is case-sensitive, rejecting a name that differs only in
    /// case — a deliberate change from the v1 case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithNameCaseDiffers_ShouldReturnFalse()
    {
        NotableDateFilter filter = NotableDateFilter.WithName("christmas day");

        Assert.IsFalse(filter.Matches(Occurrence(displayName: "Christmas Day")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithName" /> throws <see cref="ArgumentNullException" /> when the name
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithName_WhenNameIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithName(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> matches an occurrence whose display name is one of the
    /// accepted values and rejects one that is not.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithAnyName_ShouldReflectMembership()
    {
        NotableDateFilter filter = NotableDateFilter.WithAnyName("Christmas Day", "Easter Sunday");

        Assert.IsTrue(filter.Matches(Occurrence(displayName: "Easter Sunday")));
        Assert.IsFalse(filter.Matches(Occurrence(displayName: "Anzac Day")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithAnyName" /> throws <see cref="ArgumentNullException" /> when the
    /// names array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithAnyName_WhenNamesIsNull_ShouldThrowExactly()
    {
        string[] names = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithAnyName(names);
        });
    }

    // -----------------------------------------------------------------------------------------------------------
    // WithId
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithId" /> matches an occurrence produced by the requested concept id
    /// and rejects one produced by a different concept.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithId_ShouldReflectConceptIdentity()
    {
        NotableDateFilter filter = NotableDateFilter.WithId("christmas-day");

        Assert.IsTrue(filter.Matches(Occurrence(notableDateId: "christmas-day")));
        Assert.IsFalse(filter.Matches(Occurrence(notableDateId: "boxing-day")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithId" /> is case-sensitive, rejecting an id that differs only in
    /// case.
    /// </summary>
    [TestMethod]
    public void Matches_WhenWithIdCaseDiffers_ShouldReturnFalse()
    {
        NotableDateFilter filter = NotableDateFilter.WithId("Christmas-Day");

        Assert.IsFalse(filter.Matches(Occurrence(notableDateId: "christmas-day")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithId" /> throws <see cref="ArgumentNullException" /> when the id is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WithId_WhenNotableDateIdIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.WithId(null!);
        });
    }

    // -----------------------------------------------------------------------------------------------------------
    // IsNonWorkingDay / WasAdjusted
    // -----------------------------------------------------------------------------------------------------------

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
        NotableDateFilter filter = NotableDateFilter.IsNonWorkingDay();

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
        NotableDateFilter filter = NotableDateFilter.WasAdjusted();

        Assert.AreEqual(isObserved, filter.Matches(Occurrence(isObserved: isObserved)));
    }

    // -----------------------------------------------------------------------------------------------------------
    // WithMinDuration
    // -----------------------------------------------------------------------------------------------------------

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
        NotableDateFilter filter = NotableDateFilter.WithMinDuration(minimumDays);

        Assert.AreEqual(expected, filter.Matches(Occurrence(durationDays: durationDays)));
    }

    // -----------------------------------------------------------------------------------------------------------
    // InDateRange
    // -----------------------------------------------------------------------------------------------------------

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
        NotableDateFilter filter = NotableDateFilter.InDateRange(new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 30));

        Assert.AreEqual(expected, filter.Matches(Occurrence(date: new DateOnly(year, month, day))));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.InDateRange" /> matches when the start and end coincide and the
    /// occurrence falls on that single day, and rejects an adjacent day.
    /// </summary>
    [TestMethod]
    public void Matches_WhenInDateRangeStartEqualsEnd_ShouldMatchOnlyThatDay()
    {
        DateOnly day = new(2024, 6, 15);
        NotableDateFilter filter = NotableDateFilter.InDateRange(day, day);

        Assert.IsTrue(filter.Matches(Occurrence(date: day)));
        Assert.IsFalse(filter.Matches(Occurrence(date: new DateOnly(2024, 6, 16))));
    }

    // -----------------------------------------------------------------------------------------------------------
    // Matches argument validation
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Matches" /> throws <see cref="ArgumentNullException" /> when the
    /// occurrence is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Matches_WhenNotableDateIsNull_ShouldThrowExactly()
    {
        NotableDateFilter filter = NotableDateFilter.IsNonWorkingDay();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.Matches(null!);
        });
    }
}
