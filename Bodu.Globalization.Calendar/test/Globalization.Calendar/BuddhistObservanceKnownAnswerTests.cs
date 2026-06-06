// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BuddhistObservanceKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the bundled <c>global-buddhist</c> catalogue. The fixed observances (Parinirvana Day, Bodhi Day) resolve to
/// their authored dates; Vesak, the East Asian Buddha's Birthday and the reliable years of Losar resolve to within two
/// days of their published dates. Asalha Puja, Vassa and two years of Losar are governed by fixed-window lunar
/// heuristics that select the wrong lunation in some years; those divergences are pinned as characterizations and are
/// recorded in detail in the verification report.
/// </summary>
/// <remarks>
/// <para>
/// Losar uses "first new moon on or after 20 January" and Asalha Puja uses "first full moon on or after 15 June". When a
/// new or full moon falls early in that window the heuristic selects the lunation a month before the observed festival
/// (for example Losar 2025 resolves to 29 January against an observed 28 February, and Asalha Puja 2024 resolves to
/// 22 June against an observed 21 July). The defect-documenting tests below assert the current output and the size of the
/// divergence so a future algorithm correction surfaces here.
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
    /// The minimum difference, in days, that constitutes the documented full-lunation divergence of the fixed-window
    /// heuristics.
    /// </summary>
    private const int LunationDivergenceDays = 20;

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
    /// Verifies that Vesak (the Vaisakha full moon) resolves to within two days of its published date for each year
    /// 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The published Gregorian month.</param>
    /// <param name="day">The published Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 5, 5)]
    [DataRow(2024, 5, 23)]
    [DataRow(2025, 5, 12)]
    [DataRow(2026, 5, 1)]
    [DataRow(2027, 5, 20)]
    public void Resolve_Vesak_IsWithinToleranceOfPublishedDate(int year, int month, int day)
    {
        AssertWithinTolerance(CreateService(), "vesak", year, new DateOnly(year, month, day));
    }

    /// <summary>
    /// Verifies that the East Asian Buddha's Birthday (eighth day of the fourth Chinese lunisolar month) resolves to
    /// within two days of its published date for each year 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The published Gregorian month.</param>
    /// <param name="day">The published Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 5, 27)]
    [DataRow(2024, 5, 15)]
    [DataRow(2025, 5, 5)]
    [DataRow(2026, 5, 24)]
    [DataRow(2027, 5, 13)]
    public void Resolve_BuddhasBirthdayEastAsian_IsWithinToleranceOfPublishedDate(int year, int month, int day)
    {
        AssertWithinTolerance(CreateService(), "buddhas-birthday-east-asian", year, new DateOnly(year, month, day));
    }

    /// <summary>
    /// Verifies that Losar resolves to within two days of the observed Tibetan New Year in the years the "first new moon
    /// on or after 20 January" heuristic selects the correct lunation (2024, 2026, 2027).
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The observed Gregorian month.</param>
    /// <param name="day">The observed Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, 2, 10)]
    [DataRow(2026, 2, 18)]
    [DataRow(2027, 2, 7)]
    public void Resolve_Losar_IsWithinToleranceOfObservedDate_WhenHeuristicHolds(int year, int month, int day)
    {
        AssertWithinTolerance(CreateService(), "losar", year, new DateOnly(year, month, day));
    }

    /// <summary>
    /// Verifies that Asalha Puja resolves to within two days of its observed date in 2025, the year the "first full moon
    /// on or after 15 June" heuristic selects the correct lunation, and that Vassa begins the following day.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_AsalhaPujaAndVassa_AreCorrect_For2025()
    {
        NotableDateService service = CreateService();

        AssertWithinTolerance(service, "asalha-puja", 2025, new DateOnly(2025, 7, 10));

        NotableDate asalha = CommonCatalogues.ResolveSingle(service, "asalha-puja", 2025);
        NotableDate vassa = CommonCatalogues.ResolveSingle(service, "vassa", 2025);
        Assert.AreEqual(asalha.Date.AddDays(1), vassa.Date, "vassa 2025 begins the day after Asalha Puja");
        Assert.AreEqual(90, vassa.DurationDays, "vassa 2025 span");
    }

    /// <summary>
    /// Documents the known Losar defect: in years where a new moon falls in late January the heuristic selects the
    /// lunation a month before the observed Tibetan New Year. The current output and the size of the divergence are
    /// pinned so a future correction of the algorithm surfaces here.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="resolvedMonth">The current resolved month.</param>
    /// <param name="resolvedDay">The current resolved day.</param>
    /// <param name="observedMonth">The observed Tibetan New Year month.</param>
    /// <param name="observedDay">The observed Tibetan New Year day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 1, 21, 2, 21)]
    [DataRow(2025, 1, 29, 2, 28)]
    public void Resolve_Losar_DivergesByAFullLunation_KnownDefect(int year, int resolvedMonth, int resolvedDay, int observedMonth, int observedDay)
    {
        NotableDate losar = CommonCatalogues.ResolveSingle(CreateService(), "losar", year);

        Assert.AreEqual(new DateOnly(year, resolvedMonth, resolvedDay), losar.Date, $"losar {year} current output");
        var deltaDays = Math.Abs(losar.Date.DayNumber - new DateOnly(year, observedMonth, observedDay).DayNumber);
        Assert.IsGreaterThanOrEqualTo(LunationDivergenceDays, deltaDays, $"losar {year} divergence from observed");
    }

    /// <summary>
    /// Documents the known Asalha Puja defect: in years where a full moon falls in the second half of June the heuristic
    /// selects the lunation a month before the observed Asalha full moon, which also carries Vassa a month early. The
    /// current output and the size of the divergence are pinned so a future correction surfaces here.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="resolvedMonth">The current resolved month.</param>
    /// <param name="resolvedDay">The current resolved day.</param>
    /// <param name="observedMonth">The observed Asalha Puja month.</param>
    /// <param name="observedDay">The observed Asalha Puja day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, 6, 22, 7, 21)]
    [DataRow(2026, 6, 29, 7, 29)]
    [DataRow(2027, 6, 19, 7, 19)]
    public void Resolve_AsalhaPuja_DivergesByAFullLunation_KnownDefect(int year, int resolvedMonth, int resolvedDay, int observedMonth, int observedDay)
    {
        NotableDate asalha = CommonCatalogues.ResolveSingle(CreateService(), "asalha-puja", year);

        Assert.AreEqual(new DateOnly(year, resolvedMonth, resolvedDay), asalha.Date, $"asalha-puja {year} current output");
        var deltaDays = Math.Abs(asalha.Date.DayNumber - new DateOnly(year, observedMonth, observedDay).DayNumber);
        Assert.IsGreaterThanOrEqualTo(LunationDivergenceDays, deltaDays, $"asalha-puja {year} divergence from observed");
    }

    /// <summary>
    /// Asserts that a named observance resolves to within <see cref="ToleranceDays" /> of a reference date.
    /// </summary>
    /// <param name="service">The service to resolve through.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="reference">The published reference date.</param>
    private static void AssertWithinTolerance(NotableDateService service, string notableDateId, int year, DateOnly reference)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(service, notableDateId, year);
        var deltaDays = Math.Abs(observance.Date.DayNumber - reference.DayNumber);

        Assert.IsLessThanOrEqualTo(
            ToleranceDays,
            deltaDays,
            $"{notableDateId} {year}: resolved {observance.Date:yyyy-MM-dd}, expected within {ToleranceDays} days of {reference:yyyy-MM-dd}");
    }
}
