// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AustralianNotableDatesTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Data.AsiaPacific.Tests;

/// <summary>
/// Verifies the end-to-end behaviour of the Australia rule catalogue shipped in the Asia-Pacific data pack
/// across <see cref="NotableDateService" />, including national-vs-subdivision scoping, weekend substitute
/// handling, the <c>firstYear</c> bound on Reconciliation Day, the NSW Anzac Day 2026-2027 trial, and
/// caller-side delineation of country and state/territory entries via <see cref="NotableDate.TerritoryCode" />.
/// </summary>
[TestClass]
public sealed class AustralianNotableDatesTests
{
    private static readonly string[] s_expectedTerritories =
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

    /// <summary>
    /// Verifies a smoke-tier subset of the Australia named-holiday occurrences. Runs on every BVT build.
    /// </summary>
    /// <param name="knownAnswer">The known-answer row supplied by
    /// <see cref="AustraliaTerritoryAnswers.NamedHolidayOccurrencesSmoke" />.</param>
    [TestMethod]
    [DynamicData(
        nameof(AustraliaTerritoryAnswers.NamedHolidayOccurrencesSmoke),
        typeof(AustraliaTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void GetNotableDates_WhenGivenSmokeRow_ShouldReturnExpectedOccurrence(TerritoryNotableDateKnownAnswer knownAnswer) =>
        TerritoryNotableDateAssertions.AssertExpectedOccurrence(knownAnswer);

    /// <summary>
    /// Verifies every Australia named-holiday occurrence row — national rules, subdivision shadowing,
    /// weekend substitutes (AU-WA, AU-NT, AU-NSW trial), Melbourne Cup, QLD King's Birthday October
    /// placement, and the <c>firstYear</c>-suppressed Reconciliation Day. Runs only under the Regression
    /// tier.
    /// </summary>
    /// <param name="knownAnswer">The known-answer row supplied by
    /// <see cref="AustraliaTerritoryAnswers.NamedHolidayOccurrences" />.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AustraliaTerritoryAnswers.NamedHolidayOccurrences),
        typeof(AustraliaTerritoryAnswers),
        DynamicDataDisplayName = nameof(TerritoryNotableDateAssertions.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TerritoryNotableDateAssertions))]
    public void GetNotableDates_WhenGivenKnownYearAndTerritory_ShouldReturnExpectedOccurrence(TerritoryNotableDateKnownAnswer knownAnswer) =>
        TerritoryNotableDateAssertions.AssertExpectedOccurrence(knownAnswer);

    /// <summary>
    /// Verifies that every notable date returned for the AU country scope carries a non-null
    /// <see cref="NotableDate.TerritoryCode" />, and that the set of distinct territory codes spans the
    /// country plus all eight state and territory subdivisions. This is the contract that lets callers
    /// delineate national entries from subdivision-specific ones; the shape of the assertion (set
    /// equivalence over all returned entries) does not fit the per-occurrence KAT record, so it stays
    /// bespoke.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenQueryingAu_ShouldPreserveTerritoryCodeOnEveryEntry_ForDelineation()
    {
        NotableDateService service = new(
            new[] { AsiaPacificCalendarData.CreateAustraliaProvider() },
            WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> results = service.GetNotableDates(2026, "AU");

        Assert.IsTrue(results.Count > 0);
        Assert.IsTrue(results.All(d => !string.IsNullOrEmpty(d.TerritoryCode)),
            "Every Australian notable date must carry its originating TerritoryCode so callers can delineate national from subdivision entries.");

        string[] distinctTerritories = results
            .Select(d => d.TerritoryCode!)
            .Distinct()
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(s_expectedTerritories, distinctTerritories);
    }

    /// <summary>
    /// Verifies that querying NSW for 2026 reports the substitute Monday after Boxing Day as a non-working
    /// day, since 26 December 2026 falls on a Saturday and the national Boxing Day rule rolls weekend
    /// occurrences forward. The <see cref="NotableDateService.IsNonWorkingDay" /> query shape does not fit
    /// the per-occurrence KAT record, so it stays bespoke.
    /// </summary>
    [TestMethod]
    public void IsNonWorkingDay_WhenBoxingDayOnSaturday_ShouldReturnTrueForSubstituteMonday_ForAuNsw()
    {
        NotableDateService service = new(
            new[] { AsiaPacificCalendarData.CreateAustraliaProvider() },
            WorkingDaysOfWeek.MondayToFriday);

        Assert.IsTrue(service.IsNonWorkingDay(new DateTime(2026, 12, 28), "AU-NSW"));
    }
}
