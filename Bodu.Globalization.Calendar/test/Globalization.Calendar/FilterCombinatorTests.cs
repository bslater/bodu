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
public sealed class FilterCombinatorTests
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
        var sorted = expected.OrderBy(s => s, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(sorted, ResolveIds(filter, year));
    }

    // -----------------------------------------------------------------------------------------------------------
    // End-to-end single-factory baselines (filtered resolve)
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the unfiltered resolve returns every fixture concept, establishing the baseline.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenUnfiltered_ReturnsAllConcepts()
    {
        DateRange range = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        var ids = CreateService().Resolve(range, "XX")
            .Select(r => r.NotableDateId).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();

        CollectionAssert.AreEqual(
            new[] { "australia-day", "bank-holiday", "boxing-day", "christmas-day", "hanukkah", "labour-day", "lunar-festival", "year-end-holiday" },
            ids);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.ForCategory" /> emits only concepts of the matching category through a
    /// filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenForCategory_EmitsOnlyMatchingCategory()
    {
        AssertResolves(
            ["australia-day", "boxing-day", "christmas-day", "year-end-holiday"],
            NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WithTag" /> emits only concepts carrying the tag through a filtered
    /// resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWithTag_EmitsTaggedConcepts()
    {
        AssertResolves(
            ["australia-day", "bank-holiday", "boxing-day", "christmas-day", "year-end-holiday"],
            NotableDateFilter.WithTag("Public"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.IsNonWorkingDay" /> emits only non-working concepts through a filtered
    /// resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenIsNonWorkingDay_EmitsOnlyNonWorkingConcepts()
    {
        AssertResolves(
            ["australia-day", "bank-holiday", "boxing-day", "christmas-day", "labour-day", "year-end-holiday"],
            NotableDateFilter.IsNonWorkingDay());
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.WasAdjusted" /> emits only the occurrence whose emitted date was
    /// shifted by a weekend substitute. In 2022, 31 December is a Saturday, so Year-End Holiday is observed on Monday
    /// 2 January 2023.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWasAdjusted_EmitsOnlyShiftedOccurrences()
    {
        DateRange range = new(new DateOnly(2022, 12, 25), new DateOnly(2023, 1, 5));
        IReadOnlyList<NotableDate> resolved = CreateService().Resolve(range, "XX", NotableDateFilter.WasAdjusted());

        // The only emitted occurrence is the observed Year-End Holiday substitute on Monday 2 January 2023.
        CollectionAssert.AreEqual(
            new[] { ("year-end-holiday", new DateOnly(2023, 1, 2), true) },
            resolved.OrderBy(r => r.Date).Select(r => (r.NotableDateId, r.Date, r.IsObserved)).ToArray());
    }

    // -----------------------------------------------------------------------------------------------------------
    // And / Or — direct and end-to-end
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> of a category filter and a non-working filter matches an
    /// occurrence only when both component filters match.
    /// </summary>
    /// <param name="category">The occurrence category.</param>
    /// <param name="isNonWorkingDay">Whether the occurrence is a non-working day.</param>
    /// <param name="expected">The expected match result.</param>
    [TestMethod]
    [DataRow(NotableDateCategory.PublicHoliday, true, true)]    // both components match
    [DataRow(NotableDateCategory.PublicHoliday, false, false)]  // non-working component fails
    [DataRow(NotableDateCategory.Observance, true, false)]      // category component fails
    public void Matches_WhenAnd_ShouldRequireBothComponents(NotableDateCategory category, bool isNonWorkingDay, bool expected)
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday)
            .And(NotableDateFilter.IsNonWorkingDay());

        Assert.AreEqual(expected, filter.Matches(Occurrence(category, isNonWorkingDay: isNonWorkingDay)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> emits only concepts passing both filters through a filtered
    /// resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnd_EmitsIntersection()
    {
        AssertResolves(
            ["boxing-day", "christmas-day"],
            NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday).And(NotableDateFilter.WithTag("Christian")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> of two disjoint filters emits nothing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDisjointAnd_EmitsNothing()
    {
        AssertResolves(
            [],
            NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday).And(NotableDateFilter.WithTag("Jewish")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> throws <see cref="ArgumentNullException" /> when the sibling is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void And_WhenOtherIsNull_ShouldThrowExactly()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.And(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> of a category filter and a non-working filter matches an
    /// occurrence when either component filter matches.
    /// </summary>
    /// <param name="category">The occurrence category.</param>
    /// <param name="isNonWorkingDay">Whether the occurrence is a non-working day.</param>
    /// <param name="expected">The expected match result.</param>
    [TestMethod]
    [DataRow(NotableDateCategory.Observance, true, true)]       // non-working component matches
    [DataRow(NotableDateCategory.PublicHoliday, false, true)]   // category component matches
    [DataRow(NotableDateCategory.Observance, false, false)]     // neither component matches
    public void Matches_WhenOr_ShouldAcceptEitherComponent(NotableDateCategory category, bool isNonWorkingDay, bool expected)
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday)
            .Or(NotableDateFilter.IsNonWorkingDay());

        Assert.AreEqual(expected, filter.Matches(Occurrence(category, isNonWorkingDay: isNonWorkingDay)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> emits the union of two filters through a filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOr_EmitsUnion()
    {
        AssertResolves(
            ["hanukkah", "labour-day"],
            NotableDateFilter.ForCategory(NotableDateCategory.Religious).Or(NotableDateFilter.ForCategory(NotableDateCategory.Civic)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> throws <see cref="ArgumentNullException" /> when the sibling is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Or_WhenOtherIsNull_ShouldThrowExactly()
    {
        var filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.Or(null!);
        });
    }

    // -----------------------------------------------------------------------------------------------------------
    // Not
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Not" /> inverts the underlying category predicate for direct
    /// evaluation.
    /// </summary>
    /// <param name="category">The occurrence category.</param>
    /// <param name="expected">The expected match result after negation.</param>
    [TestMethod]
    [DataRow(NotableDateCategory.PublicHoliday, false)]  // negated category matches -> false
    [DataRow(NotableDateCategory.Religious, true)]       // negated category does not match -> true
    public void Matches_WhenNot_ShouldInvertPredicate(NotableDateCategory category, bool expected)
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday).Not();

        Assert.AreEqual(expected, filter.Matches(Occurrence(category)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Not" /> emits exactly the complement of the negated filter through a
    /// filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNot_EmitsComplement()
    {
        AssertResolves(
            ["australia-day", "bank-holiday", "boxing-day", "christmas-day", "year-end-holiday"],
            NotableDateFilter.WithTag("Public"));

        AssertResolves(
            ["hanukkah", "labour-day", "lunar-festival"],
            NotableDateFilter.WithTag("Public").Not());
    }

    // -----------------------------------------------------------------------------------------------------------
    // AllOf / AnyOf
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> emits only concepts passing every supplied filter through a
    /// filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAllOf_EmitsConjunction()
    {
        AssertResolves(
            ["australia-day", "boxing-day", "christmas-day", "year-end-holiday"],
            NotableDateFilter.AllOf(
                NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday),
                NotableDateFilter.IsNonWorkingDay(),
                NotableDateFilter.WithTag("Public")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> with no filters matches every occurrence, since the
    /// empty-set conjunction is vacuously satisfied.
    /// </summary>
    [TestMethod]
    public void Matches_WhenAllOfIsEmpty_ShouldMatchEveryOccurrence()
    {
        var filter = NotableDateFilter.AllOf();

        Assert.IsTrue(filter.Matches(Occurrence(NotableDateCategory.Cultural)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AllOf" /> throws <see cref="ArgumentNullException" /> when the filters
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AllOf_WhenFiltersIsNull_ShouldThrowExactly()
    {
        NotableDateFilter[] filters = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.AllOf(filters);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> emits the union across every supplied filter through a
    /// filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnyOf_EmitsDisjunction()
    {
        AssertResolves(
            ["hanukkah", "labour-day", "lunar-festival"],
            NotableDateFilter.AnyOf(
                NotableDateFilter.WithTag("Workers"),
                NotableDateFilter.WithTag("Jewish"),
                NotableDateFilter.WithTag("Asian")));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> with a single filter behaves identically to that filter
    /// alone.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnyOfWithSingleFilter_DelegatesToThatFilter()
    {
        AssertResolves(
            ["bank-holiday"],
            NotableDateFilter.AnyOf(NotableDateFilter.ForCategory(NotableDateCategory.BankHoliday)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> with no filters matches nothing, since the empty-set
    /// disjunction is never satisfied.
    /// </summary>
    [TestMethod]
    public void Matches_WhenAnyOfIsEmpty_ShouldMatchNothing()
    {
        var filter = NotableDateFilter.AnyOf();

        Assert.IsFalse(filter.Matches(Occurrence(NotableDateCategory.PublicHoliday)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> throws <see cref="ArgumentNullException" /> when the filters
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AnyOf_WhenFiltersIsNull_ShouldThrowExactly()
    {
        NotableDateFilter[] filters = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateFilter.AnyOf(filters);
        });
    }

    // -----------------------------------------------------------------------------------------------------------
    // Nested composition
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a nested <c>(category AND tag) OR category</c> composition emits the expected union through a
    /// filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNestedAndOr_EvaluatesCorrectly()
    {
        // (PublicHoliday AND Christian) OR Religious → Christmas + Boxing ∪ Hanukkah.
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday)
            .And(NotableDateFilter.WithTag("Christian"))
            .Or(NotableDateFilter.ForCategory(NotableDateCategory.Religious));

        AssertResolves(["boxing-day", "christmas-day", "hanukkah"], filter);
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

    /// <summary>
    /// Verifies that a complex composition <c>(PublicHoliday OR Observance) AND NonWorking AND tag</c> evaluates
    /// correctly over directly constructed occurrences.
    /// </summary>
    /// <param name="name">A human-readable label for the row.</param>
    /// <param name="occurrence">The occurrence under evaluation.</param>
    /// <param name="expected">The expected match result.</param>
    [TestMethod]
    [DynamicData(nameof(ComplexCompositionRows))]
    public void Matches_WhenComplexComposition_ShouldEvaluateCorrectly(string name, NotableDate occurrence, bool expected)
    {
        NotableDateFilter filter = NotableDateFilter.AnyOf(
                NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday),
                NotableDateFilter.ForCategory(NotableDateCategory.Observance))
            .And(NotableDateFilter.IsNonWorkingDay())
            .And(NotableDateFilter.WithTag("Public"));

        Assert.AreEqual(expected, filter.Matches(occurrence), name);
    }
}
