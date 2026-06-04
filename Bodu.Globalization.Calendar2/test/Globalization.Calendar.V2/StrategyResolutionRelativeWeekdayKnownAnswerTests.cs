// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionRelativeWeekdayKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that the <see cref="RelativeWeekdayInMonthStrategy" /> resolves to its known Gregorian date through the v2
/// engine end to end, across positioning directions, every anchor ordinal, the nearest-tie boundary, leap-day anchors,
/// month and calendar-year boundaries, and the no-occurrence cases where the anchor ordinal does not exist. The cases
/// are ported verbatim from the v1 <c>NotableDateRuleResolverTests.RelativeWeekdayInMonth</c> matrices.
/// </summary>
[TestClass]
public sealed class StrategyResolutionRelativeWeekdayKnownAnswerTests
{
    /// <summary>
    /// The service over the relative-weekday fixture, which pins one anchor configuration per concept.
    /// </summary>
    private static readonly NotableDateService s_service = new(NotableDateFixtures.Load("strategy-relative-weekday.xml"));

    /// <summary>
    /// Verifies that the United States Election Day pattern — the Tuesday on or after the first Monday of November —
    /// resolves across years, including the earliest (first Monday on 1 November) and latest (first Monday on 7
    /// November) positions.
    /// </summary>
    /// <param name="year">The resolution year.</param>
    /// <param name="expected">The expected resolved date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2010, "2010-11-02")]
    [DataRow(2020, "2020-11-03")]
    [DataRow(2022, "2022-11-08")]
    [DataRow(2024, "2024-11-05")]
    [DataRow(2026, "2026-11-03")]
    public void Resolve_ElectionDayPattern_ResolvesTuesdayAfterFirstMonday(int year, string expected) =>
        AssertResolvesOn("rel-election", DateOnly.Parse(expected, CultureInfo.InvariantCulture), year);

    /// <summary>
    /// Verifies that the strategy resolves the expected date across the three positioning directions, every anchor
    /// ordinal, the nearest-tie boundary in both directions, the on-the-anchor case where the relative weekday equals
    /// the anchor weekday, a leap-day anchor, and month and calendar-year boundaries.
    /// </summary>
    /// <param name="notableDateId">The concept whose anchor configuration is under test.</param>
    /// <param name="year">The resolution year.</param>
    /// <param name="expected">The expected resolved date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("rel-sweep-01", 2024, "2024-08-30")]   // OnOrBefore retreats into the previous month
    [DataRow("rel-sweep-02", 2024, "2024-05-28")]   // Last ordinal anchor
    [DataRow("rel-sweep-03", 2024, "2024-06-15")]   // Third ordinal anchor
    [DataRow("rel-sweep-04", 2024, "2024-11-29")]   // Fourth ordinal anchor (US "Black Friday")
    [DataRow("rel-sweep-05", 2024, "2024-10-15")]   // Nearest: forward closer
    [DataRow("rel-sweep-06", 2024, "2024-10-13")]   // Nearest: back closer
    [DataRow("rel-sweep-07", 2024, "2024-10-14")]   // Relative == anchor, OnOrAfter lands on anchor
    [DataRow("rel-sweep-08", 2024, "2024-10-14")]   // Relative == anchor, OnOrBefore lands on anchor
    [DataRow("rel-sweep-09", 2024, "2024-10-14")]   // Relative == anchor, Nearest lands on anchor
    [DataRow("rel-sweep-10", 2023, "2023-02-23")]   // 28-day February — Fourth and Last Thursday coincide
    [DataRow("rel-sweep-11", 2024, "2024-02-29")]   // Leap-day anchor — fifth Thursday of February 2024
    [DataRow("rel-sweep-12", 2024, "2024-03-03")]   // Leap-day anchor rolls forward across the month boundary
    [DataRow("rel-sweep-13", 2024, "2023-12-29")]   // Backward across the calendar-year boundary
    [DataRow("rel-sweep-14", 2024, "2025-01-01")]   // Forward across the calendar-year boundary
    public void Resolve_RelativeWeekdayInMonth_ResolvesExpectedDate(string notableDateId, int year, string expected)
    {
        // The anchor year is captured in the row for traceability to the v1 matrix; because the engine scans the
        // surrounding years, asserting on the expected date naturally covers anchors that resolve into an adjacent year.
        AssertResolvesOn(notableDateId, DateOnly.Parse(expected, CultureInfo.InvariantCulture), year);
    }

    /// <summary>
    /// Verifies that a fifth-occurrence anchor that exists only in a leap February resolves to 29 February in the leap
    /// year 2024 but yields no occurrence in the surrounding common years 2023 and 2025.
    /// </summary>
    [TestMethod]
    public void Resolve_FifthWeekdayOnlyExistsInLeapFebruary_FlipsAcrossYears()
    {
        AssertResolvesOn("rel-sweep-11", new DateOnly(2024, 2, 29), 2024);
        AssertNoOccurrenceInYear("rel-sweep-11", 2023);
        AssertNoOccurrenceInYear("rel-sweep-11", 2025);
    }

    /// <summary>
    /// Verifies that when the requested anchor ordinal does not occur in the month (a fifth weekday in a month with only
    /// four), the strategy produces no occurrence, in both leap and common Februarys and in a 30-day month.
    /// </summary>
    /// <param name="notableDateId">The concept whose anchor ordinal does not exist for the year.</param>
    /// <param name="year">The resolution year.</param>
    [TestMethod]
    [DataRow("rel-null-mon-feb", 2024)]   // February 2024 (leap) has only four Mondays
    [DataRow("rel-null-thu-feb", 2023)]   // February 2023 (common) has only four Thursdays
    [DataRow("rel-null-thu-feb", 2025)]   // February 2025 (common) has only four Thursdays
    [DataRow("rel-null-wed-apr", 2024)]   // April 2024 (30 days) has only four Wednesdays
    public void Resolve_AnchorOrdinalDoesNotExist_ProducesNoOccurrence(string notableDateId, int year) =>
        AssertNoOccurrenceInYear(notableDateId, year);

    /// <summary>
    /// Resolves a single-day window on the expected date and asserts the concept produced exactly one occurrence there.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="date">The expected occurrence date.</param>
    /// <param name="anchorYear">The v1 anchor year the row originates from, used only in failure messages.</param>
    private static void AssertResolvesOn(string notableDateId, DateOnly date, int anchorYear)
    {
        List<NotableDate> matches = s_service
            .Resolve(date, "XX")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' on {date:yyyy-MM-dd} (anchor year {anchorYear})");
        Assert.AreEqual(date, matches[0].Date, $"{notableDateId} anchor year {anchorYear}");
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
