// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsiaPacificCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the migrated Asia-Pacific resource pack resolves Australian, Chinese, Japanese, and New Zealand
/// holidays to their known dates, exercising territory shadowing, conflict-aware substitution, the Chinese lunisolar
/// calendar, the equinox and Qingming solar-term algorithms, and the gazetted Matariki schedule.
/// </summary>
[TestClass]
public sealed class AsiaPacificCalendarDataTests
    : CalendarDataTestsBase
{
    /// <inheritdoc />
    protected override IReadOnlyList<string> SupportedCountries => AsiaPacificCalendarData.SupportedCountries;

    /// <inheritdoc />
    protected override INotableDateService CreateService(string territory) =>
        AsiaPacificCalendarData.CreateService(territory);

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

    // Employment New Zealand published national dates for 2026 and 2027 (delivered 3 Aug 2026):
    // 2027 exercises the full Mondayisation surface - the New Year pair, Waitangi Day, ANZAC Day,
    // and the conflict-aware Christmas/Boxing chain where Boxing Day's in-lieu day (Tue 28 Dec)
    // steps past Christmas Day's (Mon 27 Dec).
    [DataRow("NZ", 2026, "new-years-day", "2026-01-01", false)]
    [DataRow("NZ", 2026, "day-after-new-years-day", "2026-01-02", false)]
    [DataRow("NZ", 2026, "waitangi-day", "2026-02-06", false)]
    [DataRow("NZ", 2026, "good-friday", "2026-04-03", false)]
    [DataRow("NZ", 2026, "easter-monday", "2026-04-06", false)]
    [DataRow("NZ", 2026, "anzac-day", "2026-04-27", true)]
    [DataRow("NZ", 2026, "kings-birthday", "2026-06-01", false)]
    [DataRow("NZ", 2026, "matariki", "2026-07-10", false)]
    [DataRow("NZ", 2026, "labour-day", "2026-10-26", false)]
    [DataRow("NZ", 2026, "christmas-day", "2026-12-25", false)]
    [DataRow("NZ", 2026, "boxing-day", "2026-12-28", true)]
    [DataRow("NZ", 2027, "new-years-day", "2027-01-01", false)]
    [DataRow("NZ", 2027, "day-after-new-years-day", "2027-01-04", true)]
    [DataRow("NZ", 2027, "waitangi-day", "2027-02-08", true)]
    [DataRow("NZ", 2027, "good-friday", "2027-03-26", false)]
    [DataRow("NZ", 2027, "easter-monday", "2027-03-29", false)]
    [DataRow("NZ", 2027, "anzac-day", "2027-04-26", true)]
    [DataRow("NZ", 2027, "kings-birthday", "2027-06-07", false)]
    [DataRow("NZ", 2027, "matariki", "2027-06-25", false)]
    [DataRow("NZ", 2027, "labour-day", "2027-10-25", false)]
    [DataRow("NZ", 2027, "christmas-day", "2027-12-27", true)]
    [DataRow("NZ", 2027, "boxing-day", "2027-12-28", true)]

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

    // Indonesia, Thailand, Philippines: fixed national days, Songkran, and the Catholic Holy Week / nth-weekday days.
    [DataRow("ID", 2024, "independence-day-id", "2024-08-17", false)]
    [DataRow("ID", 2024, "pancasila-day", "2024-06-01", false)]
    [DataRow("TH", 2024, "chakri-day", "2024-04-06", false)]
    [DataRow("TH", 2024, "songkran", "2024-04-13", false)]
    [DataRow("TH", 2024, "chulalongkorn-day", "2024-10-23", false)]
    [DataRow("PH", 2024, "independence-day-ph", "2024-06-12", false)]
    [DataRow("PH", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("PH", 2024, "national-heroes-day", "2024-08-26", false)]
    [DataRow("PH", 2024, "rizal-day", "2024-12-30", false)]

    // Vietnam, Hong Kong, Taiwan: fixed national days, Easter offsets, and the Qingming solar term (Apr 4 in 2024).
    [DataRow("VN", 2024, "reunification-day", "2024-04-30", false)]
    [DataRow("VN", 2024, "national-day-vn", "2024-09-02", false)]
    [DataRow("HK", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("HK", 2024, "ching-ming-festival", "2024-04-04", false)]
    [DataRow("HK", 2024, "national-day-hk", "2024-10-01", false)]
    [DataRow("TW", 2024, "peace-memorial-day", "2024-02-28", false)]
    [DataRow("TW", 2024, "tomb-sweeping-day", "2024-04-04", false)]
    [DataRow("TW", 2024, "national-day-tw", "2024-10-10", false)]
    public void Resolve_AsiaPacificHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that a narrower subdivision Anzac Day rule shadows the national rule for Western Australia while other
    /// states fall back to the national no-substitute rule.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAnzacDayInWesternAustralia_ShadowsNationalRule()
    {
        NotableDate wa = ResolveSingle("AU-WA", 2020, "anzac-day");

        Assert.AreEqual(
            (new DateOnly(2020, 4, 27), true, "wa"),
            (wa.Date, wa.IsObserved, wa.RuleId));
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
        var anzac = ResolveYear("AU-NSW", year)
            .Where(r => r.NotableDateId == "anzac-day")
            .OrderBy(r => r.Date)
            .ToList();

        Assert.HasCount(2, anzac, "trial year emits Anzac Day plus an additional Monday");

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
        NotableDate anzac = ResolveSingle("AU-NSW", year, "anzac-day");

        Assert.AreEqual(new DateOnly(year, 4, 25), anzac.Date, "the national rule keeps 25 April");
        Assert.IsFalse(anzac.IsObserved, "the national rule has no substitute");
        Assert.AreEqual("au", anzac.RuleId, "AU-NSW falls back to the national rule");
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

    // Indonesia and Thailand: tabular-Hijri Eids and the computed Buddhist festivals.
    [DataRow("ID", 2024, "eid-al-fitr", "2024-04-10")]
    [DataRow("ID", 2024, "eid-al-adha", "2024-06-17")]
    [DataRow("ID", 2024, "vesak", "2024-05-23")]
    [DataRow("TH", 2024, "vesak", "2024-05-22")]

    // Philippines: Chinese New Year and the tabular-Hijri Eid.
    [DataRow("PH", 2024, "chinese-new-year", "2024-02-10")]
    [DataRow("PH", 2024, "eid-al-fitr", "2024-04-10")]

    // Vietnam, Hong Kong, Taiwan: Chinese-lunisolar festivals (Tết / Lunar New Year, Dragon Boat / Tuen Ng,
    // Mid-Autumn, Buddha's Birthday, Chung Yeung, Hùng Kings).
    [DataRow("VN", 2024, "lunar-new-year", "2024-02-10")]
    [DataRow("VN", 2024, "hung-kings-festival", "2024-04-18")]
    [DataRow("VN", 2024, "mid-autumn-festival", "2024-09-17")]
    [DataRow("HK", 2024, "lunar-new-year", "2024-02-10")]
    [DataRow("HK", 2024, "buddhas-birthday", "2024-05-15")]
    [DataRow("HK", 2024, "tuen-ng-festival", "2024-06-10")]
    [DataRow("HK", 2024, "chung-yeung-festival", "2024-10-11")]
    [DataRow("TW", 2024, "lunar-new-year", "2024-02-10")]
    [DataRow("TW", 2024, "dragon-boat-festival", "2024-06-10")]
    [DataRow("TW", 2024, "mid-autumn-festival", "2024-09-17")]
    public void Resolve_LunarOrIslamicFestival_IsWithinToleranceOfKnownDate(string territory, int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        AssertWithinDays(match.Date, DateOnly.Parse(expected, CultureInfo.InvariantCulture), 2, $"{notableDateId} {territory} {year}");
    }

    /// <summary>
    /// Verifies that the Chinese seven-day Lunar New Year span carries its duration and is returned by a single-day
    /// query for a day inside the span.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenQueryFallsInsideLunarNewYearSpan_ReturnsTheMultiDayOccurrence()
    {
        // Lunar New Year 2024 begins 10 February; query a working day partway through the seven-day span.
        var matches = AsiaPacificCalendarData.CreateService("CN")
            .Resolve(new DateOnly(2024, 2, 13), "CN")
            .Where(r => r.NotableDateId == "lunar-new-year")
            .ToList();

        Assert.HasCount(1, matches, "a day inside the span returns the occurrence");
        Assert.AreEqual(
            (new DateOnly(2024, 2, 10), 7, new DateOnly(2024, 2, 16)),
            (matches[0].Date, matches[0].DurationDays, matches[0].EndDate));
    }
}
