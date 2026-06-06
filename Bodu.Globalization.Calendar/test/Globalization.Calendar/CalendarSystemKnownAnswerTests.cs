// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystemKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

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
        var expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        var matches = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}' for {year}");
        Assert.AreEqual(expectedDate, matches[0].Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that a multi-day occurrence carries its duration and end date, is returned by a single-day query for a
    /// day inside its span, and is excluded by a query that falls entirely before or after the span.
    /// </summary>
    [TestMethod]
    public void Resolve_MultiDayOccurrence_IsIncludedWhenTheQueryIntersectsTheSpan()
    {
        NotableDateService service = CreateService();

        var inside = service
            .Resolve(new DateOnly(2024, 5, 2), "XX")
            .Where(r => r.NotableDateId == "golden-week")
            .ToList();

        Assert.HasCount(1, inside, "a day inside the span returns the occurrence");
        Assert.AreEqual(new DateOnly(2024, 4, 29), inside[0].Date, "span start");
        Assert.AreEqual(7, inside[0].DurationDays, "duration");
        Assert.AreEqual(new DateOnly(2024, 5, 5), inside[0].EndDate, "span end");

        var before = service.Resolve(new DateOnly(2024, 4, 28), "XX").Count(r => r.NotableDateId == "golden-week");
        var after = service.Resolve(new DateOnly(2024, 5, 6), "XX").Count(r => r.NotableDateId == "golden-week");

        Assert.AreEqual(0, before, "a day before the span is excluded");
        Assert.AreEqual(0, after, "a day after the span is excluded");
    }

    /// <summary>
    /// Verifies that an Islamic fixed date that recurs within a single Gregorian year is emitted twice. Because the
    /// Hijri year is about eleven days shorter than the Gregorian year, the same month and day can fall in early
    /// January and again in late December of one Gregorian year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_WhenIslamicDateRecursInOneGregorianYear_EmitsBothOccurrences()
    {
        var occurrences = CreateService()
            .Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2075, 12, 31)), "XX")
            .Where(r => r.NotableDateId == "islamic-new-year")
            .ToList();

        var doubledYears = occurrences
            .GroupBy(r => r.Date.Year)
            .Where(g => g.Count() == 2)
            .ToList();

        Assert.IsNotEmpty(doubledYears, "expected at least one Gregorian year with two Islamic New Year occurrences");

        IGrouping<int, NotableDate> doubled = doubledYears[0];
        var dates = doubled.Select(r => r.Date).OrderBy(d => d).ToList();
        Assert.AreEqual(doubled.Key, dates[0].Year, "first occurrence is in the Gregorian year");
        Assert.AreEqual(doubled.Key, dates[1].Year, "second occurrence is in the same Gregorian year");
        Assert.IsGreaterThan(300, dates[1].DayNumber - dates[0].DayNumber, "the two occurrences are about a lunar year apart");
    }
}
