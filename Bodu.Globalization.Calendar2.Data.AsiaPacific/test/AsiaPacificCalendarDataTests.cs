// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsiaPacificCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.V2;

namespace Bodu.Globalization.Calendar.V2.Data;

/// <summary>
/// Verifies that the migrated Asia-Pacific resource pack resolves Australian, Chinese, Japanese, and New Zealand
/// holidays to their known dates, exercising territory shadowing, conflict-aware substitution, the Chinese lunisolar
/// calendar, the equinox and Qingming solar-term algorithms, and the gazetted Matariki schedule.
/// </summary>
[TestClass]
public sealed class AsiaPacificCalendarDataTests
{
    /// <summary>
    /// Verifies that each Asia-Pacific holiday resolves to its known emitted date and observed flag.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Australia: weekend roll, subdivision shadowing, conflict-aware substitution.
    [DataRow("AU", 2020, "australia-day", "2020-01-27", true)]
    [DataRow("AU", 2026, "anzac-day", "2026-04-25", false)]
    [DataRow("AU-WA", 2020, "anzac-day", "2020-04-27", true)]
    [DataRow("AU-VIC", 2026, "labour-day", "2026-03-09", false)]
    [DataRow("AU-WA", 2026, "kings-birthday", "2026-09-28", false)]
    [DataRow("AU", 2021, "christmas-day", "2021-12-27", true)]
    [DataRow("AU", 2021, "boxing-day", "2021-12-28", true)]

    // Australia: holidays restored from the v1 catalogue during the v2 migration gap-fill.
    [DataRow("AU", 2024, "easter-saturday", "2024-03-30", false)]
    [DataRow("AU", 2026, "harmony-day", "2026-03-21", false)]
    [DataRow("AU", 2026, "mabo-day", "2026-06-03", false)]
    [DataRow("AU", 2026, "fathers-day", "2026-09-06", false)]
    [DataRow("AU-SA", 2026, "kings-birthday", "2026-06-08", false)]
    [DataRow("AU-ACT", 2026, "kings-birthday", "2026-06-08", false)]
    [DataRow("AU-ACT", 2026, "canberra-day", "2026-03-09", false)]
    [DataRow("AU-SA", 2026, "adelaide-cup-day", "2026-03-09", false)]
    [DataRow("AU-WA", 2026, "western-australia-day", "2026-06-01", false)]
    [DataRow("AU-NT", 2026, "may-day", "2026-05-04", false)]
    [DataRow("AU-NT", 2026, "picnic-day", "2026-08-03", false)]
    [DataRow("AU-TAS", 2026, "eight-hours-day", "2026-03-09", false)]
    [DataRow("AU-TAS", 2026, "recreation-day", "2026-11-02", false)]
    [DataRow("AU-QLD", 2026, "royal-queensland-show", "2026-08-12", false)]
    [DataRow("AU-NSW", 2026, "bank-holiday", "2026-08-03", false)]
    [DataRow("AU-VIC", 2026, "afl-grand-final-friday", "2026-09-25", false)]

    // Japan: astronomical equinoxes (Japan Standard Time) and fixed/nth-weekday holidays.
    [DataRow("JP", 2024, "coming-of-age-day", "2024-01-08", false)]
    [DataRow("JP", 2024, "vernal-equinox-day", "2024-03-20", false)]
    [DataRow("JP", 2024, "autumnal-equinox-day", "2024-09-22", false)]
    [DataRow("JP", 2024, "culture-day", "2024-11-03", false)]

    // Japan: multi-day period markers restored by the entry-level migration audit (durationDays spans).
    [DataRow("JP", 2024, "golden-week", "2024-04-29", false)]
    [DataRow("JP", 2024, "obon", "2024-08-13", false)]

    // China: Chinese lunisolar fixed dates, an offset-from-rule, and the Qingming solar term.
    [DataRow("CN", 2024, "lunar-new-year", "2024-02-10", false)]
    [DataRow("CN", 2024, "lantern-festival", "2024-02-24", false)]
    [DataRow("CN", 2024, "qingming-festival", "2024-04-04", false)]
    [DataRow("CN", 2024, "dragon-boat-festival", "2024-06-10", false)]
    [DataRow("CN", 2024, "mid-autumn-festival", "2024-09-17", false)]

    // New Zealand: gazetted Matariki, Waitangi Day, and conflict-aware Christmas/Boxing substitution.
    [DataRow("NZ", 2024, "waitangi-day", "2024-02-06", false)]
    [DataRow("NZ", 2024, "matariki", "2024-06-28", false)]
    [DataRow("NZ", 2024, "labour-day", "2024-10-28", false)]
    [DataRow("NZ", 2021, "christmas-day", "2021-12-27", true)]
    [DataRow("NZ", 2021, "boxing-day", "2021-12-28", true)]

    // South Korea: Chinese lunisolar Seollal and Chuseok, plus fixed nationals.
    [DataRow("KR", 2024, "seollal", "2024-02-10", false)]
    [DataRow("KR", 2024, "chuseok", "2024-09-17", false)]
    [DataRow("KR", 2024, "hangul-day", "2024-10-09", false)]

    // Singapore: Chinese New Year, Easter-derived Good Friday, and fixed nationals.
    [DataRow("SG", 2024, "chinese-new-year", "2024-02-10", false)]
    [DataRow("SG", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("SG", 2024, "national-day", "2024-08-09", false)]

    // India: Gregorian nationals, the fixed solar Makar Sankranti, and Easter-derived Good Friday.
    [DataRow("IN", 2024, "republic-day", "2024-01-26", false)]
    [DataRow("IN", 2024, "makar-sankranti", "2024-01-14", false)]
    [DataRow("IN", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("IN", 2024, "gandhi-jayanti", "2024-10-02", false)]
    public void Resolve_AsiaPacificHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        List<NotableDate> matches = AsiaPacificCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");
        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), matches[0].Date, "emitted date");
        Assert.AreEqual(isObserved, matches[0].IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that a narrower subdivision Anzac Day rule shadows the national rule for Western Australia while other
    /// states fall back to the national no-substitute rule.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnzacDayInWesternAustralia_ShadowsNationalRule()
    {
        NotableDate wa = AsiaPacificCalendarData.CreateService("AU-WA")
            .Resolve(new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31)), "AU-WA")
            .Single(r => r.NotableDateId == "anzac-day");

        Assert.AreEqual(new DateOnly(2020, 4, 27), wa.Date);
        Assert.IsTrue(wa.IsObserved);
        Assert.AreEqual("wa", wa.RuleId);
    }

    /// <summary>
    /// Verifies the New South Wales Anzac Day weekend-substitute trial (2026-2027 only): when 25 April falls on a
    /// weekend the NSW rule emits both the actual 25 April Anzac Day and an additional observed Monday public
    /// holiday, shadowing the national rule for AU-NSW in the trial years.
    /// </summary>
    /// <param name="year">The trial Gregorian year.</param>
    /// <param name="additionalMonday">The expected additional Monday public holiday in ISO format.</param>
    [TestMethod]
    [DataRow(2026, "2026-04-27")]
    [DataRow(2027, "2027-04-26")]
    public void Resolve_WhenAnzacDayInNewSouthWalesTrialYear_EmitsActualAndAdditionalMonday(int year, string additionalMonday)
    {
        List<NotableDate> anzac = AsiaPacificCalendarData.CreateService("AU-NSW")
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "AU-NSW")
            .Where(r => r.NotableDateId == "anzac-day")
            .OrderBy(r => r.Date)
            .ToList();

        Assert.AreEqual(2, anzac.Count, "trial year emits Anzac Day plus an additional Monday");

        Assert.AreEqual(new DateOnly(year, 4, 25), anzac[0].Date, "Anzac Day stays on 25 April");
        Assert.IsFalse(anzac[0].IsObserved, "the 25 April occurrence is the actual date");

        Assert.AreEqual(DateOnly.Parse(additionalMonday, CultureInfo.InvariantCulture), anzac[1].Date, "additional Monday");
        Assert.IsTrue(anzac[1].IsObserved, "the additional Monday is an observed substitute");
        Assert.AreEqual("nsw", anzac[1].RuleId, "the NSW trial rule shadows the national rule");
    }

    /// <summary>
    /// Verifies that outside the 2026-2027 trial window New South Wales falls back to the national Anzac Day rule:
    /// a single 25 April occurrence with no additional Monday, even when 25 April lands on a weekend.
    /// </summary>
    /// <param name="year">A Gregorian year outside the trial window.</param>
    [TestMethod]
    [DataRow(2025)]
    [DataRow(2028)]
    [DataRow(2032)]
    public void Resolve_WhenAnzacDayInNewSouthWalesOutsideTrial_FallsBackToNationalRule(int year)
    {
        List<NotableDate> anzac = AsiaPacificCalendarData.CreateService("AU-NSW")
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "AU-NSW")
            .Where(r => r.NotableDateId == "anzac-day")
            .ToList();

        Assert.AreEqual(1, anzac.Count, "outside the trial NSW has a single Anzac Day");
        Assert.AreEqual(new DateOnly(year, 4, 25), anzac[0].Date, "the national rule keeps 25 April");
        Assert.IsFalse(anzac[0].IsObserved, "the national rule has no substitute");
        Assert.AreEqual("au", anzac[0].RuleId, "AU-NSW falls back to the national rule");
    }

    /// <summary>
    /// Verifies that an Islamic or lunar-algorithm festival resolves to within two days of its gazetted date. The
    /// tabular Hijri calendar and the astronomical lunar series differ from the locally proclaimed observance, so a
    /// one- to two-day offset is expected and within tolerance.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The festival id to resolve.</param>
    /// <param name="expected">The gazetted observed date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("MY", 2024, "hari-raya-aidilfitri", "2024-04-10")]
    [DataRow("MY", 2024, "hari-raya-aidiladha", "2024-06-17")]
    [DataRow("KR", 2024, "buddhas-birthday", "2024-05-15")]
    [DataRow("SG", 2024, "vesak-day", "2024-05-22")]
    [DataRow("SG", 2024, "deepavali", "2024-10-31")]
    [DataRow("IN", 2024, "diwali", "2024-11-01")]
    [DataRow("IN", 2024, "holi", "2024-03-25")]
    [DataRow("IN", 2024, "eid-al-fitr", "2024-04-11")]
    [DataRow("IN", 2024, "maha-shivaratri", "2024-03-08")]
    [DataRow("IN", 2024, "ram-navami", "2024-04-17")]
    [DataRow("IN", 2024, "janmashtami", "2024-08-26")]
    [DataRow("IN", 2024, "ganesh-chaturthi", "2024-09-07")]
    [DataRow("IN", 2024, "dussehra", "2024-10-12")]
    [DataRow("IN", 2024, "karva-chauth", "2024-10-20")]
    public void Resolve_LunarOrIslamicFestival_IsWithinToleranceOfKnownDate(string territory, int year, string notableDateId, string expected)
    {
        DateOnly expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        List<NotableDate> matches = AsiaPacificCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");

        int deltaDays = Math.Abs(matches[0].Date.DayNumber - expectedDate.DayNumber);
        Assert.IsTrue(deltaDays <= 2, $"{notableDateId} {territory} {year}: resolved {matches[0].Date}, expected within 2 days of {expectedDate}");
    }

    /// <summary>
    /// Verifies that the Chinese seven-day Lunar New Year span carries its duration and is returned by a single-day
    /// query for a day inside the span.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenQueryFallsInsideLunarNewYearSpan_ReturnsTheMultiDayOccurrence()
    {
        // Lunar New Year 2024 begins 10 February; query a working day partway through the seven-day span.
        List<NotableDate> matches = AsiaPacificCalendarData.CreateService("CN")
            .Resolve(new DateOnly(2024, 2, 13), "CN")
            .Where(r => r.NotableDateId == "lunar-new-year")
            .ToList();

        Assert.AreEqual(1, matches.Count, "a day inside the span returns the occurrence");
        Assert.AreEqual(new DateOnly(2024, 2, 10), matches[0].Date, "span start");
        Assert.AreEqual(7, matches[0].DurationDays, "duration");
        Assert.AreEqual(new DateOnly(2024, 2, 16), matches[0].EndDate, "span end");
    }
}
