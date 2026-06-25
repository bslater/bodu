// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarDataPackDiscoveryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that every regional calendar data pack the solution ships is built, loadable, and able to resolve
/// holidays, acting as a central completeness guard so that a green build can never again hide a missing regional
/// pack (as it did when the Africa and MiddleEast packs were excluded from the Debug build via <c>bodu.slnx</c>).
/// </summary>
/// <remarks>
/// The guard works at two levels. The <see cref="ExpectedRegionalDataPacks" /> manifest references each
/// <c>&lt;Region&gt;CalendarData</c> factory directly, so a pack dropped from the build breaks compilation of this
/// test assembly. At run time, the tests then confirm each pack actually loads its embedded resources and resolves
/// notable dates.
/// </remarks>
[TestClass]
public sealed class CalendarDataPackDiscoveryTests
{
    /// <summary>
    /// Gets the complete manifest of regional data packs the solution ships, one row per region.
    /// </summary>
    /// <value>A <c>[DynamicData]</c> source yielding a single <see cref="RegionalCalendarDataPack" /> per row.</value>
    public static IEnumerable<object[]> ExpectedRegionalDataPacks =>
    [
        [new RegionalCalendarDataPack("Africa", () => AfricaCalendarData.SupportedCountries, AfricaCalendarData.CreateService)],
        [new RegionalCalendarDataPack("Americas", () => AmericasCalendarData.SupportedCountries, AmericasCalendarData.CreateService)],
        [new RegionalCalendarDataPack("AsiaPacific", () => AsiaPacificCalendarData.SupportedCountries, AsiaPacificCalendarData.CreateService)],
        [new RegionalCalendarDataPack("Europe", () => EuropeCalendarData.SupportedCountries, EuropeCalendarData.CreateService)],
        [new RegionalCalendarDataPack("MiddleEast", () => MiddleEastCalendarData.SupportedCountries, MiddleEastCalendarData.CreateService)],
    ];

    /// <summary>
    /// Verifies that each expected regional pack is built and loadable: it exposes at least one supported country
    /// and its first country resolves a non-empty set of holidays, proving the embedded resource pack loads.
    /// </summary>
    /// <param name="pack">The regional data pack under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(ExpectedRegionalDataPacks),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RegionalDataPack_ForEveryExpectedRegion_IsBuiltAndResolves(RegionalCalendarDataPack pack)
    {
        IReadOnlyList<string> countries = pack.GetSupportedCountries();

        Assert.IsTrue(countries.Count > 0, $"{pack.Region} pack exposed no supported countries");

        string firstCountry = countries[0];
        IReadOnlyList<NotableDate> holidays = ResolveYear(pack, firstCountry, 2024);

        Assert.IsTrue(holidays.Count > 0, $"{pack.Region}/{firstCountry} resolved no holidays for 2024");
    }

    /// <summary>
    /// Verifies that every country in every regional pack loads, validates, and resolves a non-empty set of
    /// holidays for a representative year, exercising the full shipped territory set in one central place.
    /// </summary>
    /// <param name="pack">The regional data pack under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ExpectedRegionalDataPacks),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RegionalDataPack_ForEverySupportedCountry_ResolvesHolidays(RegionalCalendarDataPack pack)
    {
        foreach (string country in pack.GetSupportedCountries())
        {
            IReadOnlyList<NotableDate> holidays = ResolveYear(pack, country, 2024);

            Assert.IsTrue(holidays.Count > 0, $"{pack.Region}/{country} resolved no holidays for 2024");
        }
    }

    /// <summary>
    /// Resolves every notable date emitted for <paramref name="territory" /> across the whole of
    /// <paramref name="year" /> using the supplied pack's data factory.
    /// </summary>
    /// <param name="pack">The regional data pack whose factory creates the service.</param>
    /// <param name="territory">The territory code to resolve.</param>
    /// <param name="year">The Gregorian year whose 1 January–31 December window is resolved.</param>
    /// <returns>The notable dates emitted for the territory and year, in resolution order.</returns>
    private static IReadOnlyList<NotableDate> ResolveYear(RegionalCalendarDataPack pack, string territory, int year) =>
        pack.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory);
}
