// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolveScenarioTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Ports the user-observable range-resolution scenarios from the v1 pipeline scenario suite to the v2
/// <see cref="NotableDateService.Resolve(DateRange, string)" /> surface: window inclusion by emitted date, the
/// algorithmic-anchor Easter cycle, offset-from-fixed pairing, multi-day span intersection, consecutive-holiday
/// window slicing, cross-year distinct-substitute allocation, and the observed-only suppression contract.
/// </summary>
/// <remarks>
/// <para>
/// Day-of-week pairings used in the assertions are real Gregorian calendar dates so reviewers can verify them with any
/// almanac. Each scenario authors the smallest resource that exercises the behaviour under examination, either via a
/// dedicated embedded fixture or an inline document.
/// </para>
/// </remarks>
[TestClass]
public sealed class ResolveScenarioTests
{
    /// <summary>The territory used by the global-rule fixtures, which apply to every territory.</summary>
    private const string Territory = "AU";

    // =====================================================================================================================
    // Fixed-date emission and window boundaries
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a fixed-date rule whose anchor lies inside the requested window is emitted on its actual date with no
    /// adjustment.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFixedDateAnchorIsInsideWindow_ShouldEmitActualDate()
    {
        IReadOnlyList<NotableDate> results = ChristmasService()
            .Resolve(new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)), Territory);

        NotableDate match = Single(results, "christmas-day");
        Assert.AreEqual(new DateOnly(2026, 12, 25), match.Date);
        Assert.IsFalse(match.IsObserved);
    }

    /// <summary>
    /// Verifies that a fixed-date rule whose anchor lies outside the requested window is not emitted.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFixedDateAnchorIsOutsideWindow_ShouldNotEmit()
    {
        IReadOnlyList<NotableDate> results = ChristmasService()
            .Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), Territory);

        Assert.IsFalse(results.Any(n => n.NotableDateId == "christmas-day"));
    }

    /// <summary>
    /// Verifies that a fixed-date anchor exactly on the inclusive window start is emitted.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFixedDateAnchorIsExactlyTheWindowStart_ShouldEmit()
    {
        IReadOnlyList<NotableDate> results = ChristmasService()
            .Resolve(new DateRange(new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 31)), Territory);

        Assert.AreEqual(new DateOnly(2026, 12, 25), Single(results, "christmas-day").Date);
    }

    /// <summary>
    /// Verifies that a fixed-date anchor exactly on the inclusive window end is emitted.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenFixedDateAnchorIsExactlyTheWindowEnd_ShouldEmit()
    {
        IReadOnlyList<NotableDate> results = ChristmasService()
            .Resolve(new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 25)), Territory);

        Assert.AreEqual(new DateOnly(2026, 12, 25), Single(results, "christmas-day").Date);
    }

    /// <summary>
    /// Verifies that a single-day window aligned exactly on a fixed-date holiday emits that holiday and nothing else.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Resolve_WhenSingleDayWindowOnHolidayDate_ShouldEmitJustThatHoliday()
    {
        IReadOnlyList<NotableDate> results = ChristmasService().Resolve(new DateOnly(2026, 12, 25), Territory);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("christmas-day", results[0].NotableDateId);
        Assert.AreEqual(new DateOnly(2026, 12, 25), results[0].Date);
    }

    /// <summary>
    /// Verifies that a single-day window on a date that is not the anchor of any rule emits nothing, even when an adjacent
    /// fixed-date holiday lives in the same week. 26 Dec 2026 is a Saturday with no Boxing Day rule in the fixture.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleDayWindowOnNonHolidayDate_ShouldEmitNothing()
    {
        IReadOnlyList<NotableDate> results = ChristmasService().Resolve(new DateOnly(2026, 12, 26), Territory);

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that a reversed range (start later than end) throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRangeIsReversed_ShouldThrowArgumentOutOfRangeException()
    {
        NotableDateService service = ChristmasService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = service.Resolve(new DateRange(new DateOnly(2026, 12, 31), new DateOnly(2026, 12, 1)), Territory);
        });
    }

    /// <summary>
    /// Verifies that a window spanning three calendar years emits an annual fixed-date rule once per year in
    /// chronological order.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowSpansThreeCalendarYears_ShouldEmitAnnualRuleForEachYear()
    {
        IReadOnlyList<NotableDate> results = ChristmasService()
            .Resolve(new DateRange(new DateOnly(2024, 12, 1), new DateOnly(2026, 12, 31)), Territory);

        DateOnly[] christmases = results
            .Where(n => n.NotableDateId == "christmas-day")
            .OrderBy(n => n.Date)
            .Select(n => n.Date)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateOnly(2024, 12, 25),
                new DateOnly(2025, 12, 25),
                new DateOnly(2026, 12, 25),
            },
            christmases);
    }

    // =====================================================================================================================
    // Algorithmic anchor + offset-derived cycle
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a window containing the entire 2026 Easter cycle emits the algorithmic anchor and all four
    /// offset-derived occurrences at their correct projected dates.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowContainsEntireEasterCycle_ShouldEmitAnchorAndAllDerivatives()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-easter.xml")
            .Resolve(new DateRange(new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 30)), "US");

        Assert.AreEqual(new DateOnly(2026, 2, 18), Single(results, "start-of-lent").Date);
        Assert.AreEqual(new DateOnly(2026, 3, 29), Single(results, "palm-sunday").Date);
        Assert.AreEqual(new DateOnly(2026, 4, 3), Single(results, "good-friday").Date);
        Assert.AreEqual(new DateOnly(2026, 4, 5), Single(results, "easter-sunday").Date);
        Assert.AreEqual(new DateOnly(2026, 4, 6), Single(results, "easter-monday").Date);
    }

    /// <summary>
    /// Verifies that a window intersecting only one Easter derivative (Palm Sunday, 29 Mar 2026) emits only that
    /// derivative, even though the anchor and the other derivatives are computed internally for the same year.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowOnlyContainsPalmSunday_ShouldEmitPalmSundayOnly()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-easter.xml")
            .Resolve(new DateRange(new DateOnly(2026, 3, 25), new DateOnly(2026, 3, 31)), "US");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("palm-sunday", results[0].NotableDateId);
        Assert.AreEqual(new DateOnly(2026, 3, 29), results[0].Date);
    }

    // =====================================================================================================================
    // Offset-from-fixed
    // =====================================================================================================================

    /// <summary>
    /// Verifies that an offset-from-fixed rule (Boxing Day = Christmas Day + 1) is emitted alongside its anchor when both
    /// fall in the requested window.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOffsetFromFixedAndAnchorAreBothInWindow_ShouldEmitBoth()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.offset-fixed">
          <NotableDates>
            <NotableDate id="christmas-day" displayName="Christmas Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="dec-25"><Applicability calendar="Gregorian" /><Strategy><Fixed month="December" day="25" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="boxing-day" displayName="Boxing Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="xmas-plus-1"><Applicability calendar="Gregorian" />
                <Strategy><OffsetFromRule notableDateRef="christmas-day" ruleRef="dec-25" offsetDays="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 27)), Territory);

        Assert.AreEqual(new DateOnly(2026, 12, 25), Single(results, "christmas-day").Date);
        Assert.AreEqual(new DateOnly(2026, 12, 26), Single(results, "boxing-day").Date);
    }

    // =====================================================================================================================
    // Multi-day spans
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a multi-day span entirely contained within the request is emitted once with the correct duration and
    /// end date.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiDaySpanFullyInsideWindow_ShouldEmitWithSpanIntact()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-spans.xml")
            .Resolve(new DateRange(new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 10)), Territory);

        NotableDate festival = Single(results, "year-end-festival");
        Assert.AreEqual(new DateOnly(2026, 12, 30), festival.Date);
        Assert.AreEqual(7, festival.DurationDays);
        Assert.AreEqual(new DateOnly(2027, 1, 5), festival.EndDate);
    }

    /// <summary>
    /// Verifies that a multi-day span whose anchor lies before the window but whose tail wraps the year boundary into it
    /// is emitted from its anchor year with the duration intact.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiDaySpanStartsBeforeWindowAndWrapsYearBoundaryIntoIt_ShouldEmitFromAnchorYear()
    {
        // 7-day festival starting 30 Dec 2025 ends 5 Jan 2026. A January 2026 window catches only the wrapped tail.
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-spans.xml")
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)), Territory);

        NotableDate festival = Single(results, "year-end-festival");
        Assert.AreEqual(new DateOnly(2025, 12, 30), festival.Date);
        Assert.AreEqual(new DateOnly(2026, 1, 5), festival.EndDate);
    }

    /// <summary>
    /// Verifies that a multi-day span whose anchor is exactly the window start is emitted with full duration metadata.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiDaySpanAnchorIsExactlyTheWindowStart_ShouldEmit()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-spans.xml")
            .Resolve(new DateOnly(2026, 12, 30), Territory);

        NotableDate festival = Single(results, "year-end-festival");
        Assert.AreEqual(new DateOnly(2026, 12, 30), festival.Date);
        Assert.AreEqual(7, festival.DurationDays);
    }

    /// <summary>
    /// Verifies that a multi-day span whose end day just touches the window start is emitted from its adjacent-year
    /// anchor. The 2025 festival ends 5 Jan 2026; the window opens on 5 Jan 2026.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiDaySpanEndsExactlyOnWindowStart_ShouldEmitFromAdjacentYear()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-spans.xml")
            .Resolve(new DateRange(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 31)), Territory);

        NotableDate festival = Single(results, "year-end-festival");
        Assert.AreEqual(new DateOnly(2025, 12, 30), festival.Date);
        Assert.AreEqual(new DateOnly(2026, 1, 5), festival.EndDate);
    }

    /// <summary>
    /// Verifies that a window fully enclosed inside a multi-day span emits the span exactly once, not duplicated per
    /// overlapping day. The window 2 Jan .. 4 Jan 2026 sits entirely inside the 2025 festival's span.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowIsFullyInsideMultiDaySpan_ShouldEmitSpanOnce()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-spans.xml")
            .Resolve(new DateRange(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 4)), Territory);

        NotableDate[] festivals = results.Where(n => n.NotableDateId == "year-end-festival").ToArray();
        Assert.AreEqual(1, festivals.Length);
        Assert.AreEqual(new DateOnly(2025, 12, 30), festivals[0].Date);
        Assert.AreEqual(7, festivals[0].DurationDays);
    }

    /// <summary>
    /// Verifies that a 365-day span anchored on 1 January is emitted from a mid-year single-day query because its span
    /// blankets the entire year.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDurationSpansEntireYearAndQueryIsMidYear_ShouldEmitFromAnchor()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-spans.xml")
            .Resolve(new DateRange(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)), Territory);

        NotableDate programme = Single(results, "year-long-programme");
        Assert.AreEqual(new DateOnly(2026, 1, 1), programme.Date);
        Assert.AreEqual(365, programme.DurationDays);
        Assert.AreEqual(new DateOnly(2026, 12, 31), programme.EndDate);
    }

    // =====================================================================================================================
    // N-consecutive-holiday window slicing
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a window starting midway through a run of seven consecutive fixed-date holidays emits only the
    /// trailing subset whose anchors fall inside the window.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowStartsMidway_ShouldOnlyEmitTrailingSubset()
    {
        // Window 28 Dec 2026 .. 5 Jan 2027 catches the trailing four December festival days.
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-consecutive.xml")
            .Resolve(new DateRange(new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 5)), Territory);

        CollectionAssert.AreEqual(
            new[] { "festival-day-4", "festival-day-5", "festival-day-6", "festival-day-7" },
            FestivalIds(results));
    }

    /// <summary>
    /// Verifies that a window ending midway through a run of seven consecutive fixed-date holidays emits only the leading
    /// subset whose anchors fall inside the window.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowEndsMidway_ShouldOnlyEmitLeadingSubset()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-consecutive.xml")
            .Resolve(new DateRange(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 28)), Territory);

        CollectionAssert.AreEqual(
            new[] { "festival-day-1", "festival-day-2", "festival-day-3", "festival-day-4" },
            FestivalIds(results));
    }

    /// <summary>
    /// Verifies that a window entirely contained within the run emits only the middle subset whose anchors fall inside it.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowFullyInside_ShouldOnlyEmitMiddleSubset()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-consecutive.xml")
            .Resolve(new DateRange(new DateOnly(2026, 12, 27), new DateOnly(2026, 12, 29)), Territory);

        CollectionAssert.AreEqual(
            new[] { "festival-day-3", "festival-day-4", "festival-day-5" },
            FestivalIds(results));
    }

    /// <summary>
    /// Verifies that a window fully enclosing the run emits all seven consecutive holidays exactly once in chronological
    /// order.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowFullyEncloses_ShouldEmitAllSevenInOrder()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-consecutive.xml")
            .Resolve(new DateRange(new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31)), Territory);

        NotableDate[] festivals = results
            .Where(n => n.NotableDateId.StartsWith("festival-day-", StringComparison.Ordinal))
            .OrderBy(n => n.Date)
            .ToArray();

        Assert.AreEqual(7, festivals.Length);
        for (int i = 0; i < 7; i++)
        {
            Assert.AreEqual($"festival-day-{i + 1}", festivals[i].NotableDateId);
            Assert.AreEqual(new DateOnly(2026, 12, 25 + i), festivals[i].Date);
        }
    }

    /// <summary>
    /// Verifies that a window completely disjoint from the run emits none of its holidays.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowDisjoint_ShouldEmitNone()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("resolve-consecutive.xml")
            .Resolve(new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), Territory);

        Assert.IsFalse(results.Any(n => n.NotableDateId.StartsWith("festival-day-", StringComparison.Ordinal)));
    }

    // =====================================================================================================================
    // Cross-year distinct-substitute allocation
    // =====================================================================================================================

    /// <summary>
    /// Verifies that two adjacent weekend holidays whose substitutes would otherwise collide are allocated distinct
    /// observed days: Christmas Saturday rolls to Monday and Boxing Day Sunday advances past it to Tuesday. (2021)
    /// </summary>
    /// <remarks>
    /// Overlaps the conflict-avoidance contract in <see cref="AdjacentHolidayTests" />; retained here as the v1 scenario
    /// port asserting the distinct Monday/Tuesday allocation directly.
    /// </remarks>
    [TestMethod]
    public void Resolve_WhenChristmasIsSaturdayAndBoxingDayIsSunday_ShouldAllocateMondayAndTuesdaySubstitutes()
    {
        // 25 Dec 2021 = Saturday, 26 Dec 2021 = Sunday. Christmas rolls Sun -> Mon 27 Dec; Boxing Day's substitute skips
        // the claimed Monday and lands on Tue 28 Dec.
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("adjacent-holidays.xml")
            .Resolve(new DateRange(new DateOnly(2021, 12, 25), new DateOnly(2021, 12, 31)), "AU");

        NotableDate christmas = Single(results, "christmas-day");
        Assert.AreEqual(new DateOnly(2021, 12, 27), christmas.Date);
        Assert.AreEqual(new DateOnly(2021, 12, 25), christmas.ActualDate);
        Assert.IsTrue(christmas.IsObserved);

        NotableDate boxing = Single(results, "boxing-day");
        Assert.AreEqual(new DateOnly(2021, 12, 28), boxing.Date);
        Assert.AreEqual(new DateOnly(2021, 12, 26), boxing.ActualDate);
        Assert.IsTrue(boxing.IsObserved);
    }

    // =====================================================================================================================
    // Observed-only suppression and window width independence
    // =====================================================================================================================

    /// <summary>
    /// Verifies that under the default observed-only emission, when the observed substitute lies outside the window the
    /// suppressed actual date is not reported either, keeping a day's result independent of the query-window width.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenObservedSubstituteOutsideWindow_ShouldSuppressActual()
    {
        // 25 Dec 2021 = Saturday -> observed Mon 27 Dec. The window 24 Dec .. 26 Dec covers the actual Saturday but not
        // the observed Monday, so Christmas is not reported.
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("adjacent-holidays.xml")
            .Resolve(new DateRange(new DateOnly(2021, 12, 24), new DateOnly(2021, 12, 26)), "AU");

        Assert.IsFalse(results.Any(n => n.NotableDateId == "christmas-day"));
    }

    /// <summary>
    /// Verifies that an observed substitute landing exactly on the inclusive window end is emitted. 25 Dec 2021 = Saturday
    /// rolls to Mon 27 Dec; a window ending on 27 Dec admits the substitute.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenObservedSubstituteIsExactlyTheWindowEnd_ShouldEmitObservedDate()
    {
        IReadOnlyList<NotableDate> results = NotableDateFixtures.Resolver("adjacent-holidays.xml")
            .Resolve(new DateRange(new DateOnly(2021, 12, 24), new DateOnly(2021, 12, 27)), "AU");

        NotableDate christmas = Single(results, "christmas-day");
        Assert.AreEqual(new DateOnly(2021, 12, 27), christmas.Date);
        Assert.IsTrue(christmas.IsObserved);
    }

    // =====================================================================================================================
    // Helpers
    // =====================================================================================================================

    /// <summary>
    /// Builds a resolver over an inline single-concept document holding a global, non-adjusted Christmas Day rule.
    /// </summary>
    /// <returns>A resolver for the inline Christmas Day document.</returns>
    private static NotableDateService ChristmasService()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.christmas">
          <NotableDates>
            <NotableDate id="christmas-day" displayName="Christmas Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="dec-25"><Applicability calendar="Gregorian" /><Strategy><Fixed month="December" day="25" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Projects the festival-day occurrences in chronological order to their notable-date ids.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <returns>The ordered festival-day ids.</returns>
    private static string[] FestivalIds(IReadOnlyList<NotableDate> results) =>
        results
            .Where(n => n.NotableDateId.StartsWith("festival-day-", StringComparison.Ordinal))
            .OrderBy(n => n.Date)
            .Select(n => n.NotableDateId)
            .ToArray();

    /// <summary>
    /// Returns the single resolved occurrence with the supplied notable-date id, asserting exactly one match.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(IReadOnlyList<NotableDate> results, string notableDateId)
    {
        List<NotableDate> matches = results.Where(r => r.NotableDateId == notableDateId).ToList();
        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}'");
        return matches[0];
    }
}
