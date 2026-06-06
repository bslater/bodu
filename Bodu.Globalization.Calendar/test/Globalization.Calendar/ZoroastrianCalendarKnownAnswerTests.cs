// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ZoroastrianCalendarKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the year-by-year known-answer table for the Zoroastrian (Fasli / Iranian) observances authored in the
/// bundled <c>global-zoroastrian</c> catalogue. Each observance is a <see cref="FixedDateStrategy" /> at a fixed
/// month/day of the <see cref="CalendarSystem.Persian" /> calendar with a calendar-year sweep, projected onto the
/// Gregorian year by the base class library's <see cref="System.Globalization.PersianCalendar" />.
/// </summary>
/// <remarks>
/// <para>
/// The base class library Persian calendar uses the 33-year arithmetic leap rule, which can differ from the astronomical
/// Solar Hijri calendar published by Iran by a day around the equinox (for example Nowruz 2025 resolves to 21 March,
/// while Iran observed 20 March). Those divergences are recorded in the verification report; the dates pinned here are
/// the deterministic base class library projections, matching the existing Persian known-answer suite.
/// </para>
/// </remarks>
[TestClass]
public sealed class ZoroastrianCalendarKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the bundled <c>global-zoroastrian</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-zoroastrian");

    /// <summary>
    /// Verifies that each equinox-anchored Zoroastrian observance resolves to its base class library Persian-calendar
    /// date across 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Nowruz / Jamshedi Navroz (1 Farvardin).
    [DataRow(2023, "zoroastrian-nowruz", 3, 21)]
    [DataRow(2024, "zoroastrian-nowruz", 3, 20)]
    [DataRow(2025, "zoroastrian-nowruz", 3, 21)]
    [DataRow(2026, "zoroastrian-nowruz", 3, 21)]
    [DataRow(2027, "zoroastrian-nowruz", 3, 21)]

    // Khordad Sal (6 Farvardin) - six days after Nowruz.
    [DataRow(2023, "khordad-sal", 3, 26)]
    [DataRow(2024, "khordad-sal", 3, 25)]
    [DataRow(2025, "khordad-sal", 3, 26)]
    [DataRow(2026, "khordad-sal", 3, 26)]
    [DataRow(2027, "khordad-sal", 3, 26)]

    // Tirgan (13 Tir).
    [DataRow(2023, "tirgan", 7, 4)]
    [DataRow(2024, "tirgan", 7, 3)]
    [DataRow(2025, "tirgan", 7, 4)]
    [DataRow(2026, "tirgan", 7, 4)]
    [DataRow(2027, "tirgan", 7, 4)]

    // Mehregan (16 Mehr).
    [DataRow(2023, "mehregan", 10, 8)]
    [DataRow(2024, "mehregan", 10, 7)]
    [DataRow(2025, "mehregan", 10, 8)]
    [DataRow(2026, "mehregan", 10, 8)]
    [DataRow(2027, "mehregan", 10, 8)]

    // Sadeh (10 Bahman) - mid-winter, fifty days before Nowruz.
    [DataRow(2023, "sadeh", 1, 30)]
    [DataRow(2024, "sadeh", 1, 30)]
    [DataRow(2025, "sadeh", 1, 29)]
    [DataRow(2026, "sadeh", 1, 30)]
    [DataRow(2027, "sadeh", 1, 30)]
    public void Resolve_ZoroastrianObservance_YieldsPersianCalendarDate(int year, string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, month, day), observance.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that Zartosht No-Diso (11 Dey) always lands on the 31 December / 1 January boundary of the Gregorian
    /// year. Because that boundary straddles the year end, a given Gregorian year can contain zero, one or two
    /// occurrences of the swept Persian date; every occurrence resolved across a multi-year span must fall on one of
    /// those two civil dates.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_ZartoshtNoDiso_FallsOnYearBoundary()
    {
        NotableDateService service = CreateService();
        var occurrences = 0;

        for (var year = 2023; year <= 2027; year++)
        {
            foreach (NotableDate occurrence in CommonCatalogues.ResolveForYear(service, "zartosht-no-diso", year))
            {
                occurrences++;
                var landsOnBoundary =
                    (occurrence.Date.Month == 1 && occurrence.Date.Day == 1) ||
                    (occurrence.Date.Month == 12 && occurrence.Date.Day == 31);
                Assert.IsTrue(landsOnBoundary, $"zartosht-no-diso {year} fell on {occurrence.Date:yyyy-MM-dd}");
            }
        }

        Assert.IsGreaterThan(0, occurrences, "expected at least one Zartosht No-Diso occurrence across 2023-2027");
    }
}
