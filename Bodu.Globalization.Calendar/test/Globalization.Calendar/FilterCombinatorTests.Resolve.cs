// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterCombinatorTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterCombinatorTests
{
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
}
