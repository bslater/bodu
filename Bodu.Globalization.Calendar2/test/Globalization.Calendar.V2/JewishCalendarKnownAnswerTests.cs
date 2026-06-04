// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JewishCalendarKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the year-by-year known-answer table for the Jewish (Hebrew) observances authored in <c>jewish.xml</c>:
/// four anchors are <see cref="FixedDateStrategy" /> rules expressed in the <see cref="CalendarSystem.Hebrew" />
/// calendar with a calendar-year sweep (Rosh Hashanah, Hanukkah, Purim, Passover) and three are invariant
/// <see cref="OffsetFromRuleStrategy" /> rules (Yom Kippur, Sukkot, Shavuot). Expected dates span Gregorian 2020-2025
/// (Hebrew 5780-5786) and are sourced from Hebcal (<see href="https://www.hebcal.com" />), whose computation matches
/// the base class library's <see cref="System.Globalization.HebrewCalendar" />, ported from the v1
/// <c>global-jewish.xml</c> resource tests.
/// </summary>
[TestClass]
public sealed class JewishCalendarKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the Jewish (Hebrew) fixture.
    /// </summary>
    /// <returns>A service for the Jewish fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("jewish.xml");

    /// <summary>
    /// Resolves the earliest occurrence of a named observance whose anchor falls in the supplied Gregorian year.
    /// </summary>
    /// <param name="service">The service to resolve through.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The occurrences anchored in the requested year, ordered by date.</returns>
    /// <remarks>
    /// <para>
    /// The <c>Date.Year == year</c> filter selects the occurrence whose anchor is in the queried year. Multi-day
    /// holidays whose anchor falls late in the prior Gregorian year (notably Hanukkah, which can begin 25 or 26
    /// December) span the year boundary, so their prior-year occurrence is also returned by a query of the following
    /// Gregorian year; the filter discards it.
    /// </para>
    /// </remarks>
    private static List<NotableDate> ResolveForYear(NotableDateService service, string notableDateId, int year) =>
        service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Where(r => r.NotableDateId == notableDateId && r.Date.Year == year)
            .OrderBy(r => r.Date)
            .ToList();

    /// <summary>
    /// Verifies that all seven Jewish observances declared in <c>jewish.xml</c> resolve for a Gregorian year that
    /// contains them.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_WhenLoadingJewish_ResolvesAllSevenHolidays()
    {
        NotableDateService service = CreateService();

        HashSet<string> ids = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Select(r => r.NotableDateId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(ids.Contains("rosh-hashanah"));
        Assert.IsTrue(ids.Contains("yom-kippur"));
        Assert.IsTrue(ids.Contains("sukkot"));
        Assert.IsTrue(ids.Contains("hanukkah"));
        Assert.IsTrue(ids.Contains("purim"));
        Assert.IsTrue(ids.Contains("passover"));
        Assert.IsTrue(ids.Contains("shavuot"));
    }

    /// <summary>
    /// Verifies that representative Jewish observances resolve to the published Hebcal dates across Gregorian years
    /// 2020-2025. Anchor rules (Rosh Hashanah, Hanukkah, Purim, Passover) flow through <see cref="FixedDateStrategy" />
    /// with the Hebrew calendar; offset rules (Yom Kippur, Sukkot, Shavuot) flow through
    /// <see cref="OffsetFromRuleStrategy" />.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedMonth">The expected Gregorian month.</param>
    /// <param name="expectedDay">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // 2020 (Hebrew years 5780-5781)
    [DataRow(2020, "purim", 3, 10)]
    [DataRow(2020, "passover", 4, 9)]
    [DataRow(2020, "shavuot", 5, 29)]       // Passover + 50
    [DataRow(2020, "rosh-hashanah", 9, 19)]
    [DataRow(2020, "yom-kippur", 9, 28)]    // Rosh Hashanah + 9
    [DataRow(2020, "sukkot", 10, 3)]        // Rosh Hashanah + 14
    [DataRow(2020, "hanukkah", 12, 11)]

    // 2021 (Hebrew years 5781-5782)
    [DataRow(2021, "purim", 2, 26)]
    [DataRow(2021, "passover", 3, 28)]
    [DataRow(2021, "shavuot", 5, 17)]
    [DataRow(2021, "rosh-hashanah", 9, 7)]
    [DataRow(2021, "yom-kippur", 9, 16)]
    [DataRow(2021, "sukkot", 9, 21)]
    [DataRow(2021, "hanukkah", 11, 29)]

    // 2022 (Hebrew years 5782-5783)
    [DataRow(2022, "purim", 3, 17)]
    [DataRow(2022, "passover", 4, 16)]
    [DataRow(2022, "shavuot", 6, 5)]
    [DataRow(2022, "rosh-hashanah", 9, 26)]
    [DataRow(2022, "yom-kippur", 10, 5)]
    [DataRow(2022, "sukkot", 10, 10)]
    [DataRow(2022, "hanukkah", 12, 19)]

    // 2023 (Hebrew years 5783-5784)
    [DataRow(2023, "purim", 3, 7)]
    [DataRow(2023, "passover", 4, 6)]
    [DataRow(2023, "shavuot", 5, 26)]
    [DataRow(2023, "rosh-hashanah", 9, 16)]
    [DataRow(2023, "yom-kippur", 9, 25)]
    [DataRow(2023, "sukkot", 9, 30)]
    [DataRow(2023, "hanukkah", 12, 8)]

    // 2024 (Hebrew years 5784-5785)
    [DataRow(2024, "purim", 3, 24)]
    [DataRow(2024, "passover", 4, 23)]
    [DataRow(2024, "shavuot", 6, 12)]
    [DataRow(2024, "rosh-hashanah", 10, 3)]
    [DataRow(2024, "yom-kippur", 10, 12)]
    [DataRow(2024, "sukkot", 10, 17)]
    [DataRow(2024, "hanukkah", 12, 26)]

    // 2025 (Hebrew years 5785-5786)
    [DataRow(2025, "purim", 3, 14)]
    [DataRow(2025, "passover", 4, 13)]
    [DataRow(2025, "shavuot", 6, 2)]
    [DataRow(2025, "rosh-hashanah", 9, 23)]
    [DataRow(2025, "yom-kippur", 10, 2)]
    [DataRow(2025, "sukkot", 10, 7)]
    [DataRow(2025, "hanukkah", 12, 15)]
    public void Resolve_JewishObservance_MatchesHebcalReferenceDate(int year, string notableDateId, int expectedMonth, int expectedDay)
    {
        List<NotableDate> matches = ResolveForYear(CreateService(), notableDateId, year);

        Assert.IsTrue(matches.Count >= 1, $"{notableDateId} unresolved for Gregorian year {year}");
        Assert.AreEqual(new DateOnly(year, expectedMonth, expectedDay), matches[0].Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that the multi-day Jewish observances preserve their authored duration across the resolution pipeline.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedDuration">The expected authored duration in days.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("rosh-hashanah", 2)]
    [DataRow("sukkot", 7)]
    [DataRow("hanukkah", 8)]
    [DataRow("passover", 7)]
    [DataRow("yom-kippur", 1)]
    [DataRow("purim", 1)]
    [DataRow("shavuot", 1)]
    public void Resolve_JewishObservance_PreservesAuthoredDurationDays(string notableDateId, int expectedDuration)
    {
        List<NotableDate> matches = ResolveForYear(CreateService(), notableDateId, 2024);

        Assert.IsTrue(matches.Count >= 1, $"{notableDateId} unresolved for Gregorian year 2024");
        Assert.AreEqual(expectedDuration, matches[0].DurationDays, notableDateId);
    }
}
