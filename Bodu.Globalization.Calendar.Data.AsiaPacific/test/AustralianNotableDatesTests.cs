// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AustralianNotableDatesTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Data.AsiaPacific.Tests;

/// <summary>
/// Verifies the end-to-end behaviour of the Australia rule catalogue shipped in the Asia-Pacific data pack across the
/// <see cref="NotableDateService" />, including national-vs-subdivision scoping, weekend substitute handling, the <c>firstYear</c>
/// bound on Reconciliation Day, and caller-side delineation of country and state/territory entries via
/// <see cref="NotableDate.TerritoryCode" />.
/// </summary>
[TestClass]
public sealed class AustralianNotableDatesTests
{
    private static readonly string[] ExpectedTerritories =
    {
        "AU",
        "AU-ACT",
        "AU-NSW",
        "AU-NT",
        "AU-QLD",
        "AU-SA",
        "AU-TAS",
        "AU-VIC",
        "AU-WA",
    };

    private static NotableDateService BuildAuService() =>
        new(
            new[] { AsiaPacificCalendarData.CreateAustraliaProvider() },
            WorkingDaysOfWeek.MondayToFriday);

    /// <summary>
    /// Verifies that querying the country scope returns the four national fixed-date holidays at their unadjusted positions for
    /// year 2026, when none of them collide with a weekend.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAu_ShouldIncludeNationalRules_ForYear2026()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2026, "AU");

        Assert.IsTrue(results.Any(d => d.Name == "New Year's Day" && d.Date == new DateTime(2026, 1, 1)));
        Assert.IsTrue(results.Any(d => d.Name == "Australia Day" && d.Date == new DateTime(2026, 1, 26)));
        // 25 April 2026 is a Saturday. The canonical AU rule surfaces unchanged for the country-level AU query;
        // per-state shadowing only applies when the request targets a subdivision (AU-WA / AU-NT) that publishes its
        // own narrower rule. Christmas Day has its own weekend roll-forward through 26 December.
        Assert.IsTrue(results.Any(d => d.Name == "Anzac Day" && d.Date == new DateTime(2026, 4, 25) && d.TerritoryCode == "AU"));
        Assert.IsTrue(results.Any(d => d.Name == "Christmas Day"));
    }

    /// <summary>
    /// Verifies that when Australia Day falls on a Sunday (26 January 2020), the service emits the substitute non-working
    /// Monday observance carrying an <see cref="NotableDate.AdjustmentReason" />. The range pipeline emits a single
    /// post-adjustment occurrence per rule rather than the legacy pair of (original, adjusted).
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAustraliaDayFallsOnSunday_ShouldEmitMondaySubstitute()
    {
        var service = BuildAuService();

        var occurrences = service.GetNotableDates(2020, "AU")
            .Where(d => d.Name == "Australia Day")
            .OrderBy(d => d.Date)
            .ToList();

        Assert.AreEqual(1, occurrences.Count);
        Assert.AreEqual(new DateTime(2020, 1, 27), occurrences[0].Date);
        Assert.IsTrue(occurrences[0].WasAdjusted);
        Assert.AreEqual(DayOfWeek.Monday, occurrences[0].Date.DayOfWeek);
        Assert.AreEqual(new DateTime(2020, 1, 26), occurrences[0].AdjustmentReason!.OriginalDate);
    }

    /// <summary>
    /// Verifies that querying the Victorian subdivision returns the Victorian Labour Day on the second Monday of March without
    /// returning the New South Wales Labour Day, demonstrating that subdivision-scoped rules survive the composite-key flatten.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAuVic_ShouldIncludeVictoriaLabourDay_AndExcludeNswLabourDay()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2026, "AU-VIC");

        var labourDay = results.SingleOrDefault(d => d.Name == "Labour Day");
        Assert.IsNotNull(labourDay);
        Assert.AreEqual("AU-VIC", labourDay!.TerritoryCode);
        Assert.AreEqual(new DateTime(2026, 3, 9), labourDay.Date);
    }

    /// <summary>
    /// Verifies the mirror case: querying the New South Wales subdivision returns the NSW Labour Day on the first Monday of
    /// October and not the Victorian variant.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAuNsw_ShouldIncludeNswLabourDay_AndExcludeVictoriaLabourDay()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2026, "AU-NSW");

        var labourDay = results.SingleOrDefault(d => d.Name == "Labour Day");
        Assert.IsNotNull(labourDay);
        Assert.AreEqual("AU-NSW", labourDay!.TerritoryCode);
        Assert.AreEqual(new DateTime(2026, 10, 5), labourDay.Date);
    }

    /// <summary>
    /// Verifies that every notable date returned for the AU country scope carries a non-null
    /// <see cref="NotableDate.TerritoryCode" />, and that the set of distinct territory codes spans the country plus all eight
    /// state and territory subdivisions. This is the contract that lets callers delineate national entries from
    /// subdivision-specific ones in the produced list.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAu_ShouldPreserveTerritoryCodeOnEveryEntry_ForDelineation()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2026, "AU");

        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(d => !string.IsNullOrEmpty(d.TerritoryCode)),
            "Every Australian notable date must carry its originating TerritoryCode so callers can delineate national from subdivision entries.");

        var distinctTerritories = results
            .Select(d => d.TerritoryCode!)
            .Distinct()
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(ExpectedTerritories, distinctTerritories);
    }

    /// <summary>
    /// Verifies that the Queensland King's Birthday resolves to the first Monday of October for years from 2016 onwards,
    /// reflecting the statutory change that moved the holiday out of June.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAuQld_ShouldResolveKingsBirthdayToOctober_ForYear2026()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2026, "AU-QLD");

        var kingsBirthday = results.SingleOrDefault(d => d.Name == "King's Birthday");
        Assert.IsNotNull(kingsBirthday);
        Assert.AreEqual(new DateTime(2026, 10, 5), kingsBirthday!.Date);
    }

    /// <summary>
    /// Verifies that Reconciliation Day, introduced in the ACT in 2018, is suppressed for any year before 2018 by the rule's
    /// <c>firstYear</c> bound.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAuAct_ForYear2017_ShouldNotIncludeReconciliationDay()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2017, "AU-ACT");

        Assert.IsFalse(results.Any(d => d.Name == "Reconciliation Day"));
    }

    /// <summary>
    /// Verifies that Reconciliation Day appears for the ACT from 2018 onwards and resolves to a Monday in late May.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAuAct_ForYear2018_ShouldIncludeReconciliationDay()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2018, "AU-ACT");

        var reconciliation = results.SingleOrDefault(d => d.Name == "Reconciliation Day");
        Assert.IsNotNull(reconciliation);
        Assert.AreEqual(new DateTime(2018, 5, 28), reconciliation!.Date);
        Assert.AreEqual(DayOfWeek.Monday, reconciliation.Date.DayOfWeek);
    }

    /// <summary>
    /// Verifies that Melbourne Cup Day for Victoria resolves to the first Tuesday of November (3 November in 2026), equivalent
    /// to the statutory definition "the Tuesday in the week in which the first Tuesday in November occurs".
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMelbourneCupDay_ForYear2026_ShouldResolveToFirstTuesdayOfNovember()
    {
        var service = BuildAuService();

        var results = service.GetNotableDates(2026, "AU-VIC");

        var melbourneCup = results.SingleOrDefault(d => d.Name == "Melbourne Cup Day");
        Assert.IsNotNull(melbourneCup);
        Assert.AreEqual(new DateTime(2026, 11, 3), melbourneCup!.Date);
        Assert.AreEqual(DayOfWeek.Tuesday, melbourneCup.Date.DayOfWeek);
    }

    /// <summary>
    /// Verifies that querying NSW for 2026 reports the substitute Monday after Boxing Day as a non-working day, since
    /// 26 December 2026 falls on a Saturday and the national Boxing Day rule rolls weekend occurrences forward.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenBoxingDayOnSaturday_ShouldReturnTrueForSubstituteMonday_ForAuNsw()
    {
        var service = BuildAuService();

        // 26 December 2026 is a Saturday; the next non-weekend day is Monday 28 December.
        Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2026, 12, 28), "AU-NSW"));
    }

    /// <summary>
    /// Verifies that querying any Australian subdivision returns Anzac Day on 25 April 2026 (a Saturday) for the
    /// subdivisions that do not publish their own narrower rule (NSW, VIC, QLD, SA, TAS, ACT). WA and NT have their
    /// own subdivision-scoped rules with weekend substitutes — those are exercised separately.
    /// </summary>
    [TestMethod]
    [DataRow("AU-NSW")]
    [DataRow("AU-VIC")]
    [DataRow("AU-QLD")]
    [DataRow("AU-SA")]
    [DataRow("AU-TAS")]
    [DataRow("AU-ACT")]
    public void GetNotableDates_WhenQueryingSubdivisionWithoutSubstitute_ShouldEmitCanonicalAnzacDay_ForYear2026(string subdivision)
    {
        var service = BuildAuService();

        var anzacDay = service.GetNotableDates(2026, subdivision)
            .SingleOrDefault(d => d.Name == "Anzac Day");

        Assert.IsNotNull(anzacDay, $"Anzac Day should be visible to subdivision {subdivision}.");
        Assert.AreEqual("AU", anzacDay!.TerritoryCode, "Subdivisions without their own substitute rule fall back to the canonical AU rule.");
        Assert.AreEqual(new DateTime(2026, 4, 25), anzacDay.Date);
        Assert.IsFalse(anzacDay.WasAdjusted);
    }

    /// <summary>
    /// Verifies that Western Australia observes a substitute Monday when Anzac Day falls on a Saturday (25 April
    /// 2020). The AU-WA narrower rule shadows the canonical AU rule for AU-WA queries, so the emission carries the
    /// AU-WA territory code and the substitute date.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAnzacDayOnSaturday_ShouldEmitMondaySubstitute_ForAuWa()
    {
        var service = BuildAuService();

        var occurrences = service.GetNotableDates(2020, "AU-WA")
            .Where(d => d.Name == "Anzac Day")
            .OrderBy(d => d.Date)
            .ToList();

        Assert.AreEqual(1, occurrences.Count);
        Assert.AreEqual(new DateTime(2020, 4, 27), occurrences[0].Date);
        Assert.IsTrue(occurrences[0].WasAdjusted);
        Assert.AreEqual(DayOfWeek.Monday, occurrences[0].Date.DayOfWeek);
        Assert.AreEqual(new DateTime(2020, 4, 25), occurrences[0].AdjustmentReason!.OriginalDate);
        Assert.AreEqual("AU-WA", occurrences[0].TerritoryCode);
    }

    /// <summary>
    /// Verifies that New South Wales does NOT observe a substitute Monday when Anzac Day falls on a Saturday (25 April
    /// 2020): NSW has no narrower rule, so the canonical AU rule wins and emits the unadjusted Saturday observance.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAnzacDayOnSaturday_ShouldNotEmitSubstitute_ForAuNsw()
    {
        var service = BuildAuService();

        var occurrences = service.GetNotableDates(2020, "AU-NSW")
            .Where(d => d.Name == "Anzac Day")
            .ToList();

        Assert.AreEqual(1, occurrences.Count);
        Assert.AreEqual(new DateTime(2020, 4, 25), occurrences[0].Date);
        Assert.IsFalse(occurrences[0].WasAdjusted);
        Assert.AreEqual("AU", occurrences[0].TerritoryCode);
    }

    /// <summary>
    /// Verifies that the Northern Territory observes a substitute Monday when Anzac Day falls on a Sunday (25 April
    /// 2021). The AU-NT narrower rule shadows the canonical AU rule and emits the substitute Monday tagged with
    /// AU-NT.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenAnzacDayOnSunday_ShouldEmitMondaySubstitute_ForAuNt()
    {
        var service = BuildAuService();

        var occurrences = service.GetNotableDates(2021, "AU-NT")
            .Where(d => d.Name == "Anzac Day")
            .OrderBy(d => d.Date)
            .ToList();

        Assert.AreEqual(1, occurrences.Count);
        Assert.AreEqual(new DateTime(2021, 4, 26), occurrences[0].Date);
        Assert.IsTrue(occurrences[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2021, 4, 25), occurrences[0].AdjustmentReason!.OriginalDate);
        Assert.AreEqual("AU-NT", occurrences[0].TerritoryCode);
    }
}
