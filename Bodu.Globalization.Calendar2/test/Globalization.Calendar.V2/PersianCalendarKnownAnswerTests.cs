// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PersianCalendarKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the year-by-year known-answer table for the Persian (Solar Hijri) observances authored in
/// <c>persian.xml</c>: each festival is a <see cref="FixedDateStrategy" /> expressed in the
/// <see cref="CalendarSystem.Persian" /> calendar with a calendar-year sweep, projected onto the Gregorian year
/// through the service. Expected dates are sourced from the Iranian civil calendar (time.ir) and reproduced by the
/// base class library's <see cref="System.Globalization.PersianCalendar" />, ported from the v1 <c>global-persian.xml</c>
/// resource tests.
/// </summary>
[TestClass]
public sealed class PersianCalendarKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the Persian fixture.
    /// </summary>
    /// <returns>A service for the Persian fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("persian.xml");

    /// <summary>
    /// Resolves the occurrences of a named observance whose anchor falls in the supplied Gregorian year.
    /// </summary>
    /// <param name="service">The service to resolve through.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The occurrences anchored in the requested year.</returns>
    private static List<NotableDate> ResolveForYear(NotableDateService service, string notableDateId, int year) =>
        service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Where(r => r.NotableDateId == notableDateId && r.Date.Year == year)
            .ToList();

    /// <summary>
    /// Verifies that all three Persian observances declared in <c>persian.xml</c> resolve for a representative
    /// Gregorian year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_WhenLoadingPersian_ResolvesAllThreeObservances()
    {
        NotableDateService service = CreateService();

        HashSet<string> ids = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Select(r => r.NotableDateId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(ids.Contains("nowruz"));
        Assert.IsTrue(ids.Contains("sizdah-bedar"));
        Assert.IsTrue(ids.Contains("yalda-night"));
    }

    /// <summary>
    /// Verifies that each Persian observance resolves to its known Gregorian date for representative years (Persian
    /// 1401-1405, Gregorian 2022-2026), reproduced by the base class library's
    /// <see cref="System.Globalization.PersianCalendar" />.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedMonth">The expected Gregorian month.</param>
    /// <param name="expectedDay">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Nowruz (1 Farvardin) - vernal equinox; 20 or 21 March depending on the year.
    [DataRow(2022, "nowruz", 3, 21)]
    [DataRow(2023, "nowruz", 3, 21)]
    [DataRow(2024, "nowruz", 3, 20)]
    [DataRow(2025, "nowruz", 3, 21)]
    [DataRow(2026, "nowruz", 3, 21)]

    // Sizdah Bedar (13 Farvardin) - Nature's Day; 1 or 2 April.
    [DataRow(2022, "sizdah-bedar", 4, 2)]
    [DataRow(2023, "sizdah-bedar", 4, 2)]
    [DataRow(2024, "sizdah-bedar", 4, 1)]
    [DataRow(2025, "sizdah-bedar", 4, 2)]

    // Yalda Night (30 Azar) - longest night; 20 or 21 December.
    [DataRow(2022, "yalda-night", 12, 21)]
    [DataRow(2023, "yalda-night", 12, 21)]
    [DataRow(2024, "yalda-night", 12, 20)]
    [DataRow(2025, "yalda-night", 12, 21)]
    public void Resolve_PersianObservance_YieldsIranianCivilDate(int year, string notableDateId, int expectedMonth, int expectedDay)
    {
        List<NotableDate> matches = ResolveForYear(CreateService(), notableDateId, year);

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' anchored in {year}");
        Assert.AreEqual(new DateOnly(year, expectedMonth, expectedDay), matches[0].Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that Nowruz preserves its authored 13-day duration (capturing the traditional Nowruz holiday period
    /// that culminates in Sizdah Bedar) across the resolution pipeline.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_Nowruz_PreservesThirteenDayDuration()
    {
        List<NotableDate> matches = ResolveForYear(CreateService(), "nowruz", 2024);

        Assert.AreEqual(1, matches.Count, "expected exactly one 'nowruz' anchored in 2024");
        Assert.AreEqual(13, matches[0].DurationDays);
    }

    /// <summary>
    /// Verifies that Nowruz always falls on 20 or 21 March across a multi-decade span. This is a contract of the
    /// Persian solar calendar: the new year is anchored to the day containing the vernal equinox at the central
    /// meridian of Iran, which is invariably one of those two Gregorian dates within the supported range.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_NowruzAcrossDecades_AlwaysFallsOnMarch20Or21()
    {
        NotableDateService service = CreateService();

        for (int year = 1990; year <= 2050; year++)
        {
            List<NotableDate> matches = ResolveForYear(service, "nowruz", year);

            Assert.AreEqual(1, matches.Count, $"Nowruz unresolved for Gregorian year {year}.");
            Assert.AreEqual(3, matches[0].Date.Month, $"Nowruz {year} fell outside March: {matches[0].Date:yyyy-MM-dd}.");
            Assert.IsTrue(
                matches[0].Date.Day is 20 or 21,
                $"Nowruz {year} fell on March {matches[0].Date.Day} (expected 20 or 21).");
        }
    }
}
