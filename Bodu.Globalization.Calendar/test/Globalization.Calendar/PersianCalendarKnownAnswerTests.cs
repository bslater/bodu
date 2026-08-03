// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PersianCalendarKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the year-by-year known-answer table for the Persian (Solar Hijri) observances authored in
/// <c>persian.xml</c>: each festival is a <see cref="FixedDateStrategy" /> expressed in the
/// <see cref="CalendarSystem.Persian" /> calendar with a calendar-year sweep, projected onto the Gregorian year
/// through the service. Expected dates are sourced from the Iranian civil calendar (time.ir) and reproduced by the
/// base class library's <see cref="System.Globalization.PersianCalendar" />, ported from the v1 <c>global-persian.xml</c>
/// resource tests. The fifty-year sweep additionally pins the bundled <c>global-persian</c> catalogue against the
/// independently computed vector table.
/// </summary>
[TestClass]
public sealed class PersianCalendarKnownAnswerTests
{
    /// <summary>The shared sweep service over the bundled catalogue, built once for the fifty-year vector rows.</summary>
    private static readonly Lazy<NotableDateService> s_sweepService = new(() => CommonCatalogues.Service("global-persian"));

    /// <summary>
    /// Builds a service over the Persian fixture.
    /// </summary>
    /// <returns>A service for the Persian fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("persian.xml");

    /// <summary>
    /// Verifies that each Persian observance in the bundled <c>global-persian</c> catalogue resolves to the
    /// independently computed vector date across the full fifty-year sweep (Gregorian 1990-2039), pinning the
    /// equinox-anchored Solar Hijri projection against the Meeus-derived arithmetic recorded in the embedded vector
    /// table.
    /// </summary>
    /// <param name="kat">The vector row carrying the (year, observance) input and the expected date.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(PersianObservanceVectors.Rows),
        typeof(PersianObservanceVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Resolve_WhenSweptAcrossVectorRange_ShouldMatchIndependentVector(ValidKat<(int Year, string ObservanceId), DateOnly> kat)
    {
        List<NotableDate> matches = CommonCatalogues.ResolveForYear(s_sweepService.Value, kat.Input.ObservanceId, kat.Input.Year);

        Assert.HasCount(1, matches, $"expected exactly one '{kat.Input.ObservanceId}' in {kat.Input.Year}");
        Assert.AreEqual(kat.Expected, matches[0].Date, kat.Name);
    }

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
    /// Verifies that each Persian observance declared in <c>persian.xml</c> resolves for a representative Gregorian
    /// year.
    /// </summary>
    /// <param name="notableDateId">The notable-date id expected to resolve.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("nowruz")]
    [DataRow("sizdah-bedar")]
    [DataRow("yalda-night")]
    public void Resolve_WhenLoadingPersian_ShouldResolveObservance(string notableDateId)
    {
        var ids = CreateService()
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Select(r => r.NotableDateId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(notableDateId, ids);
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

        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}' anchored in {year}");
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

        Assert.HasCount(1, matches, "expected exactly one 'nowruz' anchored in 2024");
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

            Assert.HasCount(1, matches, $"Nowruz unresolved for Gregorian year {year}.");
            Assert.IsTrue(
                matches[0].Date == new DateOnly(year, 3, 20) || matches[0].Date == new DateOnly(year, 3, 21),
                $"Nowruz {year} fell on {matches[0].Date:yyyy-MM-dd} (expected 20 or 21 March).");
        }
    }
}
