// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JainFestivalKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the bundled <c>global-jain</c> catalogue, whose observances are derived from the engine's solar-anchored
/// Hindu lunar algorithm. Mahavir Jayanti, Samvatsari, Jain Diwali and Kartik Purnima resolve to within two days of
/// their published dates; the festivals authored as exact offsets from those anchors (Paryushana, Das Lakshan) are
/// verified by their defining relationship and span.
/// </summary>
/// <remarks>
/// <para>
/// Maun Agiyaras is a known divergence: the festival is traditionally Margashirsha shukla ekadashi, but the catalogue
/// authors it as eleven days after Jain Diwali (Kartik shukla ekadashi), roughly a lunar month earlier. Its current
/// behaviour is pinned as a characterization below and recorded in the verification report rather than validated against
/// a published Maun Agiyaras date.
/// </para>
/// </remarks>
[TestClass]
public sealed class JainFestivalKnownAnswerTests
{
    /// <summary>
    /// The maximum tolerated difference, in days, between a resolved festival and its published date.
    /// </summary>
    private const int ToleranceDays = 2;

    /// <summary>
    /// Builds a service over the bundled <c>global-jain</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-jain");

    /// <summary>
    /// Verifies that the lunar-anchored Jain festivals resolve to within two days of their published dates.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The published Gregorian month.</param>
    /// <param name="day">The published Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, "mahavir-jayanti", 4, 4)]
    [DataRow(2024, "mahavir-jayanti", 4, 21)]
    [DataRow(2025, "mahavir-jayanti", 4, 10)]
    [DataRow(2023, "samvatsari", 9, 19)]
    [DataRow(2024, "samvatsari", 9, 7)]
    [DataRow(2025, "samvatsari", 8, 27)]
    [DataRow(2023, "jain-diwali", 11, 12)]
    [DataRow(2024, "jain-diwali", 11, 1)]
    [DataRow(2025, "jain-diwali", 10, 21)]
    [DataRow(2023, "kartik-purnima", 11, 27)]
    [DataRow(2024, "kartik-purnima", 11, 15)]
    [DataRow(2025, "kartik-purnima", 11, 5)]
    public void Resolve_JainFestival_IsWithinToleranceOfPublishedDate(int year, string notableDateId, int month, int day)
    {
        NotableDate festival = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);
        var reference = new DateOnly(year, month, day);
        var deltaDays = Math.Abs(festival.Date.DayNumber - reference.DayNumber);

        Assert.IsLessThanOrEqualTo(
            ToleranceDays,
            deltaDays,
            $"{notableDateId} {year}: resolved {festival.Date:yyyy-MM-dd}, expected within {ToleranceDays} days of {reference:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies the exact offset relationships the catalogue authors around Samvatsari: the eight-day Shvetambara
    /// Paryushana concludes on Samvatsari, and the ten-day Digambara Das Lakshan begins the day after it.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023)]
    [DataRow(2024)]
    [DataRow(2025)]
    public void Resolve_ParyushanaAndDasLakshan_TrackSamvatsari(int year)
    {
        NotableDateService service = CreateService();

        NotableDate samvatsari = CommonCatalogues.ResolveSingle(service, "samvatsari", year);
        NotableDate paryushana = CommonCatalogues.ResolveSingle(service, "paryushana-shvetambara", year);
        NotableDate dasLakshan = CommonCatalogues.ResolveSingle(service, "das-lakshan", year);

        Assert.AreEqual(samvatsari.Date.AddDays(-7), paryushana.Date, $"paryushana-shvetambara {year}");
        Assert.AreEqual(8, paryushana.DurationDays, $"paryushana-shvetambara {year} span");
        Assert.AreEqual(samvatsari.Date.AddDays(1), dasLakshan.Date, $"das-lakshan {year}");
        Assert.AreEqual(10, dasLakshan.DurationDays, $"das-lakshan {year} span");
    }

    /// <summary>
    /// Verifies that Mahavir Jayanti resolves to four days after the catalogue's Ram Navami anchor (Chaitra shukla
    /// navami to trayodashi), the defining tithi relationship of the festival.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023)]
    [DataRow(2024)]
    [DataRow(2025)]
    public void Resolve_MahavirJayanti_FallsFourDaysAfterRamNavamiAnchor(int year)
    {
        NotableDateService service = CreateService();

        NotableDate ramNavami = CommonCatalogues.ResolveSingle(service, "ram-navami-anchor", year);
        NotableDate mahavirJayanti = CommonCatalogues.ResolveSingle(service, "mahavir-jayanti", year);

        Assert.AreEqual(ramNavami.Date.AddDays(4), mahavirJayanti.Date, $"mahavir-jayanti {year}");
    }

    /// <summary>
    /// Documents the known Maun Agiyaras divergence: the catalogue currently resolves it to eleven days after Jain
    /// Diwali (Kartik shukla ekadashi), whereas the traditional festival is Margashirsha shukla ekadashi, roughly a
    /// lunar month later. This characterization pins the current behaviour so a future correction surfaces here; the
    /// divergence itself is tracked in the verification report.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023)]
    [DataRow(2024)]
    [DataRow(2025)]
    public void Resolve_MaunAgiyaras_CurrentlyTracksKartikEkadashi_KnownDivergence(int year)
    {
        NotableDateService service = CreateService();

        NotableDate jainDiwali = CommonCatalogues.ResolveSingle(service, "jain-diwali", year);
        NotableDate maunAgiyaras = CommonCatalogues.ResolveSingle(service, "maun-agiyaras", year);

        Assert.AreEqual(jainDiwali.Date.AddDays(11), maunAgiyaras.Date, $"maun-agiyaras {year} (current Kartik-ekadashi behaviour)");
    }
}
