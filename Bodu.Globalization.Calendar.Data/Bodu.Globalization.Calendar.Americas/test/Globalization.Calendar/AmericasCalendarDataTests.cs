// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmericasCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the migrated Americas resource pack resolves United States and Canadian holidays to their known
/// dates, exercising weekend in-lieu observation, Easter-derived offsets, per-province subdivision rules, and the
/// conflict-aware Christmas/Boxing Day substitution.
/// </summary>
[TestClass]
public sealed class AmericasCalendarDataTests
    : CalendarDataTestsBase
{
    /// <inheritdoc />
    protected override IReadOnlyList<string> SupportedCountries => AmericasCalendarData.SupportedCountries;

    /// <inheritdoc />
    protected override INotableDateService CreateService(string territory) =>
        AmericasCalendarData.CreateService(territory);

    /// <summary>
    /// Verifies that each United States holiday resolves to its known emitted date and observed flag.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2026, "new-years-day", "2026-01-01", false)]
    [DataRow(2026, "mlk-day", "2026-01-19", false)]
    [DataRow(2024, "good-friday", "2024-03-29", false)]
    [DataRow(2024, "easter-sunday", "2024-03-31", false)]
    [DataRow(2024, "mothers-day", "2024-05-12", false)]
    [DataRow(2026, "memorial-day", "2026-05-25", false)]
    [DataRow(2021, "independence-day", "2021-07-05", true)]
    [DataRow(2026, "labor-day", "2026-09-07", false)]
    [DataRow(2024, "election-day", "2024-11-05", false)]
    [DataRow(2024, "black-friday", "2024-11-29", false)]
    [DataRow(2021, "christmas-day", "2021-12-24", true)]
    [DataRow(2022, "christmas-day", "2022-12-26", true)]
    public void Resolve_UnitedStatesHoliday_MatchesKnownAnswer(int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = ResolveSingle("US", year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that each Canadian holiday resolves to its known emitted date and observed flag, including provincial
    /// rules and the conflict-aware Christmas/Boxing Day substitution.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("CA", 2024, "new-years-day", "2024-01-01", false)]
    [DataRow("CA", 2024, "victoria-day", "2024-05-20", false)]
    [DataRow("CA", 2024, "canada-day", "2024-07-01", false)]
    [DataRow("CA", 2023, "canada-day", "2023-07-03", true)]
    [DataRow("CA", 2024, "thanksgiving", "2024-10-14", false)]
    [DataRow("CA", 2024, "easter-monday", "2024-04-01", false)]
    [DataRow("CA-ON", 2024, "family-day", "2024-02-19", false)]
    [DataRow("CA-QC", 2024, "fete-nationale-quebec", "2024-06-24", false)]
    [DataRow("CA", 2021, "christmas-day", "2021-12-27", true)]
    [DataRow("CA", 2021, "boxing-day", "2021-12-28", true)]

    // Restored by the entry-level migration audit: Christmas Eve (24 December), a working cultural observance.
    [DataRow("CA", 2024, "christmas-eve", "2024-12-24", false)]
    public void Resolve_CanadianHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that each fixed-date state and District of Columbia statutory holiday resolves for its subdivision
    /// to the known emitted date.
    /// </summary>
    /// <param name="territory">The requested subdivision code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("US-UT", 2024, "pioneer-day", "2024-07-24")]
    [DataRow("US-DC", 2024, "emancipation-day-dc", "2024-04-16")]
    [DataRow("US-DC", 2025, "inauguration-day", "2025-01-20")]
    [DataRow("US-DC", 2021, "inauguration-day", "2021-01-20")]
    [DataRow("US-TX", 2024, "texas-independence-day", "2024-03-02")]
    [DataRow("US-TX", 2024, "san-jacinto-day", "2024-04-21")]
    [DataRow("US-TX", 2024, "day-after-thanksgiving", "2024-11-29")]
    [DataRow("US-AK", 2024, "alaska-day", "2024-10-18")]
    [DataRow("US-HI", 2024, "kamehameha-day", "2024-06-11")]
    [DataRow("US-MO", 2024, "truman-day", "2024-05-08")]
    [DataRow("US-WV", 2024, "west-virginia-day", "2024-06-20")]
    [DataRow("US-IL", 2024, "lincolns-birthday", "2024-02-12")]
    [DataRow("US-VT", 2024, "bennington-battle-day", "2024-08-16")]
    [DataRow("US-TX", 2024, "lbj-day", "2024-08-27")]
    public void Resolve_UnitedStatesStateHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
    }

    /// <summary>
    /// Verifies that holidays whose date floats from year to year resolve to independently-known reference dates
    /// across both historical and future years — the Easter-derived offsets, the nth-weekday-in-month rules, and the
    /// nearest-weekday rules — so the moving feasts are pinned, not merely structurally validated.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Easter-derived (Algorithm + OffsetFromRule), historical and future.
    [DataRow("US", 2008, "easter-sunday", "2008-03-23")]
    [DataRow("US", 2030, "easter-sunday", "2030-04-21")]
    [DataRow("US", 2038, "easter-sunday", "2038-04-25")]
    [DataRow("US", 2008, "good-friday", "2008-03-21")]
    [DataRow("US", 2021, "good-friday", "2021-04-02")]
    [DataRow("US-LA", 2021, "mardi-gras", "2021-02-16")]
    [DataRow("US-LA", 2024, "mardi-gras", "2024-02-13")]

    // Nth-weekday-in-month, historical and future.
    [DataRow("US", 2024, "mlk-day", "2024-01-15")]
    [DataRow("US", 2031, "mlk-day", "2031-01-20")]
    [DataRow("US", 2031, "memorial-day", "2031-05-26")]
    [DataRow("US", 2024, "thanksgiving", "2024-11-28")]
    [DataRow("US", 2030, "thanksgiving", "2030-11-28")]
    [DataRow("CA", 2031, "thanksgiving", "2031-10-13")]
    [DataRow("US-MA", 2024, "patriots-day", "2024-04-15")]
    [DataRow("US-MA", 2031, "patriots-day", "2031-04-21")]
    [DataRow("US-HI", 2024, "statehood-day-hi", "2024-08-16")]
    [DataRow("US-NV", 2024, "nevada-day", "2024-10-25")]
    [DataRow("US-AK", 2024, "sewards-day", "2024-03-25")]
    [DataRow("US", 2024, "arbor-day", "2024-04-26")]
    [DataRow("US-CO", 2024, "cabrini-day", "2024-10-07")]
    [DataRow("US-AR", 2024, "daisy-bates-day", "2024-02-19")]

    // British Columbia Family Day moved from the second to the third Monday in February in 2019.
    [DataRow("CA-BC", 2016, "family-day", "2016-02-08")]
    [DataRow("CA-BC", 2019, "family-day", "2019-02-18")]

    // Nearest-weekday (WeekdayNearDate), Canada.
    [DataRow("CA", 2031, "victoria-day", "2031-05-19")]
    [DataRow("CA-QC", 2024, "national-patriots-day", "2024-05-20")]
    [DataRow("CA-NL", 2024, "nl-discovery-day", "2024-06-24")]
    [DataRow("CA-NL", 2025, "nl-discovery-day", "2025-06-23")]
    [DataRow("CA-NL", 2024, "nl-st-georges-day", "2024-04-22")]
    [DataRow("CA-NL", 2024, "nl-st-patricks-day", "2024-03-18")]
    public void Resolve_FloatingHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
    }

    /// <summary>
    /// Verifies that the shared observances consolidated in the americas-common hub resolve for both the United
    /// States and Canada from their single hub/common-library definition.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("US", 2024, "groundhog-day", "2024-02-02")]
    [DataRow("CA", 2024, "groundhog-day", "2024-02-02")]
    [DataRow("US", 2024, "st-patricks-day", "2024-03-17")]
    [DataRow("US", 2024, "cinco-de-mayo", "2024-05-05")]
    [DataRow("US", 2024, "earth-day", "2024-04-22")]
    [DataRow("US", 2024, "valentines-day", "2024-02-14")]
    [DataRow("CA", 2024, "valentines-day", "2024-02-14")]
    [DataRow("CA", 2024, "international-womens-day", "2024-03-08")]
    public void Resolve_HubSharedObservance_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
    }

    /// <summary>
    /// Verifies that National Indigenous Peoples Day resolves as a non-working statutory holiday in the Northwest
    /// Territories (the territorial rule) while remaining a working observance nationally.
    /// </summary>
    [TestMethod]
    public void Resolve_NorthwestTerritoriesIndigenousPeoplesDay_IsStatutoryHoliday()
    {
        var matches = AmericasCalendarData.CreateService("CA-NT")
            .Resolve(new DateRange(new DateOnly(2024, 6, 21), new DateOnly(2024, 6, 21)), "CA-NT")
            .Where(r => r.NotableDateId == "national-indigenous-peoples-day")
            .ToList();

        Assert.Contains(r => r.RuleId == "nt-yt" && r.IsNonWorkingDay, matches, "expected a non-working territorial rule");
    }

    /// <summary>
    /// Verifies that Good Friday resolves as a non-working statutory holiday in a state that observes it (the state
    /// override rule), while remaining a working religious observance nationally.
    /// </summary>
    [TestMethod]
    public void Resolve_StateGoodFriday_IsStatutoryHoliday()
    {
        var matches = AmericasCalendarData.CreateService("US-LA")
            .Resolve(new DateRange(new DateOnly(2024, 3, 29), new DateOnly(2024, 3, 29)), "US-LA")
            .Where(r => r.NotableDateId == "good-friday")
            .ToList();

        Assert.Contains(r => r.RuleId == "state-statutory" && r.IsNonWorkingDay, matches, "expected a non-working state rule");
    }

    /// <summary>
    /// Verifies that a holiday does not resolve in a year outside its applicability — before its first applicable
    /// year, or (for the quadrennial Inauguration Day) in a non-inauguration year.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    [TestMethod]
    [DataRow("US", 2020, "juneteenth")]
    [DataRow("CA", 2020, "truth-and-reconciliation-day")]
    [DataRow("CA-ON", 2007, "family-day")]
    [DataRow("US-DC", 2004, "emancipation-day-dc")]
    [DataRow("US-SD", 1989, "native-americans-day-sd")]
    [DataRow("US-DC", 2023, "inauguration-day")]
    [DataRow("US-DC", 2024, "inauguration-day")]
    [DataRow("BR", 2023, "black-awareness-day")]
    [DataRow("PE", 2023, "battle-of-junin")]
    public void Resolve_WhenBeforeFirstYear_ReturnsNoResult(string territory, int year, string notableDateId)
    {
        int count = AmericasCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Count(r => r.NotableDateId == notableDateId);

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that Mexican holidays whose date floats from year to year resolve to independently-known published
    /// dates — the three 2006-reform Monday holidays and the Holy Week feasts — so the moving holidays are pinned to
    /// confirmed calendar dates rather than merely structurally validated.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // 2006-reform Monday holidays (DayOfWeekInMonth), confirmed against the published official calendar.
    [DataRow(2024, "constitution-day", "2024-02-05")]
    [DataRow(2025, "constitution-day", "2025-02-03")]
    [DataRow(2024, "benito-juarez-day", "2024-03-18")]
    [DataRow(2025, "benito-juarez-day", "2025-03-17")]
    [DataRow(2024, "revolution-day", "2024-11-18")]
    [DataRow(2025, "revolution-day", "2025-11-17")]

    // Holy Week and Father's Day (Easter offset / nth-weekday), confirmed dates.
    [DataRow(2024, "maundy-thursday", "2024-03-28")]
    [DataRow(2024, "good-friday", "2024-03-29")]
    [DataRow(2025, "good-friday", "2025-04-18")]
    [DataRow(2024, "fathers-day", "2024-06-16")]
    public void Resolve_MexicanFloatingHoliday_MatchesKnownAnswer(int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle("MX", year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
    }

    /// <summary>
    /// Verifies that each fixed-date Mexican national holiday resolves for a representative year to its known date.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, "new-years-day", "2024-01-01")]
    [DataRow(2024, "workers-day", "2024-05-01")]
    [DataRow(2024, "mothers-day", "2024-05-10")]
    [DataRow(2024, "independence-day-mx", "2024-09-16")]
    [DataRow(2024, "day-of-the-dead", "2024-11-02")]
    [DataRow(2024, "guadalupe-day", "2024-12-12")]
    [DataRow(2024, "christmas-day", "2024-12-25")]
    public void Resolve_MexicanFixedHoliday_MatchesKnownAnswer(int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle("MX", year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
    }

    /// <summary>
    /// Verifies that Latin American holidays whose date floats from year to year resolve to independently-known
    /// published dates — the Easter-derived Carnival, Corpus Christi and Holy Week feasts, the nth-weekday family
    /// observances, the Argentine movable holidays, and the Colombian Emiliani Monday holidays — so the moving
    /// holidays are pinned to confirmed calendar dates rather than merely structurally validated.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Brazil: Carnival and Corpus Christi (Easter offsets) and the family observances on Brazilian dates.
    [DataRow("BR", 2024, "good-friday", "2024-03-29")]
    [DataRow("BR", 2024, "carnival-monday", "2024-02-12")]
    [DataRow("BR", 2024, "carnival-tuesday", "2024-02-13")]
    [DataRow("BR", 2025, "carnival-tuesday", "2025-03-04")]
    [DataRow("BR", 2024, "corpus-christi", "2024-05-30")]
    [DataRow("BR", 2025, "corpus-christi", "2025-06-19")]
    [DataRow("BR", 2024, "mothers-day", "2024-05-12")]
    [DataRow("BR", 2024, "fathers-day", "2024-08-11")]

    // Argentina: Carnival, the third-Sunday family observances, and the movable holidays (Ley 27.399).
    [DataRow("AR", 2024, "good-friday", "2024-03-29")]
    [DataRow("AR", 2024, "carnival-monday", "2024-02-12")]
    [DataRow("AR", 2024, "carnival-tuesday", "2024-02-13")]
    [DataRow("AR", 2024, "mothers-day", "2024-10-20")]
    [DataRow("AR", 2024, "fathers-day", "2024-06-16")]
    [DataRow("AR", 2024, "guemes-day", "2024-06-17")]
    [DataRow("AR", 2022, "diversity-day-ar", "2022-10-10")]

    // Chile: Good Friday and Holy Saturday (Easter offsets) and Mother's Day.
    [DataRow("CL", 2024, "good-friday", "2024-03-29")]
    [DataRow("CL", 2024, "holy-saturday", "2024-03-30")]
    [DataRow("CL", 2024, "mothers-day", "2024-05-12")]

    // Colombia: Holy Week and the Emiliani Monday holidays (fixed-date moved to the following Monday) and the
    // Easter-derived Monday holidays computed directly with their offset.
    [DataRow("CO", 2024, "maundy-thursday", "2024-03-28")]
    [DataRow("CO", 2024, "saint-joseph", "2024-03-25")]
    [DataRow("CO", 2024, "ascension-day-co", "2024-05-13")]
    [DataRow("CO", 2025, "ascension-day-co", "2025-06-02")]
    [DataRow("CO", 2024, "corpus-christi-co", "2024-06-03")]
    [DataRow("CO", 2024, "sacred-heart-co", "2024-06-10")]
    [DataRow("CO", 2024, "saints-peter-and-paul", "2024-07-01")]
    [DataRow("CO", 2024, "assumption-of-mary", "2024-08-19")]
    [DataRow("CO", 2024, "dia-de-la-raza-co", "2024-10-14")]
    [DataRow("CO", 2024, "all-saints-day", "2024-11-04")]

    // Peru: Holy Week and Father's Day.
    [DataRow("PE", 2024, "maundy-thursday", "2024-03-28")]
    [DataRow("PE", 2024, "good-friday", "2024-03-29")]
    [DataRow("PE", 2024, "fathers-day", "2024-06-16")]
    public void Resolve_LatinAmericanFloatingHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
    }

    /// <summary>
    /// Verifies that representative fixed-date Latin American national holidays resolve to their known dates.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("BR", 2024, "tiradentes", "2024-04-21")]
    [DataRow("BR", 2024, "independence-day-br", "2024-09-07")]
    [DataRow("BR", 2024, "nossa-senhora-aparecida", "2024-10-12")]
    [DataRow("BR", 2024, "proclamation-of-the-republic", "2024-11-15")]
    [DataRow("BR", 2024, "black-awareness-day", "2024-11-20")]
    [DataRow("AR", 2024, "may-revolution-day", "2024-05-25")]
    [DataRow("AR", 2024, "independence-day-ar", "2024-07-09")]
    [DataRow("AR", 2024, "flag-day-ar", "2024-06-20")]
    [DataRow("AR", 2024, "malvinas-day", "2024-04-02")]
    [DataRow("CL", 2024, "navy-day", "2024-05-21")]
    [DataRow("CL", 2024, "independence-day-cl", "2024-09-18")]
    [DataRow("CL", 2024, "assumption-of-mary", "2024-08-15")]
    [DataRow("CL", 2024, "immaculate-conception", "2024-12-08")]
    [DataRow("CO", 2024, "independence-day-co", "2024-07-20")]
    [DataRow("CO", 2024, "battle-of-boyaca", "2024-08-07")]
    [DataRow("PE", 2024, "independence-day-pe", "2024-07-28")]
    [DataRow("PE", 2024, "fiestas-patrias-day-two", "2024-07-29")]
    [DataRow("PE", 2024, "santa-rosa-de-lima", "2024-08-30")]
    [DataRow("PE", 2024, "battle-of-ayacucho", "2024-12-09")]
    public void Resolve_LatinAmericanFixedHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
    }

    /// <summary>
    /// Verifies that a Colombian Emiliani-law holiday whose calendar date is not a Monday is observed on the following
    /// Monday and carries the observed flag. Epiphany 2024 fell on a Saturday and is observed on Monday 8 January.
    /// </summary>
    [TestMethod]
    public void Resolve_ColombianEmilianiHoliday_WhenNotOnMonday_IsObservedOnFollowingMonday()
    {
        NotableDate moved = ResolveSingle("CO", 2024, "epiphany");

        Assert.AreEqual(
            (new DateOnly(2024, 1, 8), true),
            (moved.Date, moved.IsObserved));
    }

    /// <summary>
    /// Verifies that a Colombian Emiliani-law holiday already falling on a Monday is not moved and remains unobserved.
    /// Epiphany 2025 already falls on Monday 6 January.
    /// </summary>
    [TestMethod]
    public void Resolve_ColombianEmilianiHoliday_WhenAlreadyOnMonday_IsNotMoved()
    {
        NotableDate notMoved = ResolveSingle("CO", 2025, "epiphany");

        Assert.AreEqual(
            (new DateOnly(2025, 1, 6), false),
            (notMoved.Date, notMoved.IsObserved));
    }

    /// <summary>
    /// Verifies that the São Paulo Constitutionalist Revolution holiday resolves for the São Paulo subdivision query.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenBrazilianStateHolidayQueriedForState_ReturnsSingleResult()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.AreEqual(1, AmericasCalendarData.CreateService("BR-SP").Resolve(year, "BR-SP").Count(r => r.NotableDateId == "constitutionalist-revolution"));
    }

    /// <summary>
    /// Verifies that the São Paulo Constitutionalist Revolution holiday does not resolve for a plain national Brazil
    /// query, confirming subdivision scoping.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenBrazilianStateHolidayQueriedNationally_ReturnsNoResult()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.AreEqual(0, AmericasCalendarData.CreateService("BR").Resolve(year, "BR").Count(r => r.NotableDateId == "constitutionalist-revolution"));
    }
}
