// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystemKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that fixed-date rules expressed in non-Gregorian calendar systems (Chinese lunisolar, Hebrew, Persian, and
/// Islamic) project onto the correct Gregorian date, exercising the calendar-year sweep, the Hebrew leap-month alias,
/// and the Chinese leap-month skip.
/// </summary>
[TestClass]
public sealed class CalendarSystemKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the non-Gregorian calendar fixture.
    /// </summary>
    /// <returns>A service for the calendar fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("calendars.xml");

    /// <summary>
    /// Verifies that each non-Gregorian fixed-date concept resolves to its known Gregorian date for the requested year.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected projected Gregorian date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("chinese-new-year", 2023, "2023-01-22")]
    [DataRow("chinese-new-year", 2024, "2024-02-10")]
    [DataRow("chinese-new-year", 2025, "2025-01-29")]
    [DataRow("mid-autumn", 2023, "2023-09-29")]
    [DataRow("mid-autumn", 2024, "2024-09-17")]
    [DataRow("rosh-hashanah", 2023, "2023-09-16")]
    [DataRow("rosh-hashanah", 2024, "2024-10-03")]
    [DataRow("passover", 2024, "2024-04-23")]
    [DataRow("nowruz", 2023, "2023-03-21")]
    [DataRow("nowruz", 2024, "2024-03-20")]
    [DataRow("islamic-new-year", 2024, "2024-07-07")]
    public void Resolve_NonGregorianFixedDate_MatchesKnownAnswer(string notableDateId, int year, string expected)
    {
        DateOnly expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        List<NotableDate> matches = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {year}");
        Assert.AreEqual(expectedDate, matches[0].Date, $"{notableDateId} {year}");
    }
}
