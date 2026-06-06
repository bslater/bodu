// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BuddhistObservanceKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the bundled <c>global-buddhist</c> catalogue. The fixed observances (Parinirvana Day, Bodhi Day) resolve to
/// their authored dates; Vesak, the East Asian Buddha's Birthday, Tibetan Losar and Asalha Puja resolve to within two
/// days of their published dates, and Vassa follows Asalha Puja by a day.
/// </summary>
/// <remarks>
/// <para>
/// Losar is computed by <c>TibetanLosarCalculator</c> (the new moon nearest the Tibetan first-month solar point) and
/// Asalha Puja by the engine's leap-month-aware sidereal lunar calculator as the Asadha full moon, so both track the
/// Phukpa and Theravada lunisolar months across the leap-month divergences that an earlier fixed-window heuristic missed.
/// The engine evaluates the lunar phase in Universal Time, so Losar typically resolves to the day before the observed
/// Tibetan New Year, well within the two-day tolerance. The 2023 Asalha date follows the Indian Asadha full moon (Guru
/// Purnima); Thailand observed the leap-year date a lunation later that year.
/// </para>
/// </remarks>
[TestClass]
public sealed class BuddhistObservanceKnownAnswerTests
{
    /// <summary>
    /// The maximum tolerated difference, in days, between a resolved observance and its published date.
    /// </summary>
    private const int ToleranceDays = 2;

    /// <summary>
    /// Builds a service over the bundled <c>global-buddhist</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-buddhist");

    /// <summary>
    /// Verifies that the fixed Buddhist observances resolve to their authored Gregorian dates.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, "parinirvana-day", 2, 15)]
    [DataRow(2025, "parinirvana-day", 2, 15)]
    [DataRow(2024, "bodhi-day", 12, 8)]
    [DataRow(2025, "bodhi-day", 12, 8)]
    public void Resolve_FixedBuddhistObservance_YieldsAuthoredDate(int year, string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, month, day), observance.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that the computed Buddhist observances resolve to within two days of their published dates for 2023-2027:
    /// Vesak (Vaisakha full moon), the East Asian Buddha's Birthday (eighth day of the fourth Chinese lunisolar month),
    /// Tibetan Losar (Gyalpo Losar), and Asalha Puja (Asadha full moon).
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The published Gregorian month.</param>
    /// <param name="day">The published Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // Vesak (Buddha Purnima).
    [DataRow(2023, "vesak", 5, 5)]
    [DataRow(2024, "vesak", 5, 23)]
    [DataRow(2025, "vesak", 5, 12)]
    [DataRow(2026, "vesak", 5, 1)]
    [DataRow(2027, "vesak", 5, 20)]

    // East Asian Buddha's Birthday (Korea / Hong Kong observance).
    [DataRow(2023, "buddhas-birthday-east-asian", 5, 27)]
    [DataRow(2024, "buddhas-birthday-east-asian", 5, 15)]
    [DataRow(2025, "buddhas-birthday-east-asian", 5, 5)]
    [DataRow(2026, "buddhas-birthday-east-asian", 5, 24)]
    [DataRow(2027, "buddhas-birthday-east-asian", 5, 13)]

    // Tibetan New Year (Gyalpo Losar) — including the leap-divergence year 2027 (9 March).
    [DataRow(2023, "losar", 2, 21)]
    [DataRow(2024, "losar", 2, 10)]
    [DataRow(2025, "losar", 2, 28)]
    [DataRow(2026, "losar", 2, 18)]
    [DataRow(2027, "losar", 3, 9)]

    // Asalha Puja (Asadha full moon / Guru Purnima) — including the leap year 2026 (29 July).
    [DataRow(2023, "asalha-puja", 7, 3)]
    [DataRow(2024, "asalha-puja", 7, 21)]
    [DataRow(2025, "asalha-puja", 7, 10)]
    [DataRow(2026, "asalha-puja", 7, 29)]
    [DataRow(2027, "asalha-puja", 7, 18)]
    public void Resolve_ComputedBuddhistObservance_IsWithinToleranceOfPublishedDate(int year, string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);
        var reference = new DateOnly(year, month, day);
        var deltaDays = Math.Abs(observance.Date.DayNumber - reference.DayNumber);

        Assert.IsLessThanOrEqualTo(
            ToleranceDays,
            deltaDays,
            $"{notableDateId} {year}: resolved {observance.Date:yyyy-MM-dd}, expected within {ToleranceDays} days of {reference:yyyy-MM-dd}");
    }

    /// <summary>
    /// Verifies that Vassa (the Rains Retreat) begins the day after Asalha Puja and preserves its authored ninety-day
    /// span across 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023)]
    [DataRow(2024)]
    [DataRow(2025)]
    [DataRow(2026)]
    [DataRow(2027)]
    public void Resolve_Vassa_BeginsTheDayAfterAsalhaPuja(int year)
    {
        NotableDateService service = CreateService();

        NotableDate asalha = CommonCatalogues.ResolveSingle(service, "asalha-puja", year);
        NotableDate vassa = CommonCatalogues.ResolveSingle(service, "vassa", year);

        Assert.AreEqual(asalha.Date.AddDays(1), vassa.Date, $"vassa {year}");
        Assert.AreEqual(90, vassa.DurationDays, $"vassa {year} span");
    }

    /// <summary>
    /// Verifies that Losar always falls in February or early March across a multi-decade span, the contract of the
    /// Tibetan first-month new moon, confirming the calculator never selects a stray January or April lunation.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_LosarAcrossDecades_FallsInFebruaryOrEarlyMarch()
    {
        NotableDateService service = CreateService();

        for (var year = 2000; year <= 2050; year++)
        {
            NotableDate losar = CommonCatalogues.ResolveSingle(service, "losar", year);
            var inWindow = losar.Date.Month == 2 || (losar.Date.Month == 3 && losar.Date.Day <= 12);
            Assert.IsTrue(inWindow, $"losar {year} fell outside the February / early-March window: {losar.Date:yyyy-MM-dd}");
        }
    }
}
