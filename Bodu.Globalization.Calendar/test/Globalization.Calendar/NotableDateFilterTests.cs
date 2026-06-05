// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFilterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateFilter" /> restricts resolved occurrences by category, tag, name, duration, and
/// observed state, and that the filters compose with <c>And</c>, <c>Or</c>, <c>AllOf</c>, and <c>AnyOf</c>.
/// </summary>
[TestClass]
public sealed class NotableDateFilterTests
{
    /// <summary>
    /// Builds a service over the filter fixture.
    /// </summary>
    /// <returns>A service for the filter fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("filter.xml");

    /// <summary>
    /// Resolves the 2023 calendar year for the filter fixture, optionally restricted by a filter.
    /// </summary>
    /// <param name="filter">The filter to apply, or <see langword="null" /> for none.</param>
    /// <returns>The resolved notable-date identifiers.</returns>
    private static List<string> Resolve(NotableDateFilter? filter)
    {
        DateRange year = new(new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31));
        NotableDateService service = CreateService();

        IReadOnlyList<NotableDate> results = filter is null
            ? service.Resolve(year, "XX")
            : service.Resolve(year, "XX", filter);

        return results.Select(r => r.NotableDateId).OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Verifies that the unfiltered resolve returns every concept, establishing the fixture baseline.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNoFilter_ReturnsAllConcepts()
    {
        CollectionAssert.AreEqual(new[] { "culture-fest", "new-year", "spring-week" }, Resolve(null));
    }

    /// <summary>
    /// Verifies that the category filter keeps only occurrences of the requested category.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenForCategory_KeepsOnlyThatCategory()
    {
        CollectionAssert.AreEqual(new[] { "culture-fest" }, Resolve(NotableDateFilter.ForCategory(NotableDateCategory.Cultural)));
    }

    /// <summary>
    /// Verifies that the non-working filter drops working observances.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenIsNonWorkingDay_DropsWorkingObservances()
    {
        CollectionAssert.AreEqual(new[] { "new-year", "spring-week" }, Resolve(NotableDateFilter.IsNonWorkingDay()));
    }

    /// <summary>
    /// Verifies that the tag filter keeps only occurrences carrying the tag.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWithTag_KeepsOnlyTaggedOccurrences()
    {
        CollectionAssert.AreEqual(new[] { "new-year", "spring-week" }, Resolve(NotableDateFilter.WithTag("major")));
    }

    /// <summary>
    /// Verifies that the minimum-duration filter keeps only multi-day occurrences.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWithMinDuration_KeepsOnlyLongerSpans()
    {
        CollectionAssert.AreEqual(new[] { "spring-week" }, Resolve(NotableDateFilter.WithMinDuration(2)));
    }

    /// <summary>
    /// Verifies that the observed filter keeps only occurrences whose emitted date was adjusted. In 2023, 1 January
    /// falls on a Sunday and is observed on the following Monday.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWasAdjusted_KeepsOnlyObservedOccurrences()
    {
        CollectionAssert.AreEqual(new[] { "new-year" }, Resolve(NotableDateFilter.WasAdjusted()));
    }

    /// <summary>
    /// Verifies that conjunction keeps only occurrences matching every filter, and disjunction keeps occurrences
    /// matching either.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenComposedWithAndOr_AppliesBooleanLogic()
    {
        NotableDateFilter majorPublicHoliday = NotableDateFilter.WithTag("major")
            .And(NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));
        CollectionAssert.AreEqual(new[] { "new-year", "spring-week" }, Resolve(majorPublicHoliday));

        NotableDateFilter culturalOrLong = NotableDateFilter.AnyOf(
            NotableDateFilter.ForCategory(NotableDateCategory.Cultural),
            NotableDateFilter.WithMinDuration(2));
        CollectionAssert.AreEqual(new[] { "culture-fest", "spring-week" }, Resolve(culturalOrLong));
    }

    /// <summary>
    /// Verifies that the single-day filtered overload returns the matching occurrence for a day inside a multi-day span.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleDayWithFilter_AppliesTheFilter()
    {
        IReadOnlyList<NotableDate> results = CreateService()
            .Resolve(new DateOnly(2023, 2, 3), "XX", NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("spring-week", results[0].NotableDateId);
    }
}
