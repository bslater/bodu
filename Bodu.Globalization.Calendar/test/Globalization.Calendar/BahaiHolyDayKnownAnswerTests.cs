// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BahaiHolyDayKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the floating Baha'i holy days authored in the bundled <c>global-bahai</c> catalogue. Naw-Ruz is computed by
/// the engine's vernal-equinox algorithm (in Universal Time) and the solar holy days are authored as
/// <see cref="OffsetFromRuleStrategy" /> day-offsets from it, reproducing the Badi-calendar structure.
/// </summary>
/// <remarks>
/// <para>
/// The engine evaluates the equinox in Universal Time rather than at Tehran, so a year whose equinox falls late on
/// 20 March UT (notably 2023) resolves one day before the Tehran-anchored Baha'i date. The dates are therefore validated
/// against the published Western reckoning with a two-day tolerance, which the verification report cross-checks against
/// the official Badi dates announced for 2025 (First Day of Ridvan 20 April, Declaration of the Bab 23 May, Ascension of
/// Baha'u'llah 28 May), all reproduced exactly by the engine.
/// </para>
/// </remarks>
[TestClass]
public sealed class BahaiHolyDayKnownAnswerTests
{
    /// <summary>
    /// The maximum tolerated difference, in days, between a resolved Baha'i holy day and its published reference date.
    /// </summary>
    private const int ToleranceDays = 2;

    /// <summary>
    /// Builds a service over the bundled <c>global-bahai</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-bahai");

    /// <summary>
    /// Verifies that each Baha'i holy day resolves to within two days of its published Western reference date for every
    /// year 2023-2027.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The reference Gregorian month.</param>
    /// <param name="day">The reference Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("naw-ruz", 3, 21)]
    [DataRow("first-day-of-ridvan", 4, 21)]
    [DataRow("ninth-day-of-ridvan", 4, 29)]
    [DataRow("twelfth-day-of-ridvan", 5, 2)]
    [DataRow("declaration-of-the-bab", 5, 23)]
    [DataRow("ascension-of-bahaullah", 5, 29)]
    [DataRow("martyrdom-of-the-bab", 7, 9)]
    [DataRow("day-of-the-covenant", 11, 26)]
    [DataRow("ascension-of-abdul-baha", 11, 28)]
    public void Resolve_BahaiHolyDay_IsWithinToleranceOfPublishedDate(string notableDateId, int month, int day)
    {
        NotableDateService service = CreateService();

        for (var year = 2023; year <= 2027; year++)
        {
            NotableDate observance = CommonCatalogues.ResolveSingle(service, notableDateId, year);
            var reference = new DateOnly(year, month, day);
            var deltaDays = Math.Abs(observance.Date.DayNumber - reference.DayNumber);

            Assert.IsLessThanOrEqualTo(
                ToleranceDays,
                deltaDays,
                $"{notableDateId} {year}: resolved {observance.Date:yyyy-MM-dd}, expected within {ToleranceDays} days of {reference:yyyy-MM-dd}");
        }
    }

    /// <summary>
    /// Verifies that the holy days whose 2025 Gregorian dates were announced by the Baha'i World Centre resolve to those
    /// exact dates, confirming the equinox-plus-offset model reproduces the official Badi reckoning for that year.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("naw-ruz", 3, 20)]
    [DataRow("first-day-of-ridvan", 4, 20)]
    [DataRow("declaration-of-the-bab", 5, 23)]
    [DataRow("ascension-of-bahaullah", 5, 28)]
    public void Resolve_BahaiHolyDay_MatchesOfficial2025BadiDate(string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, 2025);

        Assert.AreEqual(new DateOnly(2025, month, day), observance.Date, $"{notableDateId} 2025");
    }

    /// <summary>
    /// Verifies that Naw-Ruz always falls within the equinox window of 19, 20 or 21 March across the multi-decade span
    /// 2000-2050.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_NawRuzAcrossDecades_ShouldFallInEquinoxWindow()
    {
        NotableDateService service = CreateService();

        DateOnly[] outsideWindow = Enumerable
            .Range(2000, 51)
            .Select(year => CommonCatalogues.ResolveSingle(service, "naw-ruz", year).Date)
            .Where(date => date.Month != 3 || date.Day is not (19 or 20 or 21))
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<DateOnly>(), outsideWindow);
    }

    /// <summary>
    /// Verifies that the Festival of Ridvan preserves its authored twelve-day duration.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_Ridvan_ShouldSpanTwelveDays()
    {
        Assert.AreEqual(12, CommonCatalogues.ResolveSingle(CreateService(), "ridvan", 2025).DurationDays);
    }
}
