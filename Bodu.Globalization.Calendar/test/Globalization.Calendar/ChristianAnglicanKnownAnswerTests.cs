// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChristianAnglicanKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the Anglican feasts authored in the bundled <c>christian-anglican</c> catalogue. The single floating concept
/// is the Baptism of Christ (the first Sunday strictly after Epiphany, 6 January); the remainder are fixed-date apostle,
/// evangelist and calendar feasts that follow the Book of Common Prayer (1662) calendar.
/// </summary>
/// <remarks>
/// <para>
/// Two fixed dates reflect the BCP position rather than the modern Common Worship calendar: Matthias the Apostle is kept
/// on 24 February (Common Worship transfers it to 14 May) and Thomas the Apostle on 21 December (Common Worship transfers
/// it to 3 July). Both choices are recorded in the verification report; a province pack can override either.
/// </para>
/// </remarks>
[TestClass]
public sealed class ChristianAnglicanKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the bundled <c>christian-anglican</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("christian-anglican");

    /// <summary>
    /// Verifies that the Baptism of Christ resolves to the first Sunday strictly after Epiphany (6 January) for each year
    /// 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 1, 8)]
    [DataRow(2024, 1, 7)]
    [DataRow(2025, 1, 12)]
    [DataRow(2026, 1, 11)]
    [DataRow(2027, 1, 10)]
    public void Resolve_BaptismOfChrist_MatchesSundayAfterEpiphany(int year, int month, int day)
    {
        NotableDate feast = CommonCatalogues.ResolveSingle(CreateService(), "baptism-of-christ", year);

        Assert.AreEqual(new DateOnly(year, month, day), feast.Date, $"baptism-of-christ {year}");
        Assert.AreEqual(DayOfWeek.Sunday, feast.Date.DayOfWeek, $"baptism-of-christ {year} must be a Sunday");
        Assert.IsGreaterThan(new DateOnly(year, 1, 6), feast.Date, $"baptism-of-christ {year} must fall after Epiphany");
    }

    /// <summary>
    /// Verifies that every fixed Anglican feast resolves to its authored Gregorian date (validated for 2025).
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("naming-and-circumcision-of-jesus", 1, 1)]
    [DataRow("conversion-of-saint-paul", 1, 25)]
    [DataRow("matthias-the-apostle", 2, 24)]       // BCP position; Common Worship 14 May.
    [DataRow("joseph-of-nazareth", 3, 19)]
    [DataRow("mark-the-evangelist", 4, 25)]
    [DataRow("philip-and-james-apostles", 5, 1)]
    [DataRow("barnabas-the-apostle", 6, 11)]
    [DataRow("john-the-baptist", 6, 24)]
    [DataRow("peter-and-paul-apostles", 6, 29)]
    [DataRow("mary-magdalene", 7, 22)]
    [DataRow("james-the-apostle", 7, 25)]
    [DataRow("mary-mother-of-our-lord", 8, 15)]
    [DataRow("bartholomew-the-apostle", 8, 24)]
    [DataRow("matthew-apostle-and-evangelist", 9, 21)]
    [DataRow("michael-and-all-angels", 9, 29)]
    [DataRow("luke-the-evangelist", 10, 18)]
    [DataRow("simon-and-jude-apostles", 10, 28)]
    [DataRow("andrew-the-apostle", 11, 30)]
    [DataRow("thomas-the-apostle", 12, 21)]         // BCP position; Common Worship 3 July.
    [DataRow("stephen-deacon-and-first-martyr", 12, 26)]
    [DataRow("john-apostle-and-evangelist", 12, 27)]
    [DataRow("holy-innocents", 12, 28)]
    public void Resolve_FixedAnglicanFeast_YieldsAuthoredDate(string notableDateId, int month, int day)
    {
        NotableDate feast = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, 2025);

        Assert.AreEqual(new DateOnly(2025, month, day), feast.Date, notableDateId);
    }
}
