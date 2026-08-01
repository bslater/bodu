// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalIslamicCatalogueKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the year-by-year known-answer tables for the Hijri observances authored in the bundled
/// <c>global-islamic</c> (tabular) and <c>global-islamic-umm-al-qura</c> catalogues, including the Hajj-season and
/// Ramadan observances added in the latest pass (Isra and Mi'raj, Laylat al-Qadr, Hajj, Day of Arafah). Each anchor is a
/// <see cref="FixedDateStrategy" /> in the Hijri calendar with a calendar-year sweep; Laylat al-Qadr is an
/// <see cref="OffsetFromRuleStrategy" /> offset from Ramadan. Expected dates are produced by the base class library's
/// <see cref="System.Globalization.HijriCalendar" /> (at the default <c>HijriAdjustment = 0</c>) and
/// <see cref="System.Globalization.UmAlQuraCalendar" />; both follow arithmetic reckoning and can differ from a
/// locally moon-sighted observance by up to two days. The fifty-year sweep additionally pins the Umm al-Qura
/// catalogue against the astronomically cross-checked vector table.
/// </summary>
[TestClass]
public sealed class GlobalIslamicCatalogueKnownAnswerTests
{
    /// <summary>The shared sweep service over the Umm al-Qura catalogue, built once for the fifty-year vector rows.</summary>
    private static readonly Lazy<NotableDateService> s_ummAlQuraSweepService =
        new(() => CommonCatalogues.Service("global-islamic-umm-al-qura"));

    /// <summary>
    /// Verifies that each Umm al-Qura observance resolves to the vector table's ordered occurrence list across the
    /// full fifty-year sweep (Gregorian 1990-2039), pinning the KACST month-table projection — including the years
    /// where a short lunar date lands twice in one Gregorian year — against the astronomically cross-checked vectors.
    /// </summary>
    /// <param name="kat">The vector row carrying the (year, observance) input and the expected occurrence list.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(UmmAlQuraObservanceVectors.Rows),
        typeof(UmmAlQuraObservanceVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Resolve_WhenSweptAcrossVectorRange_ShouldMatchUmmAlQuraVector(ValidKat<(int Year, string ObservanceId), IReadOnlyList<DateOnly>> kat)
    {
        List<NotableDate> matches = CommonCatalogues.ResolveForYear(s_ummAlQuraSweepService.Value, kat.Input.ObservanceId, kat.Input.Year);

        CollectionAssert.AreEqual(
            kat.Expected.ToList(),
            matches.Select(m => m.Date).ToList(),
            $"{kat.Name}: expected [{string.Join(", ", kat.Expected)}], resolved [{string.Join(", ", matches.Select(m => m.Date))}]");
    }

    /// <summary>
    /// Verifies that each Umm al-Qura observance resolves within one day of the date the High Judiciary Council of
    /// Saudi Arabia actually announced (Hijri 1422-1448), pinning the computed catalogue against the gazetted civil
    /// dates. One day is the empirically maximal computed-versus-announced divergence across the range - the
    /// announcements moved seventeen month starts by a single day (in both directions) and never more.
    /// </summary>
    /// <param name="kat">The vector row carrying the (year, observance) input and the announced date.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(SaudiAnnouncedObservanceVectors.Rows),
        typeof(SaudiAnnouncedObservanceVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Resolve_WhenSweptAcrossAnnouncedDates_ShouldBeWithinOneDayOfGazettedDate(ValidKat<(int Year, string ObservanceId), DateOnly> kat)
    {
        List<NotableDate> matches = CommonCatalogues.ResolveForYear(s_ummAlQuraSweepService.Value, kat.Input.ObservanceId, kat.Input.Year);

        Assert.IsNotEmpty(matches, $"expected at least one '{kat.Input.ObservanceId}' in {kat.Input.Year}");

        int distance = matches.Min(m => Math.Abs(m.Date.DayNumber - kat.Expected.DayNumber));
        Assert.IsTrue(
            distance <= 1,
            $"{kat.Name}: resolved [{string.Join(", ", matches.Select(m => m.Date))}] is {distance} days from the announced date");
    }

    /// <summary>
    /// Verifies that each tabular-Hijri observance resolves to its base class library date across 2023-2025.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // 2023 (Hijri 1444 / start of 1445)
    [DataRow(2023, "isra-and-miraj", 2, 17)]
    [DataRow(2023, "ramadan", 3, 22)]
    [DataRow(2023, "laylat-al-qadr", 4, 17)]
    [DataRow(2023, "eid-al-fitr", 4, 21)]
    [DataRow(2023, "hajj", 6, 26)]
    [DataRow(2023, "day-of-arafah", 6, 27)]
    [DataRow(2023, "eid-al-adha", 6, 28)]
    [DataRow(2023, "islamic-new-year", 7, 18)]
    [DataRow(2023, "day-of-ashura", 7, 27)]
    [DataRow(2023, "mawlid-al-nabi", 9, 26)]

    // 2024 (Hijri 1445 / start of 1446)
    [DataRow(2024, "isra-and-miraj", 2, 6)]
    [DataRow(2024, "ramadan", 3, 10)]
    [DataRow(2024, "laylat-al-qadr", 4, 5)]
    [DataRow(2024, "eid-al-fitr", 4, 9)]
    [DataRow(2024, "hajj", 6, 14)]
    [DataRow(2024, "day-of-arafah", 6, 15)]
    [DataRow(2024, "eid-al-adha", 6, 16)]
    [DataRow(2024, "islamic-new-year", 7, 7)]
    [DataRow(2024, "day-of-ashura", 7, 16)]
    [DataRow(2024, "mawlid-al-nabi", 9, 15)]

    // 2025 (Hijri 1446 / start of 1447)
    [DataRow(2025, "isra-and-miraj", 1, 26)]
    [DataRow(2025, "ramadan", 2, 28)]
    [DataRow(2025, "laylat-al-qadr", 3, 26)]
    [DataRow(2025, "eid-al-fitr", 3, 30)]
    [DataRow(2025, "hajj", 6, 4)]
    [DataRow(2025, "day-of-arafah", 6, 5)]
    [DataRow(2025, "eid-al-adha", 6, 6)]
    [DataRow(2025, "islamic-new-year", 6, 26)]
    [DataRow(2025, "day-of-ashura", 7, 5)]
    [DataRow(2025, "mawlid-al-nabi", 9, 4)]
    public void Resolve_TabularHijriObservance_YieldsHijriCalendarDate(int year, string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CommonCatalogues.Service("global-islamic"), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, month, day), observance.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that each Umm al-Qura observance resolves to its base class library date across 2023-2025.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // 2023
    [DataRow(2023, "isra-and-miraj", 2, 18)]
    [DataRow(2023, "ramadan", 3, 23)]
    [DataRow(2023, "eid-al-fitr", 4, 21)]
    [DataRow(2023, "day-of-arafah", 6, 27)]
    [DataRow(2023, "eid-al-adha", 6, 28)]
    [DataRow(2023, "islamic-new-year", 7, 19)]
    [DataRow(2023, "mawlid-al-nabi", 9, 27)]

    // 2024
    [DataRow(2024, "isra-and-miraj", 2, 8)]
    [DataRow(2024, "ramadan", 3, 11)]
    [DataRow(2024, "eid-al-fitr", 4, 10)]
    [DataRow(2024, "day-of-arafah", 6, 15)]
    [DataRow(2024, "eid-al-adha", 6, 16)]
    [DataRow(2024, "islamic-new-year", 7, 7)]
    [DataRow(2024, "mawlid-al-nabi", 9, 15)]

    // 2025
    [DataRow(2025, "isra-and-miraj", 1, 27)]
    [DataRow(2025, "ramadan", 3, 1)]
    [DataRow(2025, "eid-al-fitr", 3, 30)]
    [DataRow(2025, "day-of-arafah", 6, 5)]
    [DataRow(2025, "eid-al-adha", 6, 6)]
    [DataRow(2025, "islamic-new-year", 6, 26)]
    [DataRow(2025, "mawlid-al-nabi", 9, 4)]
    public void Resolve_UmmAlQuraObservance_YieldsUmmAlQuraCalendarDate(int year, string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CommonCatalogues.Service("global-islamic-umm-al-qura"), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, month, day), observance.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that the multi-day tabular Hijri observances preserve their authored duration across the resolution
    /// pipeline.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedDuration">The expected authored duration in days.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("ramadan", 30)]
    [DataRow("eid-al-fitr", 3)]
    [DataRow("hajj", 6)]
    [DataRow("eid-al-adha", 4)]
    [DataRow("day-of-arafah", 1)]
    public void Resolve_TabularHijriObservance_PreservesAuthoredDurationDays(string notableDateId, int expectedDuration)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CommonCatalogues.Service("global-islamic"), notableDateId, 2024);

        Assert.AreEqual(expectedDuration, observance.DurationDays, notableDateId);
    }
}
