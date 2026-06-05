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
        string[] sorted = expected.OrderBy(s => s, StringComparer.Ordinal).ToArray();

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
        List<string> ids = CreateService().Resolve(range, "XX")
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
            new[] { "australia-day", "boxing-day", "christmas-day", "year-end-holiday" },
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
            new[] { "australia-day", "bank-holiday", "boxing-day", "christmas-day", "year-end-holiday" },
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
            new[] { "australia-day", "bank-holiday", "boxing-day", "christmas-day", "labour-day", "year-end-holiday" },
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

        Assert.IsTrue(resolved.All(r => r.IsObserved));
        Assert.AreEqual(1, resolved.Count);
        NotableDate observed = resolved[0];
        Assert.AreEqual("year-end-holiday", observed.NotableDateId);
        Assert.AreEqual(new DateOnly(2023, 1, 2), observed.Date);
    }

    // -----------------------------------------------------------------------------------------------------------
    // And / Or — direct and end-to-end
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> matches an occurrence only when both component filters match.
    /// </summary>
    [TestMethod]
    public void Matches_WhenAnd_RequiresBothComponents()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday)
            .And(NotableDateFilter.IsNonWorkingDay());

        Assert.IsTrue(filter.Matches(Occurrence(NotableDateCategory.PublicHoliday, isNonWorkingDay: true)));
        Assert.IsFalse(filter.Matches(Occurrence(NotableDateCategory.PublicHoliday, isNonWorkingDay: false)));
        Assert.IsFalse(filter.Matches(Occurrence(NotableDateCategory.Observance, isNonWorkingDay: true)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.And" /> emits only concepts passing both filters through a filtered
    /// resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnd_EmitsIntersection()
    {
        AssertResolves(
            new[] { "boxing-day", "christmas-day" },
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
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.And(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> matches an occurrence when either component filter matches.
    /// </summary>
    [TestMethod]
    public void Matches_WhenOr_AcceptsEitherComponent()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday)
            .Or(NotableDateFilter.IsNonWorkingDay());

        Assert.IsTrue(filter.Matches(Occurrence(NotableDateCategory.Observance, isNonWorkingDay: true)));
        Assert.IsTrue(filter.Matches(Occurrence(NotableDateCategory.PublicHoliday, isNonWorkingDay: false)));
        Assert.IsFalse(filter.Matches(Occurrence(NotableDateCategory.Observance, isNonWorkingDay: false)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> emits the union of two filters through a filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOr_EmitsUnion()
    {
        AssertResolves(
            new[] { "hanukkah", "labour-day" },
            NotableDateFilter.ForCategory(NotableDateCategory.Religious).Or(NotableDateFilter.ForCategory(NotableDateCategory.Civic)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Or" /> throws <see cref="ArgumentNullException" /> when the sibling is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Or_WhenOtherIsNull_ShouldThrowExactly()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = filter.Or(null!);
        });
    }

    // -----------------------------------------------------------------------------------------------------------
    // Not
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Not" /> inverts the underlying predicate for direct evaluation.
    /// </summary>
    [TestMethod]
    public void Matches_WhenNot_InvertsPredicate()
    {
        NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday).Not();

        Assert.IsFalse(filter.Matches(Occurrence(NotableDateCategory.PublicHoliday)));
        Assert.IsTrue(filter.Matches(Occurrence(NotableDateCategory.Religious)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.Not" /> emits exactly the complement of the negated filter through a
    /// filtered resolve.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNot_EmitsComplement()
    {
        AssertResolves(
            new[] { "australia-day", "bank-holiday", "boxing-day", "christmas-day", "year-end-holiday" },
            NotableDateFilter.WithTag("Public"));

        AssertResolves(
            new[] { "hanukkah", "labour-day", "lunar-festival" },
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
            new[] { "australia-day", "boxing-day", "christmas-day", "year-end-holiday" },
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
        NotableDateFilter filter = NotableDateFilter.AllOf();

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
            new[] { "hanukkah", "labour-day", "lunar-festival" },
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
            new[] { "bank-holiday" },
            NotableDateFilter.AnyOf(NotableDateFilter.ForCategory(NotableDateCategory.BankHoliday)));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFilter.AnyOf" /> with no filters matches nothing, since the empty-set
    /// disjunction is never satisfied.
    /// </summary>
    [TestMethod]
    public void Matches_WhenAnyOfIsEmpty_ShouldMatchNothing()
    {
        NotableDateFilter filter = NotableDateFilter.AnyOf();

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

        AssertResolves(new[] { "boxing-day", "christmas-day", "hanukkah" }, filter);
    }

    /// <summary>
    /// Verifies that a complex composition <c>(PublicHoliday OR Observance) AND NonWorking AND tag</c> evaluates
    /// correctly over directly constructed occurrences.
    /// </summary>
    [TestMethod]
    public void Matches_WhenComplexComposition_EvaluatesCorrectly()
    {
        NotableDateFilter filter = NotableDateFilter.AnyOf(
                NotableDateFilter.ForCategory(NotableDateCategory.PublicHoliday),
                NotableDateFilter.ForCategory(NotableDateCategory.Observance))
            .And(NotableDateFilter.IsNonWorkingDay())
            .And(NotableDateFilter.WithTag("Public"));

        Assert.IsTrue(filter.Matches(Occurrence(NotableDateCategory.Observance, isNonWorkingDay: true, tags: ["Public", "Religious"])));
        Assert.IsFalse(filter.Matches(Occurrence(NotableDateCategory.Cultural, isNonWorkingDay: true, tags: ["Public"])));
        Assert.IsFalse(filter.Matches(Occurrence(NotableDateCategory.PublicHoliday, isNonWorkingDay: false, tags: ["Public"])));
    }
}
