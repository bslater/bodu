// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the migrated Europe resource pack resolves United Kingdom, French, and German holidays to their known
/// dates, exercising the British bank-holiday weekend roll and conflict-aware substitution, Easter-derived offsets, and
/// the continental no-substitution convention.
/// </summary>
[TestClass]
public sealed class EuropeCalendarDataTests
    : CalendarDataTestsBase
{
    /// <inheritdoc />
    protected override IReadOnlyList<string> SupportedCountries => EuropeCalendarData.SupportedCountries;

    /// <inheritdoc />
    protected override INotableDateService CreateService(string territory) =>
        EuropeCalendarData.CreateService(territory);

    /// <summary>
    /// Verifies that each European holiday resolves to its known emitted date and observed flag.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // United Kingdom: bank-holiday weekend roll, Easter offsets, Mothering Sunday, conflict-aware Christmas/Boxing.
    [DataRow("GB", 2023, "new-years-day", "2023-01-02", true)]
    [DataRow("GB", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("GB", 2024, "mothering-sunday", "2024-03-10", false)]
    [DataRow("GB", 2024, "early-may-bank-holiday", "2024-05-06", false)]
    [DataRow("GB", 2024, "spring-bank-holiday", "2024-05-27", false)]
    [DataRow("GB-ENG", 2024, "summer-bank-holiday", "2024-08-26", false)]
    [DataRow("GB", 2021, "christmas-day", "2021-12-27", true)]
    [DataRow("GB", 2021, "boxing-day", "2021-12-28", true)]

    // United Kingdom: entries restored by the entry-level migration audit — Christmas Eve and Father's Day come via
    // the europe-common hub; April Fool's Day and Remembrance Day (11 November) come via the global-all aggregate.
    [DataRow("GB", 2024, "christmas-eve", "2024-12-24", false)]
    [DataRow("GB", 2024, "fathers-day", "2024-06-16", false)]
    [DataRow("GB", 2024, "april-fools-day", "2024-04-01", false)]
    [DataRow("GB", 2024, "remembrance-day", "2024-11-11", false)]

    // Germany: entries restored/fixed by the audit — Christmas Eve and Father's Day come via the europe-common hub.
    [DataRow("DE", 2024, "christmas-eve", "2024-12-24", false)]
    [DataRow("DE", 2024, "fathers-day", "2024-06-16", false)]

    // France: Easter offsets and fixed nationals, no weekend substitution (Bastille Day on a Sunday stays put).
    [DataRow("FR", 2024, "easter-monday", "2024-04-01", false)]
    [DataRow("FR", 2024, "ascension-day", "2024-05-09", false)]
    [DataRow("FR", 2024, "whit-monday", "2024-05-20", false)]
    [DataRow("FR", 2024, "bastille-day", "2024-07-14", false)]
    [DataRow("FR", 2024, "armistice-day", "2024-11-11", false)]

    // France: entries restored/fixed by the entry-level migration audit — Christmas Eve, Father's Day, Whit Sunday
    // (Pentecost), the Assumption, and April Fool's Day come via the europe-common hub; the Alsace-Moselle Good
    // Friday and Saint Stephen's Day are subdivision-scoped to FR-67/FR-68/FR-57.
    [DataRow("FR", 2024, "christmas-eve", "2024-12-24", false)]
    [DataRow("FR", 2024, "fathers-day", "2024-06-16", false)]
    [DataRow("FR", 2024, "whit-sunday", "2024-05-19", false)]
    [DataRow("FR", 2024, "assumption-of-mary", "2024-08-15", false)]
    [DataRow("FR", 2024, "april-fools-day", "2024-04-01", false)]
    [DataRow("FR-67", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("FR-67", 2024, "saint-stephens-day", "2024-12-26", false)]

    // Germany: Easter offsets, fixed nationals, and the now state-scoped regional feasts (Corpus Christi and the
    // weekday-near Repentance Day resolve only for the Länder that observe them, per the audit re-scoping).
    [DataRow("DE", 2024, "ascension-day", "2024-05-09", false)]
    [DataRow("DE-BW", 2024, "corpus-christi", "2024-05-30", false)]
    [DataRow("DE", 2024, "german-unity-day", "2024-10-03", false)]
    [DataRow("DE-SN", 2024, "repentance-day", "2024-11-20", false)]
    public void Resolve_EuropeanHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies representative migrated holidays from the script-generated countries: Italian fixed nationals, the Greek
    /// Orthodox Easter cycle, and the Irish weekend substitution.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("IT", 2024, "epiphany", "2024-01-06", false)]
    [DataRow("IT", 2024, "liberation-day", "2024-04-25", false)]
    [DataRow("IT", 2024, "republic-day", "2024-06-02", false)]
    [DataRow("IT", 2024, "immaculate-conception", "2024-12-08", false)]
    [DataRow("GR", 2024, "orthodox-easter-monday", "2024-05-06", false)]
    [DataRow("IE", 2024, "saint-patricks-day", "2024-03-18", true)]
    public void Resolve_MigratedEuropeanHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = ResolveSingle(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that a Scotland-scoped holiday does not resolve for an England query, confirming subdivision filtering.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenScottishHolidayQueriedForEngland_ReturnsNoResult()
    {
        var count = EuropeCalendarData.CreateService("GB-ENG")
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "GB-ENG")
            .Count(r => r.NotableDateId == "saint-andrews-day");

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that the Alsace-Moselle Good Friday resolves for a département query (FR-67), confirming the
    /// subdivision-scoped rule applies for the départements that observe it.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_FrenchAlsaceMoselleGoodFriday_WhenQueriedForDepartement_ReturnsSingleResult()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.ContainsSingle(r => r.NotableDateId == "good-friday", EuropeCalendarData.CreateService("FR-67").Resolve(year, "FR-67"));
    }

    /// <summary>
    /// Verifies that the Alsace-Moselle Good Friday does not resolve for a plain national France query, confirming the
    /// subdivision scoping.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_FrenchAlsaceMoselleGoodFriday_WhenQueriedNationally_ReturnsNoResult()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.AreEqual(0, EuropeCalendarData.CreateService("FR").Resolve(year, "FR").Count(r => r.NotableDateId == "good-friday"));
    }

    /// <summary>
    /// Verifies that the restored Assumption of Mary (15 August) is a non-working public holiday in France — checked on
    /// the occurrence flag rather than the IsNonWorkingDay extension, which also treats weekends as non-working.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_FrenchAssumptionOfMary_IsNonWorkingPublicHoliday()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        NotableDate assumption = EuropeCalendarData.CreateService("FR").Resolve(year, "FR").Single(r => r.NotableDateId == "assumption-of-mary");

        Assert.IsTrue(assumption.IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that Epiphany (6 January) is a non-working public holiday in Bavaria, confirming the Länder-scoped feast
    /// resolves for a state that observes it.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_GermanEpiphany_WhenQueriedForBavaria_IsNonWorkingPublicHoliday()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        NotableDate bavarianEpiphany = EuropeCalendarData.CreateService("DE-BY").Resolve(year, "DE-BY").Single(r => r.NotableDateId == "epiphany");

        Assert.IsTrue(bavarianEpiphany.IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that Epiphany does not resolve for a plain national Germany query, confirming the feast is scoped to the
    /// observing Länder rather than the nation.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_GermanEpiphany_WhenQueriedNationally_ReturnsNoResult()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.AreEqual(0, EuropeCalendarData.CreateService("DE").Resolve(year, "DE").Count(r => r.NotableDateId == "epiphany"));
    }

    /// <summary>
    /// Verifies that Epiphany does not resolve for Berlin (a non-observing state), confirming the feast is scoped to the
    /// Länder that observe it.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_GermanEpiphany_WhenQueriedForNonObservingState_ReturnsNoResult()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.AreEqual(0, EuropeCalendarData.CreateService("DE-BE").Resolve(year, "DE-BE").Count(r => r.NotableDateId == "epiphany"));
    }

    /// <summary>
    /// Verifies that International Women's Day (8 March) is a non-working public holiday in Berlin, confirming the state
    /// rule shadows the national rule for a Berlin query.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_GermanWomensDay_WhenQueriedForBerlin_IsNonWorkingPublicHoliday()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        NotableDate berlinWomensDay = EuropeCalendarData.CreateService("DE-BE").Resolve(year, "DE-BE").Single(r => r.NotableDateId == "womens-day");

        Assert.IsTrue(berlinWomensDay.IsNonWorkingDay);
    }

    /// <summary>
    /// Verifies that International Women's Day is a working observance for a plain national Germany query, confirming the
    /// national rule applies where no shadowing state rule matches.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_GermanWomensDay_WhenQueriedNationally_IsWorkingObservance()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        NotableDate nationalWomensDay = EuropeCalendarData.CreateService("DE").Resolve(year, "DE").Single(r => r.NotableDateId == "womens-day");

        Assert.IsFalse(nationalWomensDay.IsNonWorkingDay);
    }
}
