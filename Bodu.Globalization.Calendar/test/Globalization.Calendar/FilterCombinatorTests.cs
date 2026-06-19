// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterCombinatorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the <see cref="NotableDateFilter" /> combinators (<see cref="NotableDateFilter.And" />,
/// <see cref="NotableDateFilter.Or" />, <see cref="NotableDateFilter.Not" />, <see cref="NotableDateFilter.AllOf" />,
/// and <see cref="NotableDateFilter.AnyOf" />) compose boolean logic correctly, both as direct predicates over
/// constructed occurrences and end-to-end through <see cref="INotableDateService.Resolve(DateRange, string, NotableDateFilter)" />.
/// </summary>
[TestClass]
public sealed partial class FilterCombinatorTests
{
    /// <summary>
    /// Builds a service over the filter-combinator fixture.
    /// </summary>
    /// <returns>A service for the fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("filter-combinators.xml");

    /// <summary>
    /// Resolves a calendar year of the fixture for the supplied filter and returns the matched concept ids in order.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="year">The calendar year to resolve.</param>
    /// <returns>The ordered, distinct matched concept ids.</returns>
    private static List<string> ResolveIds(NotableDateFilter filter, int year = 2026)
    {
        DateRange range = new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));

        return CreateService()
            .Resolve(range, "XX", filter)
            .Select(r => r.NotableDateId)
            .Distinct()
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Builds a resolved occurrence with the supplied category, non-working flag, and tags for direct combinator
    /// evaluation.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="isNonWorkingDay">Whether the occurrence is a non-working day.</param>
    /// <param name="tags">The tags carried by the occurrence.</param>
    /// <returns>The constructed occurrence.</returns>
    private static NotableDate Occurrence(
        NotableDateCategory category = NotableDateCategory.PublicHoliday,
        bool isNonWorkingDay = false,
        string[]? tags = null) =>
        new(
            Date: new DateOnly(2024, 1, 1),
            ActualDate: new DateOnly(2024, 1, 1),
            IsObserved: false,
            Identity: new NotableDateRuleIdentity("res", "test", "rule"),
            DisplayName: "Test",
            TerritoryCode: "XX",
            Category: category,
            Priority: 0,
            DurationDays: 1,
            IsNonWorkingDay: isNonWorkingDay,
            Tags: tags ?? [],
            AdjustmentPolicyId: null,
            AdjustmentReason: null);

    /// <summary>
    /// Asserts that the resolved ids equal the expected set, regardless of order.
    /// </summary>
    /// <param name="expected">The expected concept ids.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="year">The calendar year to resolve.</param>
    private static void AssertResolves(string[] expected, NotableDateFilter filter, int year = 2026)
    {
        string[] sorted = expected.OrderBy(s => s, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(sorted, ResolveIds(filter, year));
    }

    /// <summary>
    /// Provides directly constructed occurrences for the complex-composition matcher, each paired with its expected
    /// match result against <c>(PublicHoliday OR Observance) AND NonWorking AND tag</c>.
    /// </summary>
    /// <returns>A sequence of <c>(string name, NotableDate occurrence, bool expected)</c> rows.</returns>
    public static IEnumerable<object[]> ComplexCompositionRows()
    {
        yield return new object[] { "observance non-working tagged -> match", Occurrence(NotableDateCategory.Observance, isNonWorkingDay: true, tags: ["Public", "Religious"]), true };
        yield return new object[] { "cultural fails category gate", Occurrence(NotableDateCategory.Cultural, isNonWorkingDay: true, tags: ["Public"]), false };
        yield return new object[] { "working day fails non-working gate", Occurrence(NotableDateCategory.PublicHoliday, isNonWorkingDay: false, tags: ["Public"]), false };
    }
}
