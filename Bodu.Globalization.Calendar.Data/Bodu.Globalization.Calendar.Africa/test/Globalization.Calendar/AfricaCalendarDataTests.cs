// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AfricaCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the Africa resource pack resolves South African, Nigerian, Kenyan, Ghanaian, Ethiopian, Egyptian,
/// and Moroccan holidays to their known dates, exercising the South African Sunday-to-Monday substitution, the
/// Western and Orthodox Easter offsets, the nth-weekday Farmers' Day, and the tabular-Hijri Islamic festivals.
/// </summary>
[TestClass]
public sealed class AfricaCalendarDataTests
    : CalendarDataTestsBase
{
    /// <inheritdoc />
    protected override IReadOnlyList<string> SupportedCountries => AfricaCalendarData.SupportedCountries;

    /// <inheritdoc />
    protected override INotableDateService CreateService(string territory) =>
        AfricaCalendarData.CreateService(territory);

    /// <summary>
    /// Verifies that each fixed, Easter-derived, or nth-weekday African holiday resolves to its known emitted date
    /// and observed flag.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // South Africa: fixed days, the Western Easter offsets, and the Sunday-to-Monday substitution (Youth Day 2024
    // falls on a Sunday and is observed on the Monday).
    [DataRow("ZA", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("ZA", 2024, "family-day-za", "2024-04-01", false)]
    [DataRow("ZA", 2024, "freedom-day", "2024-04-27", false)]
    [DataRow("ZA", 2024, "youth-day", "2024-06-17", true)]
    [DataRow("ZA", 2024, "reconciliation-day", "2024-12-16", false)]

    // Nigeria, Kenya, Ghana: fixed national days, Western Easter offsets, and the nth-weekday Farmers' Day.
    [DataRow("NG", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("NG", 2024, "easter-monday", "2024-04-01", false)]
    [DataRow("NG", 2024, "democracy-day-ng", "2024-06-12", false)]
    [DataRow("NG", 2024, "independence-day-ng", "2024-10-01", false)]
    [DataRow("KE", 2024, "madaraka-day", "2024-06-01", false)]
    [DataRow("KE", 2024, "jamhuri-day", "2024-12-12", false)]
    [DataRow("GH", 2024, "independence-day-gh", "2024-03-06", false)]
    [DataRow("GH", 2024, "farmers-day", "2024-12-06", false)]

    // Ethiopia and Egypt: Orthodox Easter offsets and the modern-era Gregorian Coptic/Ethiopic projections.
    [DataRow("ET", 2024, "ethiopian-christmas", "2024-01-07", false)]
    [DataRow("ET", 2024, "adwa-victory-day", "2024-03-02", false)]
    [DataRow("ET", 2024, "orthodox-good-friday", "2024-05-03", false)]
    [DataRow("EG", 2024, "coptic-christmas", "2024-01-07", false)]
    [DataRow("EG", 2024, "sham-ennessim", "2024-05-06", false)]
    [DataRow("EG", 2024, "armed-forces-day", "2024-10-06", false)]

    // Morocco: civil and royal commemorations.
    [DataRow("MA", 2024, "amazigh-new-year", "2024-01-14", false)]
    [DataRow("MA", 2024, "throne-day", "2024-07-30", false)]
    [DataRow("MA", 2024, "independence-day-ma", "2024-11-18", false)]
    public void Resolve_AfricanHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that an Islamic festival resolves to within two days of its known date. The tabular Hijri calendar
    /// differs from local moon-sighting, so a one- to two-day offset is expected and within tolerance.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The festival id to resolve.</param>
    /// <param name="expected">The known observed date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("NG", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("NG", 2024, "eid-al-adha", "2024-06-16")]
    [DataRow("KE", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("GH", 2024, "eid-al-adha", "2024-06-16")]
    [DataRow("ET", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("EG", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("EG", 2024, "eid-al-adha", "2024-06-16")]
    [DataRow("MA", 2024, "eid-al-fitr", "2024-04-10")]
    public void Resolve_IslamicFestival_IsWithinToleranceOfKnownDate(string territory, int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        AssertWithinDays(match.Date, DateOnly.Parse(expected, CultureInfo.InvariantCulture), 2, $"{notableDateId} {territory} {year}");
    }

    /// <summary>
    /// Verifies that a South African holiday falling on a Sunday is observed on the following Monday and carries the
    /// observed flag. Youth Day (16 June) fell on a Sunday in 2024 and is observed on Monday 17 June.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSouthAfricanHolidayFallsOnSunday_IsObservedOnMonday()
    {
        NotableDate moved = ResolveSingle("ZA", 2024, "youth-day");

        Assert.AreEqual(
            (new DateOnly(2024, 6, 17), true),
            (moved.Date, moved.IsObserved));
    }

    /// <summary>
    /// Verifies that a South African holiday falling on a Saturday is not moved and remains unobserved. Freedom Day
    /// (27 April) fell on a Saturday in 2024 and stays on its calendar date.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSouthAfricanHolidayFallsOnSaturday_IsNotMoved()
    {
        NotableDate notMoved = ResolveSingle("ZA", 2024, "freedom-day");

        Assert.AreEqual(
            (new DateOnly(2024, 4, 27), false),
            (notMoved.Date, notMoved.IsObserved));
    }
}
