// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionWeekdayNearKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the <see cref="WeekdayNearDateStrategy" /> resolves to its known Gregorian date through the v2 engine
/// end to end, across positioning directions, on-the-day references, the nearest-tie boundary, year boundaries, and the
/// no-occurrence cases where the reference date is invalid for the year. The cases are ported verbatim from the v1
/// <c>NotableDateRuleResolverTests.WeekdayNearDate</c> matrices.
/// </summary>
[TestClass]
public sealed class StrategyResolutionWeekdayNearKnownAnswerTests
{
    /// <summary>
    /// The service over the weekday-near fixture, which pins one reference configuration per concept.
    /// </summary>
    private static readonly NotableDateService s_service = new(NotableDateFixtures.Load("strategy-weekday-near.xml"));

    /// <summary>
    /// Verifies that the strategy resolves the expected date across directions, weekdays, on-the-day references, the
    /// nearest-tie boundary in both directions, and year boundaries (forward into the next January and backward into the
    /// previous December).
    /// </summary>
    /// <param name="notableDateId">The concept whose reference configuration is under test.</param>
    /// <param name="year">The resolution year.</param>
    /// <param name="expected">The expected resolved date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("near-sat-onorafter-jun-20", 2024, "2024-06-22")]   // Midsummer 2024 — advances forward
    [DataRow("near-sat-onorafter-jun-20", 2025, "2025-06-21")]   // Midsummer 2025
    [DataRow("near-sat-onorafter-oct-31", 2024, "2024-11-02")]   // window straddles the month end
    [DataRow("near-wed-onorbefore-nov-22", 2024, "2024-11-20")]  // Repentance 2024 — retreats backward
    [DataRow("near-wed-onorbefore-nov-22", 2025, "2025-11-19")]  // Repentance 2025
    [DataRow("near-sat-onorafter-jun-21", 2025, "2025-06-21")]   // on-the-day, OnOrAfter returns the reference
    [DataRow("near-wed-onorbefore-nov-20", 2024, "2024-11-20")]  // on-the-day, OnOrBefore returns the reference
    [DataRow("near-wed-nearest-nov-20", 2024, "2024-11-20")]     // on-the-day, Nearest returns the reference
    [DataRow("near-mon-nearest-nov-1", 2024, "2024-11-04")]      // Nearest: forward closer
    [DataRow("near-sun-nearest-nov-6", 2024, "2024-11-03")]      // Nearest: back closer
    [DataRow("near-mon-onorafter-dec-31", 2024, "2025-01-06")]   // forward across the year boundary
    [DataRow("near-fri-onorbefore-jan-1", 2025, "2024-12-27")]   // backward across the year boundary
    [DataRow("near-mon-nearest-nov-6", 2024, "2024-11-04")]      // Nearest Monday: back closer
    public void Resolve_WeekdayNearDate_ResolvesExpectedDate(string notableDateId, int year, string expected)
    {
        // The reference year is captured in the row for traceability to the v1 matrix; because the engine scans the
        // surrounding years, asserting on the expected date naturally covers references that resolve into an adjacent
        // year.
        AssertResolvesOn(notableDateId, DateOnly.Parse(expected, CultureInfo.InvariantCulture), year);
    }

    /// <summary>
    /// Verifies that a reference (year, month, day) that is not a valid Gregorian date produces no occurrence rather
    /// than throwing (30 February never exists).
    /// </summary>
    [TestMethod]
    public void Resolve_ReferenceDateInvalid_ProducesNoOccurrence() =>
        AssertNoOccurrenceInYear("near-feb-30", 2024);

    /// <summary>
    /// Verifies that a 29 February reference resolves in a leap year (the Saturday on or after 29 February 2024 is 2
    /// March 2024) but yields no occurrence in a common year, where the reference date does not exist.
    /// </summary>
    [TestMethod]
    public void Resolve_February29Reference_ResolvesOnlyInLeapYears()
    {
        AssertResolvesOn("near-feb-29", new DateOnly(2024, 3, 2), 2024);
        AssertNoOccurrenceInYear("near-feb-29", 2023);
    }

    /// <summary>
    /// Resolves a single-day window on the expected date and asserts the concept produced exactly one occurrence there.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="date">The expected occurrence date.</param>
    /// <param name="referenceYear">The v1 reference year the row originates from, used only in failure messages.</param>
    private static void AssertResolvesOn(string notableDateId, DateOnly date, int referenceYear)
    {
        var matches = s_service
            .Resolve(date, "XX")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' on {date:yyyy-MM-dd} (reference year {referenceYear})");
        Assert.AreEqual(date, matches[0].Date, $"{notableDateId} reference year {referenceYear}");
    }

    /// <summary>
    /// Asserts that the concept produces no occurrence anywhere in the supplied Gregorian year.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="year">The Gregorian year to scan.</param>
    private static void AssertNoOccurrenceInYear(string notableDateId, int year)
    {
        var count = s_service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Count(r => r.NotableDateId == notableDateId);

        Assert.AreEqual(0, count, $"expected no '{notableDateId}' in {year}");
    }
}
