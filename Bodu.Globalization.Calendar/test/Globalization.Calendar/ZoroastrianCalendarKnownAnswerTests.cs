// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ZoroastrianCalendarKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

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
    /// <summary>The shared sweep service, built once for the fifty-year vector rows.</summary>
    private static readonly Lazy<NotableDateService> s_sweepService = new(CreateService);

    /// <summary>
    /// Builds a service over the bundled <c>global-zoroastrian</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-zoroastrian");

    /// <summary>
    /// Verifies that each Zoroastrian observance resolves to the independently computed vector list across the full
    /// fifty-year sweep (Gregorian 1990-2039), pinning the Solar Hijri projection — including the years where Zartosht
    /// No-Diso straddles the Gregorian new year and lands zero or two times — against the Meeus-derived arithmetic
    /// recorded in the embedded vector table.
    /// </summary>
    /// <param name="kat">The vector row carrying the (year, observance) input and the expected occurrence list.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ZoroastrianObservanceVectors.Rows),
        typeof(ZoroastrianObservanceVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Resolve_WhenSweptAcrossVectorRange_ShouldMatchIndependentVector(ValidKat<(int Year, string ObservanceId), IReadOnlyList<DateOnly>> kat)
    {
        List<NotableDate> matches = CommonCatalogues.ResolveForYear(s_sweepService.Value, kat.Input.ObservanceId, kat.Input.Year);

        CollectionAssert.AreEqual(
            kat.Expected.ToList(),
            matches.Select(m => m.Date).ToList(),
            $"{kat.Name}: expected [{string.Join(", ", kat.Expected)}], resolved [{string.Join(", ", matches.Select(m => m.Date))}]");
    }

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
    /// occurrences of the swept Persian date; every occurrence resolved across 2023-2027 falls on one of those two civil
    /// dates.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_ZartoshtNoDiso_FallsOnYearBoundary()
    {
        NotableDateService service = CreateService();

        DateOnly[] resolved = Enumerable
            .Range(2023, 5)
            .SelectMany(year => CommonCatalogues.ResolveForYear(service, "zartosht-no-diso", year))
            .Select(occurrence => occurrence.Date)
            .OrderBy(date => date)
            .ToArray();

        // Every occurrence lands on the 1 January / 31 December civil boundary; 2025 has none (its 11 Dey anchor fell on
        // 31 December 2024) while 2024 carries two.
        CollectionAssert.AreEqual(
            new[]
            {
                new DateOnly(2023, 1, 1),
                new DateOnly(2024, 1, 1),
                new DateOnly(2024, 12, 31),
                new DateOnly(2026, 1, 1),
                new DateOnly(2027, 1, 1),
            },
            resolved);
    }
}
