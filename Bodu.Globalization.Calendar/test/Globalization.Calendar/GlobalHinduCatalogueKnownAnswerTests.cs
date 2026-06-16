// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalHinduCatalogueKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the bundled <c>global-hindu</c> catalogue: the solar harvest festivals added in the latest pass (Makar
/// Sankranti and Pongal, fixed at 14 January by civil convention), that Saraswati Puja shares the Vasant Panchami
/// computation, and that the solar-anchored lunar festivals resolve to within two days of their published dates. The
/// underlying Hindu lunar algorithm is documented as accurate to within a day or two of regional panchanga reckoning.
/// </summary>
[TestClass]
public sealed class GlobalHinduCatalogueKnownAnswerTests
{
    /// <summary>
    /// The maximum tolerated difference, in days, between a resolved lunar festival and its published date.
    /// </summary>
    private const int ToleranceDays = 2;

    /// <summary>
    /// Builds a service over the bundled <c>global-hindu</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-hindu");

    /// <summary>
    /// Verifies that the solar harvest festivals Makar Sankranti and Pongal resolve to their fixed 14 January date.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, "makar-sankranti")]
    [DataRow(2024, "makar-sankranti")]
    [DataRow(2025, "makar-sankranti")]
    [DataRow(2024, "pongal")]
    [DataRow(2025, "pongal")]
    public void Resolve_SolarHarvestFestival_FallsOn14January(int year, string notableDateId)
    {
        NotableDate festival = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, 1, 14), festival.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that Saraswati Puja resolves to the same date as Vasant Panchami, since the catalogue computes both from
    /// the <c>vasant-panchami</c> algorithm.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023)]
    [DataRow(2024)]
    [DataRow(2025)]
    public void Resolve_SaraswatiPuja_CoincidesWithVasantPanchami(int year)
    {
        NotableDateService service = CreateService();

        NotableDate saraswati = CommonCatalogues.ResolveSingle(service, "saraswati-puja", year);
        NotableDate vasant = CommonCatalogues.ResolveSingle(service, "vasant-panchami", year);

        Assert.AreEqual(vasant.Date, saraswati.Date, $"saraswati-puja vs vasant-panchami {year}");
    }

    /// <summary>
    /// Verifies that representative solar-anchored lunar festivals resolve to within two days of their published dates,
    /// confirming the catalogue wires each algorithm key correctly.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The published Gregorian month.</param>
    /// <param name="day">The published Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, "vasant-panchami", 2, 14)]
    [DataRow(2025, "vasant-panchami", 2, 2)]
    [DataRow(2024, "holi", 3, 25)]
    [DataRow(2025, "holi", 3, 14)]
    [DataRow(2024, "ganesh-chaturthi", 9, 7)]
    [DataRow(2024, "dussehra", 10, 12)]
    [DataRow(2024, "diwali", 11, 1)]
    [DataRow(2025, "diwali", 10, 21)]
    public void Resolve_HinduLunarFestival_IsWithinToleranceOfPublishedDate(int year, string notableDateId, int month, int day)
    {
        NotableDate festival = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);
        var reference = new DateOnly(year, month, day);
        int deltaDays = Math.Abs(festival.Date.DayNumber - reference.DayNumber);

        Assert.IsLessThanOrEqualTo(
            ToleranceDays,
            deltaDays,
            $"{notableDateId} {year}: resolved {festival.Date:yyyy-MM-dd}, expected within {ToleranceDays} days of {reference:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies that the multi-day Hindu festivals preserve their authored duration across the resolution pipeline.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedDuration">The expected authored duration in days.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("pongal", 4)]
    [DataRow("holi", 2)]
    [DataRow("ganesh-chaturthi", 10)]
    [DataRow("navaratri", 9)]
    [DataRow("diwali", 5)]
    public void Resolve_HinduFestival_PreservesAuthoredDurationDays(string notableDateId, int expectedDuration)
    {
        NotableDate festival = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, 2024);

        Assert.AreEqual(expectedDuration, festival.DurationDays, notableDateId);
    }
}
