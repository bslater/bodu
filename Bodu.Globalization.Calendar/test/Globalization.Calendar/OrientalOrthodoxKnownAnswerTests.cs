// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrientalOrthodoxKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the floating Oriental Orthodox observances authored in the bundled <c>christian-oriental-orthodox</c>
/// catalogue. The Pascha cluster is anchored by the engine's <c>orthodox-easter</c> algorithm (the Alexandrian /
/// Julian-paschalion computus shared by the Coptic, Ethiopian, Armenian and related traditions) with the remaining
/// feasts authored as <see cref="OffsetFromRuleStrategy" /> day-offsets from it, so every date is deterministic.
/// Expected Pascha dates are the published Orthodox Easter Sundays for 2023-2027.
/// </summary>
/// <remarks>
/// <para>
/// The fixed civil observances (Armenian Christmas, Oriental Christmas, Theophany, Coptic New Year, Meskel) use their
/// common Gregorian-era dates. Coptic New Year and Meskel are pinned at 11 and 27 September; in practice both fall a day
/// later (12 / 28 September) in the Gregorian year preceding a leap year, a rite-specific shift the common catalogue
/// intentionally leaves to territory packs and which is recorded in the verification report.
/// </para>
/// </remarks>
[TestClass]
public sealed class OrientalOrthodoxKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the bundled <c>christian-oriental-orthodox</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("christian-oriental-orthodox");

    /// <summary>
    /// Verifies that the Oriental Orthodox Pascha anchor resolves to the published Orthodox Easter Sunday for each year
    /// 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 4, 16)]
    [DataRow(2024, 5, 5)]
    [DataRow(2025, 4, 20)]
    [DataRow(2026, 4, 12)]
    [DataRow(2027, 5, 2)]
    public void Resolve_OrientalOrthodoxEaster_MatchesPublishedOrthodoxPascha(int year, int month, int day)
    {
        NotableDate easter = CommonCatalogues.ResolveSingle(CreateService(), "oriental-orthodox-easter-sunday", year);

        Assert.AreEqual(new DateOnly(year, month, day), easter.Date, $"Oriental Orthodox Pascha {year}");
    }

    /// <summary>
    /// Verifies that every feast derived from the Pascha anchor resolves to its exact offset from Orthodox Easter for two
    /// representative years (2024, Pascha 5 May; 2025, Pascha 20 April).
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // 2024 — Orthodox Pascha 5 May.
    [DataRow(2024, "oriental-orthodox-palm-sunday", 4, 28)]      // Pascha - 7
    [DataRow(2024, "oriental-orthodox-covenant-thursday", 5, 2)] // Pascha - 3
    [DataRow(2024, "oriental-orthodox-good-friday", 5, 3)]       // Pascha - 2
    [DataRow(2024, "oriental-orthodox-holy-saturday", 5, 4)]     // Pascha - 1
    [DataRow(2024, "oriental-orthodox-easter-monday", 5, 6)]     // Pascha + 1
    [DataRow(2024, "oriental-orthodox-ascension-day", 6, 13)]    // Pascha + 39
    [DataRow(2024, "oriental-orthodox-pentecost", 6, 23)]        // Pascha + 49

    // 2025 — Orthodox Pascha 20 April.
    [DataRow(2025, "oriental-orthodox-palm-sunday", 4, 13)]
    [DataRow(2025, "oriental-orthodox-covenant-thursday", 4, 17)]
    [DataRow(2025, "oriental-orthodox-good-friday", 4, 18)]
    [DataRow(2025, "oriental-orthodox-holy-saturday", 4, 19)]
    [DataRow(2025, "oriental-orthodox-easter-monday", 4, 21)]
    [DataRow(2025, "oriental-orthodox-ascension-day", 5, 29)]
    [DataRow(2025, "oriental-orthodox-pentecost", 6, 8)]
    public void Resolve_PaschaDerivedFeast_MatchesOffsetFromOrthodoxEaster(int year, string notableDateId, int month, int day)
    {
        NotableDate feast = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, month, day), feast.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that the fixed civil Oriental Orthodox observances resolve to their authored Gregorian dates.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("armenian-christmas-and-theophany", 1, 6)]
    [DataRow("oriental-orthodox-christmas-day", 1, 7)]
    [DataRow("oriental-orthodox-theophany", 1, 19)]
    [DataRow("coptic-new-year", 9, 11)]
    [DataRow("meskel", 9, 27)]
    public void Resolve_FixedOrientalObservance_YieldsAuthoredDate(string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, 2025);

        Assert.AreEqual(new DateOnly(2025, month, day), observance.Date, notableDateId);
    }

    /// <summary>
    /// Verifies that Palm Sunday, Easter Sunday and Pentecost always fall on a Sunday across a multi-decade span, a
    /// structural contract of the Pascha cluster's seven-day offsets.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_PaschaCluster_KeepsSundayFeastsOnSunday()
    {
        NotableDateService service = CreateService();

        for (int year = 2000; year <= 2050; year++)
        {
            foreach (string? id in new[] { "oriental-orthodox-palm-sunday", "oriental-orthodox-easter-sunday", "oriental-orthodox-pentecost" })
            {
                NotableDate feast = CommonCatalogues.ResolveSingle(service, id, year);
                Assert.AreEqual(DayOfWeek.Sunday, feast.Date.DayOfWeek, $"{id} {year} ({feast.Date:yyyy-MM-dd})");
            }
        }
    }
}
