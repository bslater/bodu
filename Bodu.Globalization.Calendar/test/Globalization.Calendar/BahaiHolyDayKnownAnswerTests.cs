// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BahaiHolyDayKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

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

    /// <summary>The shared catalogue service, built once for the fifty-year vector sweep.</summary>
    private static readonly Lazy<NotableDateService> s_sweepService = new(CreateService);

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

        for (int year = 2023; year <= 2027; year++)
        {
            NotableDate observance = CommonCatalogues.ResolveSingle(service, notableDateId, year);
            var reference = new DateOnly(year, month, day);
            int deltaDays = Math.Abs(observance.Date.DayNumber - reference.DayNumber);

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
    /// Verifies that each Baha'i holy day resolves to within one day of the official 183 B.E. (2026) Gregorian date
    /// published in the Baha'i calendar for that year.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The official Gregorian month.</param>
    /// <param name="day">The official Gregorian day.</param>
    /// <remarks>
    /// <para>
    /// 2026 is a Tehran-sunset boundary year: the March equinox falls at roughly 14:46 UT on 20 March, which is after
    /// sunset in Tehran, so the official Badi year begins on 21 March while the engine's Universal-Time equinox model
    /// anchors it to 20 March. The engine therefore resolves every offset holy day uniformly one day before the
    /// official date, which this anchor pins at a one-day tolerance (tighter than the general two-day window above).
    /// The official table's two Twin Birthday dates (Birth of the Bab 10 November, Birth of Baha'u'llah
    /// 11 November 2026) are intentionally not asserted: the catalogue omits those concepts until the dedicated
    /// eighth-new-moon-after-Naw-Ruz algorithm exists.
    /// </para>
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("naw-ruz", 3, 21)]
    [DataRow("first-day-of-ridvan", 4, 21)]
    [DataRow("ninth-day-of-ridvan", 4, 29)]
    [DataRow("twelfth-day-of-ridvan", 5, 2)]
    [DataRow("declaration-of-the-bab", 5, 24)]
    [DataRow("ascension-of-bahaullah", 5, 29)]
    [DataRow("martyrdom-of-the-bab", 7, 10)]
    [DataRow("day-of-the-covenant", 11, 26)]
    [DataRow("ascension-of-abdul-baha", 11, 28)]
    public void Resolve_BahaiHolyDay_WhenComparedWith183BEOfficialDate_ShouldBeWithinOneDay(string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, 2026);
        var official = new DateOnly(2026, month, day);
        int deltaDays = Math.Abs(observance.Date.DayNumber - official.DayNumber);

        Assert.IsLessThanOrEqualTo(
            1,
            deltaDays,
            $"{notableDateId} 183 B.E.: resolved {observance.Date:yyyy-MM-dd}, official {official:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies that every holy day resolves to the official Universal House of Justice date, or exactly one day
    /// before it, across the full fifty-year Badi table 172-221 B.E. (2015-2064).
    /// </summary>
    /// <param name="kat">The vector row carrying the (Gregorian year, holy day) input and the official date.</param>
    /// <remarks>
    /// <para>
    /// The measured relationship between the engine's Universal-Time equinox model and the official Tehran-anchored
    /// reckoning is asymmetric and year-uniform: the engine is exact in the thirty years whose official Naw-Ruz is
    /// 20 March and exactly one day early in the twenty Tehran-sunset boundary years whose official Naw-Ruz is
    /// 21 March — never late and never mixed within a year. The sweep therefore asserts the signed bound
    /// (official minus one day, or official) rather than a symmetric tolerance.
    /// </para>
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(BahaiHolyDayVectors.Rows),
        typeof(BahaiHolyDayVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Resolve_WhenSweptAcrossOfficialBadiTable_ShouldMatchOfficialDateOrRunOneDayEarly(ValidKat<(int Year, string ObservanceId), DateOnly> kat)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(s_sweepService.Value, kat.Input.ObservanceId, kat.Input.Year);
        int deltaDays = observance.Date.DayNumber - kat.Expected.DayNumber;

        Assert.IsTrue(
            deltaDays is 0 or -1,
            $"{kat.Name}: resolved {observance.Date:yyyy-MM-dd}, official {kat.Expected:yyyy-MM-dd} (delta {deltaDays:+0;-0;0}d)");
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
