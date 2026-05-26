// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AustraliaTerritoryAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources for the
/// Australia rule catalogue shipped in the Asia-Pacific data pack. Each row encodes a single (territory, year, holiday)
/// expectation — including weekend substitutes, subdivision shadowing, and the <c>firstYear</c>-suppressed
/// Reconciliation Day case.
/// </summary>
public static class AustraliaTerritoryAnswers
{
    /// <summary>
    /// Gets a delegate that constructs the Australia rule provider used by every row in this class.
    /// </summary>
    /// <returns>The provider factory.</returns>
    private static Func<INotableDateRuleProvider> AuProvider =>
        AsiaPacificCalendarData.CreateAustraliaProvider;

    /// <summary>
    /// Provides the smoke-tier subset of Australia named-holiday occurrences. Runs on every BVT build.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="TerritoryNotableDateKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> NamedHolidayOccurrencesSmoke()
    {
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU",
            Year = 2026,
            Name = "Australia Day",
            ExpectedDate = new DateTime(2026, 1, 26),
        });

        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-VIC",
            Year = 2026,
            Name = "Labour Day",
            ExpectedDate = new DateTime(2026, 3, 9),
        });
    }

    /// <summary>
    /// Provides the full Australia named-holiday occurrence table. Covers national fixed-date rules, subdivision-scoped
    /// shadowing (NSW vs VIC Labour Day, AU-WA / AU-NT / AU-NSW Anzac Day weekend substitutes), the Queensland King's
    /// Birthday October placement, Melbourne Cup Day, the <c>firstYear</c>-suppressed Reconciliation Day, and the NSW
    /// Anzac Day 2026-2027 trial.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="TerritoryNotableDateKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> NamedHolidayOccurrences()
    {
        // National fixed-date holidays for AU 2026 (none collide with a weekend).
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU",
            Year = 2026,
            Name = "New Year's Day",
            ExpectedDate = new DateTime(2026, 1, 1),
        });

        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU",
            Year = 2026,
            Name = "Australia Day",
            ExpectedDate = new DateTime(2026, 1, 26),
        });

        // 25 April 2026 is a Saturday; the canonical AU rule surfaces unchanged for the AU country query
        // (subdivision shadowing only applies for subdivision-scoped queries).
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU",
            Year = 2026,
            Name = "Anzac Day",
            ExpectedDate = new DateTime(2026, 4, 25),
        });

        // Australia Day 2020 fell on Sunday 26 January; substitute Monday 27 January is the only emission.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU",
            Year = 2020,
            Name = "Australia Day",
            ExpectedDate = new DateTime(2020, 1, 27),
            WasAdjusted = true,
            OriginalDate = new DateTime(2020, 1, 26),
            ExpectedDayOfWeek = DayOfWeek.Monday,
        });

        // Victorian Labour Day on the second Monday of March (NSW's October Labour Day must NOT appear).
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-VIC",
            Year = 2026,
            Name = "Labour Day",
            ExpectedDate = new DateTime(2026, 3, 9),
        });

        // NSW Labour Day on the first Monday of October (Victorian variant must NOT appear).
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-NSW",
            Year = 2026,
            Name = "Labour Day",
            ExpectedDate = new DateTime(2026, 10, 5),
        });

        // Queensland King's Birthday moved to the first Monday of October from 2016.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-QLD",
            Year = 2026,
            Name = "King's Birthday",
            ExpectedDate = new DateTime(2026, 10, 5),
        });

        // ACT Reconciliation Day suppressed in 2017 by the rule's firstYear=2018 bound.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-ACT",
            Year = 2017,
            Name = "Reconciliation Day",
            ExpectedDate = default,
            ShouldExist = false,
            Note = "firstYear=2018",
        });

        // ACT Reconciliation Day resolves to a Monday in late May from 2018 onwards.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-ACT",
            Year = 2018,
            Name = "Reconciliation Day",
            ExpectedDate = new DateTime(2018, 5, 28),
            ExpectedDayOfWeek = DayOfWeek.Monday,
        });

        // Melbourne Cup Day resolves to the first Tuesday of November.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-VIC",
            Year = 2026,
            Name = "Melbourne Cup Day",
            ExpectedDate = new DateTime(2026, 11, 3),
            ExpectedDayOfWeek = DayOfWeek.Tuesday,
        });

        // Subdivisions without their own substitute rule fall back to the canonical AU Anzac Day rule on
        // Saturday 25 April 2026.
        foreach (string subdivision in new[] { "AU-VIC", "AU-QLD", "AU-SA", "AU-TAS", "AU-ACT" })
        {
            yield return Row(new TerritoryNotableDateKnownAnswer
            {
                ProviderFactory = AuProvider,
                Territory = subdivision,
                Year = 2026,
                Name = "Anzac Day",
                ExpectedDate = new DateTime(2026, 4, 25),
                ExpectedTerritoryCode = "AU",
            });
        }

        // AU-WA narrower rule shadows the canonical AU rule and applies a substitute Monday for Saturday
        // 25 April 2020.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-WA",
            Year = 2020,
            Name = "Anzac Day",
            ExpectedDate = new DateTime(2020, 4, 27),
            WasAdjusted = true,
            OriginalDate = new DateTime(2020, 4, 25),
            ExpectedDayOfWeek = DayOfWeek.Monday,
        });

        // AU-NSW Anzac Day in 2020: the NSW rule shadows AU but its adjustment is dormant outside the
        // 2026-2027 Minns trial — the base 25 April observance surfaces unchanged tagged AU-NSW.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-NSW",
            Year = 2020,
            Name = "Anzac Day",
            ExpectedDate = new DateTime(2020, 4, 25),
            Note = "NSW rule shadows AU; substitute dormant outside trial",
        });

        // First year of the NSW 2026-2027 Minns substitute trial — Saturday 25 April 2026 rolls to
        // Monday 27 April 2026 tagged AU-NSW.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-NSW",
            Year = 2026,
            Name = "Anzac Day",
            ExpectedDate = new DateTime(2026, 4, 27),
            WasAdjusted = true,
            OriginalDate = new DateTime(2026, 4, 25),
            ExpectedDayOfWeek = DayOfWeek.Monday,
            Note = "NSW Minns trial year 1",
        });

        // Second (final) year of the NSW trial — Sunday 25 April 2027 rolls to Monday 26 April 2027.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-NSW",
            Year = 2027,
            Name = "Anzac Day",
            ExpectedDate = new DateTime(2027, 4, 26),
            WasAdjusted = true,
            OriginalDate = new DateTime(2027, 4, 25),
            ExpectedDayOfWeek = DayOfWeek.Monday,
            Note = "NSW Minns trial year 2",
        });

        // AU-NT Anzac Day 2021: Sunday 25 April rolls to Monday 26 April tagged AU-NT.
        yield return Row(new TerritoryNotableDateKnownAnswer
        {
            ProviderFactory = AuProvider,
            Territory = "AU-NT",
            Year = 2021,
            Name = "Anzac Day",
            ExpectedDate = new DateTime(2021, 4, 26),
            WasAdjusted = true,
            OriginalDate = new DateTime(2021, 4, 25),
        });
    }

    /// <summary>
    /// Wraps a known-answer row in the single-element object array shape
    /// <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> expects.
    /// </summary>
    /// <param name="row">The row to wrap.</param>
    /// <returns>A single-element object array carrying the row.</returns>
    private static object[] Row(TerritoryNotableDateKnownAnswer row) => [row];
}
