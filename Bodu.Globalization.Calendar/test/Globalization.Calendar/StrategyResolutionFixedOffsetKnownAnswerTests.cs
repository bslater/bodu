// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionFixedOffsetKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="FixedDateStrategy" /> (Gregorian), <see cref="DayOfWeekInMonthStrategy" />, the rule
/// applicability year window, and the <see cref="OffsetFromRuleStrategy" /> (single-link, multi-link, and circular)
/// resolved through the v2 engine end to end. The cases are ported from the v1 <c>NotableDateRuleResolverTests</c> Fixed
/// / DayOfWeekInMonth / OffsetFromAnchor matrices and the applicability truth table, adapted to the v2 model where year
/// bounds are <c>fromYear</c>/<c>toYear</c>, an unresolved offset reference is a load-time validation error, and a
/// circular offset chain loads but produces no occurrence.
/// </summary>
[TestClass]
public sealed class StrategyResolutionFixedOffsetKnownAnswerTests
{
    /// <summary>
    /// The service over the fixed/offset fixture.
    /// </summary>
    private static readonly NotableDateService s_service = new(NotableDateFixtures.Load("strategy-fixed-offset.xml"));

    /// <summary>
    /// Verifies that a fixed Gregorian rule resolves to the calendar day named by its month and day.
    /// </summary>
    [TestMethod]
    public void Resolve_FixedRule_ResolvesSpecifiedMonthAndDay() =>
        AssertResolvesOn("fixed-new-year", new DateOnly(2026, 1, 1));

    /// <summary>
    /// Verifies that a fixed rule whose lower year bound exceeds the requested year produces no occurrence.
    /// </summary>
    [TestMethod]
    public void Resolve_YearBeforeFromYear_ProducesNoOccurrence() =>
        AssertNoOccurrenceInYear("fixed-from-2030", 2025);

    /// <summary>
    /// Verifies that a fixed rule whose upper year bound is below the requested year produces no occurrence.
    /// </summary>
    [TestMethod]
    public void Resolve_YearAfterToYear_ProducesNoOccurrence() =>
        AssertNoOccurrenceInYear("fixed-to-2020", 2025);

    /// <summary>
    /// Verifies the rule applicability year window: a rule bounded to 2020–2030 inclusive resolves for an in-window
    /// year, an unbounded rule always resolves, and a year below or above the window produces no occurrence. Ported from
    /// the non-cadence rows of the v1 <c>IsApplicable_TruthTable</c> (the v1 <c>OccurrenceYears</c> cadence has no v2
    /// authoring surface and is excluded).
    /// </summary>
    /// <param name="notableDateId">The concept whose applicability window is under test.</param>
    /// <param name="year">The resolution year.</param>
    /// <param name="expectedApplies">Whether the rule is expected to resolve for the year.</param>
    [TestMethod]
    [DataRow("fixed-new-year", 2025, true)]            // no bounds → always applies
    [DataRow("fixed-window-2020-2030", 2025, true)]    // in-window
    [DataRow("fixed-window-2020-2030", 2019, false)]   // below fromYear
    [DataRow("fixed-window-2020-2030", 2031, false)]   // above toYear
    public void Resolve_ApplicabilityWindow_AppliesOnlyWithinBounds(string notableDateId, int year, bool expectedApplies)
    {
        int count = s_service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Count(r => r.NotableDateId == notableDateId);

        Assert.AreEqual(expectedApplies ? 1 : 0, count, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that a DayOfWeekInMonth rule resolves to the requested ordinal weekday (the second Sunday of May 2025 is
    /// 11 May).
    /// </summary>
    [TestMethod]
    public void Resolve_DayOfWeekInMonthRule_ResolvesNthWeekday() =>
        AssertResolvesOn("dow-2nd-sun-may", new DateOnly(2025, 5, 11));

    /// <summary>
    /// Verifies that a single-link OffsetFromRule rule resolves through its referenced anchor (two days before 10 April
    /// is 8 April).
    /// </summary>
    [TestMethod]
    public void Resolve_OffsetFromRule_ResolvesThroughAnchor() =>
        AssertResolvesOn("offset-minus-2", new DateOnly(2025, 4, 8));

    /// <summary>
    /// Verifies that a multi-link OffsetFromRule chain resolves through each referenced rule in turn (anchor 10 April,
    /// minus two days, then plus five days, a net of 13 April).
    /// </summary>
    [TestMethod]
    public void Resolve_OffsetFromRuleChain_ResolvesThroughEachLink() =>
        AssertResolvesOn("offset-chain-plus-5", new DateOnly(2025, 4, 13));

    /// <summary>
    /// Verifies that a circular OffsetFromRule chain loads successfully (each reference resolves to exactly one rule) but
    /// produces no occurrence, because the resolver breaks the cycle rather than recursing without end. The v1 engine
    /// reported the cycle as an <see cref="InvalidOperationException" />; the v2 engine resolves it to no occurrence.
    /// </summary>
    /// <param name="notableDateId">The concept participating in the cycle.</param>
    [TestMethod]
    [DataRow("offset-cycle-a")]
    [DataRow("offset-cycle-b")]
    public void Resolve_OffsetFromRuleCycle_ProducesNoOccurrence(string notableDateId) =>
        AssertNoOccurrenceInYear(notableDateId, 2025);

    /// <summary>
    /// Verifies that a document whose OffsetFromRule reference cannot be resolved fails to load with a
    /// <see cref="NotableDateValidationException" />. The v1 engine reported the missing anchor at resolution time; the
    /// v2 engine reports it during validation, before the service is constructed.
    /// </summary>
    [TestMethod]
    public void Load_OffsetFromRuleReferenceMissing_ThrowsValidationException()
    {
        string xml = NotableDateFixtures.ReadText("invalid-offset-missing.xml");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(
            d => d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics,
            "expected at least one error diagnostic for the unresolved offset reference");
    }

    /// <summary>
    /// Resolves a single-day window on the expected date and asserts the concept produced exactly one occurrence there.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="date">The expected occurrence date.</param>
    private static void AssertResolvesOn(string notableDateId, DateOnly date)
    {
        var matches = s_service
            .Resolve(date, "XX")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}' on {date:yyyy-MM-dd}");
        Assert.AreEqual(date, matches[0].Date, $"{notableDateId} {date:yyyy-MM-dd}");
    }

    /// <summary>
    /// Asserts that the concept produces no occurrence anywhere in the supplied Gregorian year.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="year">The Gregorian year to scan.</param>
    private static void AssertNoOccurrenceInYear(string notableDateId, int year)
    {
        int count = s_service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Count(r => r.NotableDateId == notableDateId);

        Assert.AreEqual(0, count, $"expected no '{notableDateId}' in {year}");
    }
}
