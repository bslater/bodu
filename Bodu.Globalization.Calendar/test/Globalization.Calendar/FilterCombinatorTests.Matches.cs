// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterCombinatorTests.Matches.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class FilterCombinatorTests
{
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
