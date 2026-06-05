// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionCalendarSystemKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="FixedDateStrategy" /> rules expressed in non-Gregorian calendar systems project onto the
/// correct Gregorian date through the v2 engine, exercising the Chinese lunisolar direct and leap-month-skip paths, the
/// Hebrew calendar-year sweep with leap-shifting month aliases, and the Islamic and Umm al-Qura sweeps. The cases are
/// ported verbatim from the v1 <c>NotableDateRuleResolverTests</c> CalendarType / SkipLeapMonth / SweepCalendarYears
/// matrices and the Hebrew alias tests.
/// </summary>
[TestClass]
public sealed class StrategyResolutionCalendarSystemKnownAnswerTests
{
    /// <summary>
    /// The service over the non-Gregorian calendar-system fixture.
    /// </summary>
    private static readonly NotableDateService s_service = new(NotableDateFixtures.Load("strategy-calendar-systems.xml"));

    /// <summary>
    /// Verifies that a Chinese lunisolar fixed date (month 1, day 1) projects Lunar New Year onto its known Gregorian
    /// date across a wide span of years, including the lower and upper extents of the supported calendar range.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected projected Gregorian date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(1901, "1901-02-19")]
    [DataRow(2000, "2000-02-05")]
    [DataRow(2020, "2020-01-25")]
    [DataRow(2024, "2024-02-10")]
    [DataRow(2025, "2025-01-29")]
    [DataRow(2026, "2026-02-17")]
    [DataRow(2100, "2100-02-09")]
    public void Resolve_ChineseLunarNewYear_MatchesKnownAnswer(int year, string expected) =>
        AssertResolvesInYear("lunar-new-year", year, DateOnly.Parse(expected, CultureInfo.InvariantCulture));

    /// <summary>
    /// Verifies that a Chinese conventional-month festival that skips the intercalary leap month projects onto its known
    /// Gregorian date, including years that contain a leap month before the festival's month.
    /// </summary>
    /// <param name="notableDateId">The festival concept to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected projected Gregorian date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("dragon-boat", 2022, "2022-06-03")]
    [DataRow("dragon-boat", 2023, "2023-06-22")]
    [DataRow("dragon-boat", 2024, "2024-06-10")]
    [DataRow("mid-autumn-festival", 2022, "2022-09-10")]
    [DataRow("mid-autumn-festival", 2023, "2023-09-29")]
    [DataRow("mid-autumn-festival", 2024, "2024-09-17")]
    [DataRow("double-ninth", 2022, "2022-10-04")]
    [DataRow("double-ninth", 2023, "2023-10-23")]
    [DataRow("double-ninth", 2024, "2024-10-11")]
    public void Resolve_ChineseSkipLeapMonthFestival_MatchesKnownAnswer(string notableDateId, int year, string expected) =>
        AssertResolvesInYear(notableDateId, year, DateOnly.Parse(expected, CultureInfo.InvariantCulture));

    /// <summary>
    /// Verifies that a Chinese leap-month-skip festival produces no occurrence for years outside the supported
    /// ChineseLunisolarCalendar range.
    /// </summary>
    /// <param name="year">The unsupported Gregorian year.</param>
    [TestMethod]
    [DataRow(1800)]
    [DataRow(2200)]
    public void Resolve_ChineseSkipLeapMonth_YearOutOfRange_ProducesNoOccurrence(int year) =>
        AssertNoOccurrenceInYear("dragon-boat", year);

    /// <summary>
    /// Verifies that Hebrew fixed dates resolved by a calendar-year sweep project onto their known Gregorian dates,
    /// including the Nisan and LastAdar aliases whose month index shifts in a leap Hebrew year.
    /// </summary>
    /// <param name="notableDateId">The Hebrew observance concept to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected projected Gregorian date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("rosh-hashanah", 2022, "2022-09-26")]
    [DataRow("rosh-hashanah", 2023, "2023-09-16")]
    [DataRow("rosh-hashanah", 2024, "2024-10-03")]
    [DataRow("passover", 2022, "2022-04-16")]
    [DataRow("passover", 2023, "2023-04-06")]
    [DataRow("passover", 2024, "2024-04-23")]
    [DataRow("hanukkah", 2022, "2022-12-19")]
    [DataRow("hanukkah", 2023, "2023-12-08")]
    [DataRow("hanukkah", 2024, "2024-12-26")]
    [DataRow("purim", 2022, "2022-03-17")]   // Hebrew 5782 — leap year (Adar II)
    [DataRow("purim", 2023, "2023-03-07")]   // Hebrew 5783 — non-leap year
    [DataRow("purim", 2024, "2024-03-24")]   // Hebrew 5784 — leap year (Adar II)
    public void Resolve_HebrewSweepObservance_MatchesKnownAnswer(string notableDateId, int year, string expected) =>
        AssertResolvesInYear(notableDateId, year, DateOnly.Parse(expected, CultureInfo.InvariantCulture));

    /// <summary>
    /// Verifies that the Tisha B'Av (9 Av) Hebrew alias resolves to a Gregorian date inside the requested civil year.
    /// </summary>
    [TestMethod]
    public void Resolve_HebrewAvAlias_ResolvesWithinRequestedYear()
    {
        List<NotableDate> matches = ResolveInYear("tisha-bav", 2026);

        Assert.AreEqual(1, matches.Count, "expected exactly one Tisha B'Av in 2026");
        Assert.AreEqual(2026, matches[0].Date.Year, "Tisha B'Av falls inside the requested year");
    }

    /// <summary>
    /// Verifies that the AdarII Hebrew alias produces no occurrence in a Gregorian year whose sweep covers only non-leap
    /// Hebrew years (Gregorian 2025 spans Hebrew 5785 and 5786, both non-leap, so no Adar II exists).
    /// </summary>
    [TestMethod]
    public void Resolve_HebrewAdarIIAlias_InNonLeapYear_ProducesNoOccurrence() =>
        AssertNoOccurrenceInYear("purim-adar-ii", 2025);

    /// <summary>
    /// Verifies that the AdarII Hebrew alias resolves in a Gregorian year whose sweep covers a leap Hebrew year, and
    /// that the projected date lands in the leap Adar II month (index 7 of a thirteen-month year). Gregorian 2027
    /// includes Hebrew 5787, a leap year.
    /// </summary>
    [TestMethod]
    public void Resolve_HebrewAdarIIAlias_InLeapYear_ResolvesToLeapAdarMonth()
    {
        List<NotableDate> matches = ResolveInYear("purim-adar-ii", 2027);

        Assert.AreEqual(1, matches.Count, "expected exactly one Adar II Purim in 2027");

        HebrewCalendar hebrew = new();
        DateTime resolved = matches[0].Date.ToDateTime(TimeOnly.MinValue);
        Assert.AreEqual(7, hebrew.GetMonth(resolved), "Adar II is month 7 of a leap Hebrew year");
        Assert.AreEqual(13, hebrew.GetMonthsInYear(hebrew.GetYear(resolved)), "the Hebrew year is a thirteen-month leap year");
    }

    /// <summary>
    /// Verifies that an Islamic (Hijri) fixed date resolved by a calendar-year sweep produces a non-null occurrence that
    /// falls within the requested Gregorian year, for years where the observance occurs (Eid al-Adha, 10 Dhu
    /// al-Hijjah).
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    [TestMethod]
    [DataRow(2022)]
    [DataRow(2023)]
    [DataRow(2024)]
    public void Resolve_IslamicSweepObservance_ResolvesWithinRequestedYear(int year)
    {
        List<NotableDate> matches = ResolveInYear("eid-al-adha", year);

        Assert.IsTrue(matches.Count >= 1, $"expected Eid al-Adha to occur in {year}");
        Assert.IsTrue(matches.All(m => m.Date.Year == year), $"every Eid al-Adha occurrence falls inside {year}");
    }

    /// <summary>
    /// Verifies that an Umm al-Qura fixed date resolved by a calendar-year sweep projects Islamic New Year onto its
    /// known Gregorian date.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected projected Gregorian date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2022, "2022-07-30")]
    [DataRow(2023, "2023-07-19")]
    [DataRow(2024, "2024-07-07")]
    public void Resolve_UmmAlQuraNewYear_MatchesKnownAnswer(int year, string expected) =>
        AssertResolvesInYear("umm-al-qura-new-year", year, DateOnly.Parse(expected, CultureInfo.InvariantCulture));

    /// <summary>
    /// Resolves every occurrence of a concept within a Gregorian year.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="year">The Gregorian year to scan.</param>
    /// <returns>The matching occurrences.</returns>
    private static List<NotableDate> ResolveInYear(string notableDateId, int year) =>
        s_service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

    /// <summary>
    /// Asserts that the concept resolves to exactly one occurrence on the expected date within the supplied year.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="year">The Gregorian year to scan.</param>
    /// <param name="expected">The expected projected Gregorian date.</param>
    private static void AssertResolvesInYear(string notableDateId, int year, DateOnly expected)
    {
        List<NotableDate> matches = ResolveInYear(notableDateId, year);

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {year}");
        Assert.AreEqual(expected, matches[0].Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Asserts that the concept produces no occurrence anywhere in the supplied Gregorian year.
    /// </summary>
    /// <param name="notableDateId">The concept id to resolve.</param>
    /// <param name="year">The Gregorian year to scan.</param>
    private static void AssertNoOccurrenceInYear(string notableDateId, int year) =>
        Assert.AreEqual(0, ResolveInYear(notableDateId, year).Count, $"expected no '{notableDateId}' in {year}");
}
