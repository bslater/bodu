// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Verifies that the migrated Europe resource pack resolves United Kingdom, French, and German holidays to their known
/// dates, exercising the British bank-holiday weekend roll and conflict-aware substitution, Easter-derived offsets, and
/// the continental no-substitution convention.
/// </summary>
[TestClass]
public sealed class EuropeCalendarDataTests
{
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
        List<NotableDate> matches = EuropeCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");
        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), matches[0].Date, "emitted date");
        Assert.AreEqual(isObserved, matches[0].IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that every supported European country loads and validates (the loader throws on a validation error) and
    /// resolves a non-empty set of holidays for a representative year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void CreateService_ForEverySupportedCountry_LoadsAndResolves()
    {
        foreach (string country in EuropeCalendarData.SupportedCountries)
        {
            IReadOnlyList<NotableDate> holidays = EuropeCalendarData.CreateService(country)
                .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), country);

            Assert.IsTrue(holidays.Count > 0, $"{country} resolved no holidays for 2024");
        }
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
        List<NotableDate> matches = EuropeCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");
        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), matches[0].Date, "emitted date");
        Assert.AreEqual(isObserved, matches[0].IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that a Scotland-scoped holiday does not resolve for an England query, confirming subdivision filtering.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenScottishHolidayQueriedForEngland_ReturnsNoResult()
    {
        int count = EuropeCalendarData.CreateService("GB-ENG")
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "GB-ENG")
            .Count(r => r.NotableDateId == "saint-andrews-day");

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that the Alsace-Moselle Good Friday resolves for a département query but not for a plain national
    /// France query, and that the restored Assumption of Mary is a non-working public holiday in France.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_FrenchAlsaceMoselleAndRestoredEntries_RespectScopeAndStatus()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.AreEqual(1, EuropeCalendarData.CreateService("FR-67").Resolve(year, "FR-67").Count(r => r.NotableDateId == "good-friday"),
            "Good Friday should resolve for Alsace-Moselle (FR-67)");
        Assert.AreEqual(0, EuropeCalendarData.CreateService("FR").Resolve(year, "FR").Count(r => r.NotableDateId == "good-friday"),
            "Good Friday should not resolve for a plain national France query");

        // The Assumption (15 August) is a statutory non-working public holiday in France — checked on the occurrence
        // flag rather than the IsNonWorkingDay extension, which also treats weekends as non-working.
        NotableDate assumption = EuropeCalendarData.CreateService("FR").Resolve(year, "FR").Single(r => r.NotableDateId == "assumption-of-mary");
        Assert.IsTrue(assumption.IsNonWorkingDay, "the Assumption should be a non-working public holiday in France");
    }

    /// <summary>
    /// Verifies that the German federal-state religious feasts resolve as non-working public holidays for the Länder
    /// that observe them but not for a plain national query or a non-observing state, confirming the audit re-scoping.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_GermanStateFeasts_AreScopedToTheirLaender()
    {
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        // Epiphany (6 Jan) is a non-working public holiday in Bavaria, but resolves for neither a plain national
        // query (DE-BW/BY/ST does not match "DE") nor a non-observing state (Berlin).
        NotableDate bavarianEpiphany = EuropeCalendarData.CreateService("DE-BY").Resolve(year, "DE-BY").Single(r => r.NotableDateId == "epiphany");
        Assert.IsTrue(bavarianEpiphany.IsNonWorkingDay, "Epiphany should be a non-working public holiday in Bavaria");
        Assert.AreEqual(0, EuropeCalendarData.CreateService("DE").Resolve(year, "DE").Count(r => r.NotableDateId == "epiphany"),
            "Epiphany should not resolve for a plain national Germany query");
        Assert.AreEqual(0, EuropeCalendarData.CreateService("DE-BE").Resolve(year, "DE-BE").Count(r => r.NotableDateId == "epiphany"),
            "Epiphany should not resolve for Berlin");

        // International Women's Day (8 Mar) is a non-working public holiday in Berlin but a working observance
        // nationally — the two-rule concept with territory shadowing returns the state rule for BE and the national
        // rule for a plain DE query.
        NotableDate berlinWomensDay = EuropeCalendarData.CreateService("DE-BE").Resolve(year, "DE-BE").Single(r => r.NotableDateId == "womens-day");
        Assert.IsTrue(berlinWomensDay.IsNonWorkingDay, "Women's Day should be a non-working public holiday in Berlin");
        NotableDate nationalWomensDay = EuropeCalendarData.CreateService("DE").Resolve(year, "DE").Single(r => r.NotableDateId == "womens-day");
        Assert.IsFalse(nationalWomensDay.IsNonWorkingDay, "Women's Day should be a working observance nationally");
    }
}
