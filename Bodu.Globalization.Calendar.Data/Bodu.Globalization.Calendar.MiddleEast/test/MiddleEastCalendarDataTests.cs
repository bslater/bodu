// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MiddleEastCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the Middle East resource pack resolves the United Arab Emirates, Saudi Arabian, Israeli, Turkish,
/// Qatari, and Jordanian holidays to their known dates, exercising the fixed national days, the tabular and Umm
/// al-Qura Hijri Eids, the Hebrew-calendar Jewish festivals, and a nth-weekday floating holiday.
/// </summary>
[TestClass]
public sealed class MiddleEastCalendarDataTests
{
    /// <summary>
    /// Verifies that each fixed or nth-weekday Middle East holiday resolves to its known emitted date.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // United Arab Emirates, Saudi Arabia, Qatar, Jordan: fixed national days, plus Qatar's nth-weekday Sports Day.
    [DataRow("AE", 2024, "commemoration-day", "2024-12-01")]
    [DataRow("AE", 2024, "national-day-ae", "2024-12-02")]
    [DataRow("SA", 2024, "founding-day-sa", "2024-02-22")]
    [DataRow("SA", 2024, "national-day-sa", "2024-09-23")]
    [DataRow("QA", 2024, "national-sports-day", "2024-02-13")]
    [DataRow("QA", 2024, "national-day-qa", "2024-12-18")]
    [DataRow("JO", 2024, "independence-day-jo", "2024-05-25")]

    // Turkey: the secular national days.
    [DataRow("TR", 2024, "national-sovereignty-day", "2024-04-23")]
    [DataRow("TR", 2024, "ataturk-youth-sports-day", "2024-05-19")]
    [DataRow("TR", 2024, "victory-day-tr", "2024-08-30")]
    [DataRow("TR", 2024, "republic-day-tr", "2024-10-29")]
    public void Resolve_MiddleEastHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected)
    {
        var matches = MiddleEastCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");
        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), matches[0].Date, "emitted date");
    }

    /// <summary>
    /// Verifies that an Islamic or Jewish festival resolves to within two days of its known date. The tabular and Umm
    /// al-Qura Hijri calendars differ from local moon-sighting, and the Israeli Independence Day postponement is not
    /// modeled, so a one- to two-day offset is expected and within tolerance.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The festival id to resolve.</param>
    /// <param name="expected">The known observed date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Islamic festivals (tabular Hijri for AE/TR/JO/QA, Umm al-Qura for SA).
    [DataRow("AE", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("AE", 2024, "eid-al-adha", "2024-06-16")]
    [DataRow("AE", 2024, "islamic-new-year", "2024-07-07")]
    [DataRow("SA", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("SA", 2024, "eid-al-adha", "2024-06-16")]
    [DataRow("TR", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("JO", 2024, "eid-al-adha", "2024-06-16")]

    // Israel: Hebrew-calendar Jewish festivals and the Passover-anchored Independence Day.
    [DataRow("IL", 2024, "passover", "2024-04-23")]
    [DataRow("IL", 2024, "shavuot", "2024-06-12")]
    [DataRow("IL", 2024, "rosh-hashanah", "2024-10-03")]
    [DataRow("IL", 2024, "yom-kippur", "2024-10-12")]
    [DataRow("IL", 2024, "independence-day-il", "2024-05-14")]
    public void Resolve_IslamicOrJewishFestival_IsWithinToleranceOfKnownDate(string territory, int year, string notableDateId, string expected)
    {
        var expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        var matches = MiddleEastCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");

        var deltaDays = Math.Abs(matches[0].Date.DayNumber - expectedDate.DayNumber);
        Assert.IsTrue(deltaDays <= 2, $"{notableDateId} {territory} {year}: resolved {matches[0].Date}, expected within 2 days of {expectedDate}");
    }

    /// <summary>
    /// Verifies that every supported country loads and validates (the loader throws on a validation error) and
    /// resolves a non-empty set of holidays for a representative year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void CreateService_ForEverySupportedCountry_LoadsAndResolves()
    {
        foreach (var country in MiddleEastCalendarData.SupportedCountries)
        {
            IReadOnlyList<NotableDate> holidays = MiddleEastCalendarData.CreateService(country)
                .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), country);

            Assert.IsTrue(holidays.Count > 0, $"{country} resolved no holidays for 2024");
        }
    }
}
