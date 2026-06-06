// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolveBoundaryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Ports the v1 boundary scenarios to the v2 <see cref="NotableDateService.Resolve(DateRange, string)" /> surface:
/// civil-year extremes (year 1 and year 9999), request bounds at <see cref="DateOnly.MinValue" /> /
/// <see cref="DateOnly.MaxValue" /> (the ±1-year fringe scan must not overflow), single-day and many-year windows,
/// long-duration spans reaching the last supported day, and offset projections that stay inside the supported range.
/// </summary>
/// <remarks>
/// <para>
/// The v2 service scans one civil year either side of the requested window and clamps that scan to the
/// <c>[1, 9999]</c> civil-year range, so fringe arithmetic at the extremes is year-based and cannot throw. Offset
/// projections that would step a <see cref="DateOnly" /> past <see cref="DateOnly.MaxValue" /> are not exercised here:
/// v2 surfaces that as an <see cref="ArgumentOutOfRangeException" /> rather than the v1 pipeline's silent skip, so those
/// v1 rows are intentionally not ported.
/// </para>
/// </remarks>
[TestClass]
public sealed class ResolveBoundaryTests
{
    /// <summary>The territory used by the global-rule documents, which apply to every territory.</summary>
    private const string Territory = "AU";

    // =====================================================================================================================
    // Civil-year boundaries
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a fixed-date rule constrained to civil year 1 emits its anchor when the window spans year 1.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowIsInYearOne_ShouldEmitYearOneAnchor()
    {
        IReadOnlyList<NotableDate> results = FixedService(6, 15, fromYear: 1, toYear: 1)
            .Resolve(new DateRange(new DateOnly(1, 1, 1), new DateOnly(1, 12, 31)), Territory);

        Assert.AreEqual(new DateOnly(1, 6, 15), Single(results).Date);
    }

    /// <summary>
    /// Verifies that a request whose start is exactly <see cref="DateOnly.MinValue" /> does not throw on the
    /// fringe-year scan and still emits a year-1 anchor.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestStartIsExactlyDateOnlyMinValue_ShouldNotThrowOnFringeArithmetic()
    {
        IReadOnlyList<NotableDate> results = FixedService(1, 1, fromYear: 1, toYear: 1)
            .Resolve(new DateRange(DateOnly.MinValue, new DateOnly(1, 12, 31)), Territory);

        Assert.AreEqual(new DateOnly(1, 1, 1), Single(results).Date);
    }

    /// <summary>
    /// Verifies that a fixed-date rule constrained to civil year 9999 emits its anchor when the window spans year 9999.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowIsInYear9999_ShouldEmitYear9999Anchor()
    {
        IReadOnlyList<NotableDate> results = FixedService(6, 15, fromYear: 9999, toYear: 9999)
            .Resolve(new DateRange(new DateOnly(9999, 1, 1), new DateOnly(9999, 12, 31)), Territory);

        Assert.AreEqual(new DateOnly(9999, 6, 15), Single(results).Date);
    }

    /// <summary>
    /// Verifies that a request whose end is exactly <see cref="DateOnly.MaxValue" /> does not throw on the fringe-year
    /// scan and still emits a year-9999 anchor.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRequestEndIsExactlyDateOnlyMaxValue_ShouldNotThrowOnFringeArithmetic()
    {
        IReadOnlyList<NotableDate> results = FixedService(12, 31, fromYear: 9999, toYear: 9999)
            .Resolve(new DateRange(new DateOnly(9999, 1, 1), DateOnly.MaxValue), Territory);

        Assert.AreEqual(new DateOnly(9999, 12, 31), Single(results).Date);
    }

    // =====================================================================================================================
    // Window-shape extremes
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a request whose start equals its end is treated as a one-day window, not an empty interval.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowStartEqualsEnd_ShouldQuerySingleDay()
    {
        IReadOnlyList<NotableDate> results = FixedService(7, 4, fromYear: 2026, toYear: 2026)
            .Resolve(new DateRange(new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 4)), Territory);

        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 7, 4) },
            results.Select(r => r.Date).ToArray());
    }

    /// <summary>
    /// Verifies that a request whose rule set is empty returns an empty result without throwing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRuleSetIsEmpty_ShouldReturnEmpty()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.empty">
          <NotableDates />
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), Territory);

        Assert.IsEmpty(results);
    }

    /// <summary>
    /// Verifies that a request spanning many civil years emits an annual rule once per year in chronological order.
    /// </summary>
    /// <param name="startYear">The first civil year of the window.</param>
    /// <param name="endYear">The last civil year of the window.</param>
    /// <param name="expectedCount">The expected number of annual emissions.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2000, 2009, 10)]
    [DataRow(1990, 2030, 41)]
    [DataRow(2020, 2120, 101)]
    public void Resolve_WhenWindowSpansManyYears_ShouldEmitAnnualRuleOncePerYear(int startYear, int endYear, int expectedCount)
    {
        // A global, unbounded annual rule on 1 January.
        IReadOnlyList<NotableDate> results = FixedService(1, 1, fromYear: null, toYear: null)
            .Resolve(new DateRange(new DateOnly(startYear, 1, 1), new DateOnly(endYear, 12, 31)), Territory);

        NotableDate[] annual = results.Where(n => n.NotableDateId == "anchor").OrderBy(n => n.Date).ToArray();

        Assert.HasCount(expectedCount, annual);
        for (var i = 0; i < annual.Length; i++)
            Assert.AreEqual(new DateOnly(startYear + i, 1, 1), annual[i].Date);
    }

    // =====================================================================================================================
    // Long-duration spans at the upper boundary
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a multi-day span whose inclusive end lands exactly on <see cref="DateOnly.MaxValue" /> is emitted
    /// with the full duration. Anchor 25 Dec 9999, duration 7 -> span ends 31 Dec 9999.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSpanEndIsExactlyDateOnlyMaxValue_ShouldEmitWithFullDuration()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.year-end-span">
          <NotableDates>
            <NotableDate id="year-end-span" displayName="Year-End Span" category="Cultural" defaultNonWorkingDay="false">
              <Rules><Rule id="dec-25" durationDays="7"><Applicability calendar="Gregorian" fromYear="9999" toYear="9999" />
                <Strategy><Fixed month="December" day="25" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(9999, 12, 1), DateOnly.MaxValue), Territory);

        NotableDate span = Single(results);
        Assert.AreEqual(
            (new DateOnly(9999, 12, 25), new DateOnly(9999, 12, 31)),
            (span.Date, span.EndDate));
    }

    // =====================================================================================================================
    // Offset projection within the supported range
    // =====================================================================================================================

    /// <summary>
    /// Verifies that an offset rule whose projection stays inside the supported range emits at the projected date,
    /// including offsets that cross multiple year boundaries.
    /// </summary>
    /// <param name="offsetDays">The signed day offset applied to the 1 July 2026 anchor.</param>
    /// <param name="expectedYear">The expected emission year.</param>
    /// <param name="expectedMonth">The expected emission month.</param>
    /// <param name="expectedDay">The expected emission day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1, 2026, 7, 2)]        // +1 day
    [DataRow(365, 2027, 7, 1)]      // +1 year (no leap day in [Jul 1 2026, Jul 1 2027))
    [DataRow(730, 2028, 6, 30)]     // +2 years - 1 day: 29 Feb 2028 falls in the second year
    [DataRow(3650, 2036, 6, 28)]    // +10 years - 3 days: leap days 2028, 2032, 2036 add 3 days
    [DataRow(-1, 2026, 6, 30)]      // -1 day
    [DataRow(-365, 2025, 7, 1)]     // -1 year (no leap day in [Jul 1 2025, Jul 1 2026))
    [DataRow(-3650, 2016, 7, 3)]    // -10 years + 2 days: leap days 2020, 2024 add 2 days
    public void Resolve_WhenOffsetRuleProjectionStaysInRange_ShouldEmitAtProjectedDate(
        int offsetDays, int expectedYear, int expectedMonth, int expectedDay)
    {
        // The anchor and the offset are both pinned to 2026 (the offset resolves the anchor for its own year), so the
        // offset projects 2026-07-01 + offsetDays. Both rules are global.
        var xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.offset-range">
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="jul-1"><Applicability calendar="Gregorian" fromYear="2026" toYear="2026" />
                <Strategy><Fixed month="July" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="offset" displayName="Offset" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="projected"><Applicability calendar="Gregorian" fromYear="2026" toYear="2026" />
                <Strategy><OffsetFromRule notableDateRef="anchor" ruleRef="jul-1" offsetDays="{offsetDays}" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        var windowStartYear = Math.Min(2026, expectedYear);
        var windowEndYear = Math.Max(2026, expectedYear);

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(windowStartYear, 1, 1), new DateOnly(windowEndYear, 12, 31)), Territory);

        Assert.AreEqual(new DateOnly(expectedYear, expectedMonth, expectedDay), Single(results, "offset").Date);
    }

    /// <summary>
    /// Verifies that an offset rule whose projection lands exactly on <see cref="DateOnly.MaxValue" /> emits — the
    /// boundary value itself is supported. Anchor 25 Dec 9999 + 6 days = 31 Dec 9999.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOffsetRuleProjectionLandsExactlyOnMaxValue_ShouldEmit()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.offset-max">
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="dec-25"><Applicability calendar="Gregorian" fromYear="9999" toYear="9999" />
                <Strategy><Fixed month="December" day="25" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="offset" displayName="Offset" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="plus-6"><Applicability calendar="Gregorian" fromYear="9999" toYear="9999" />
                <Strategy><OffsetFromRule notableDateRef="anchor" ruleRef="dec-25" offsetDays="6" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        IReadOnlyList<NotableDate> results = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(9999, 12, 1), DateOnly.MaxValue), Territory);

        Assert.AreEqual(new DateOnly(9999, 12, 31), Single(results, "offset").Date);
    }

    // =====================================================================================================================
    // Helpers
    // =====================================================================================================================

    /// <summary>
    /// Builds a resolver over an inline single-concept document holding one global fixed-date rule, optionally
    /// year-bounded.
    /// </summary>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <param name="fromYear">The optional inclusive lower year bound.</param>
    /// <param name="toYear">The optional inclusive upper year bound.</param>
    /// <returns>A resolver for the inline document.</returns>
    private static NotableDateService FixedService(int month, int day, int? fromYear, int? toYear)
    {
        var monthName = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
        var bounds = (fromYear, toYear) switch
        {
            (int f, int t) => $" fromYear=\"{f}\" toYear=\"{t}\"",
            (int f, null) => $" fromYear=\"{f}\"",
            (null, int t) => $" toYear=\"{t}\"",
            _ => string.Empty,
        };

        var xml = $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.anchor">
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="fixed"><Applicability calendar="Gregorian"{bounds} />
                <Strategy><Fixed month="{monthName}" day="{day}" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        return new NotableDateService(NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Returns the single resolved occurrence, asserting exactly one was emitted.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <returns>The single occurrence.</returns>
    private static NotableDate Single(IReadOnlyList<NotableDate> results)
    {
        Assert.HasCount(1, results, "expected exactly one occurrence");
        return results[0];
    }

    /// <summary>
    /// Returns the single resolved occurrence with the supplied notable-date id, asserting exactly one match.
    /// </summary>
    /// <param name="results">The resolver results.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(IReadOnlyList<NotableDate> results, string notableDateId)
    {
        var matches = results.Where(r => r.NotableDateId == notableDateId).ToList();
        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}'");
        return matches[0];
    }
}
