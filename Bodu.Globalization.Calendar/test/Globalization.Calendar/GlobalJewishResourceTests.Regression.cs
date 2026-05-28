// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalJewishResourceTests.Regression.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Regression-tier coverage for <c>global-jewish.xml</c>: a six-year table (Gregorian 2020–2025, Hebrew 5780–5786)
/// of expected dates for the principal Jewish observances. Expected values are sourced from Hebcal
/// (<see href="https://www.hebcal.com" />), the widely-used civic-calendar reference whose computation matches the
/// Rabbinic consensus and the BCL <see cref="System.Globalization.HebrewCalendar" />. Tagged <c>Regression</c> so
/// the default BVT run is unaffected; included in <c>regression.runsettings</c>.
/// </summary>
public sealed partial class GlobalJewishResourceTests
{
    /// <summary>
    /// Verifies that representative Jewish observances resolve to the published Hebcal dates across Gregorian years
    /// 2020–2025. Anchor rules (Rosh Hashanah, Hanukkah, Purim, Passover) flow through
    /// <see cref="DateResolutionStrategy.Fixed" /> with <see cref="System.Globalization.HebrewCalendar" />; offset
    /// rules (Yom Kippur, Sukkot, Shavuot) flow through <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
    /// </summary>
    // 2020 (Hebrew years 5780–5781)
    [DataRow(2020, "Purim", 3, 10)]
    [DataRow(2020, "Passover", 4, 9)]
    [DataRow(2020, "Shavuot", 5, 29)]       // Passover + 50
    [DataRow(2020, "Rosh Hashanah", 9, 19)]
    [DataRow(2020, "Yom Kippur", 9, 28)]    // Rosh Hashanah + 9
    [DataRow(2020, "Sukkot", 10, 3)]        // Rosh Hashanah + 14
    [DataRow(2020, "Hanukkah", 12, 11)]
    // 2021 (Hebrew years 5781–5782)
    [DataRow(2021, "Purim", 2, 26)]
    [DataRow(2021, "Passover", 3, 28)]
    [DataRow(2021, "Shavuot", 5, 17)]
    [DataRow(2021, "Rosh Hashanah", 9, 7)]
    [DataRow(2021, "Yom Kippur", 9, 16)]
    [DataRow(2021, "Sukkot", 9, 21)]
    [DataRow(2021, "Hanukkah", 11, 29)]
    // 2022 (Hebrew years 5782–5783)
    [DataRow(2022, "Purim", 3, 17)]
    [DataRow(2022, "Passover", 4, 16)]
    [DataRow(2022, "Shavuot", 6, 5)]
    [DataRow(2022, "Rosh Hashanah", 9, 26)]
    [DataRow(2022, "Yom Kippur", 10, 5)]
    [DataRow(2022, "Sukkot", 10, 10)]
    [DataRow(2022, "Hanukkah", 12, 19)]
    // 2023 (Hebrew years 5783–5784)
    [DataRow(2023, "Purim", 3, 7)]
    [DataRow(2023, "Passover", 4, 6)]
    [DataRow(2023, "Shavuot", 5, 26)]
    [DataRow(2023, "Rosh Hashanah", 9, 16)]
    [DataRow(2023, "Yom Kippur", 9, 25)]
    [DataRow(2023, "Sukkot", 9, 30)]
    [DataRow(2023, "Hanukkah", 12, 8)]
    // 2024 (Hebrew years 5784–5785)
    [DataRow(2024, "Purim", 3, 24)]
    [DataRow(2024, "Passover", 4, 23)]
    [DataRow(2024, "Shavuot", 6, 12)]
    [DataRow(2024, "Rosh Hashanah", 10, 3)]
    [DataRow(2024, "Yom Kippur", 10, 12)]
    [DataRow(2024, "Sukkot", 10, 17)]
    [DataRow(2024, "Hanukkah", 12, 26)]
    // 2025 (Hebrew years 5785–5786)
    [DataRow(2025, "Purim", 3, 14)]
    [DataRow(2025, "Passover", 4, 13)]
    [DataRow(2025, "Shavuot", 6, 2)]
    [DataRow(2025, "Rosh Hashanah", 9, 23)]
    [DataRow(2025, "Yom Kippur", 10, 2)]
    [DataRow(2025, "Sukkot", 10, 7)]
    [DataRow(2025, "Hanukkah", 12, 15)]
    [TestMethod]
    [TestCategory("Regression")]
    public void GetNotableDates_WhenLoadingGlobalJewishAcrossSixYears_ShouldMatchHebcalReferenceDates(int year, string holiday, int expectedMonth, int expectedDay)
    {
        NotableDateService service = CreateBareService();

        // Filter on Date.Year because multi-day holidays whose anchor falls late in the prior Gregorian year
        // (notably Hanukkah, which can begin 25 Dec) are returned for both adjacent year queries when their
        // duration spans the boundary. We want the occurrence whose anchor is in the queried year.
        NotableDate? resolved = service.GetNotableDates(year)
            .Where(r => string.Equals(r.Name, holiday, StringComparison.OrdinalIgnoreCase) && r.Date.Year == year)
            .OrderBy(r => r.Date)
            .FirstOrDefault();

        Assert.IsNotNull(resolved, $"{holiday} unresolved for Gregorian year {year}.");
        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), resolved!.Date);
    }
}
