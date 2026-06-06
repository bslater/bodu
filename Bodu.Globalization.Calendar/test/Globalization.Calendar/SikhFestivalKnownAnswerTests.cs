// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SikhFestivalKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the bundled <c>global-sikh</c> catalogue: the fixed Nanakshahi-style observances resolve to their authored
/// Gregorian dates, and the lunisolar-linked observances (Hola Mohalla, Bandi Chhor Divas, Guru Nanak Gurpurab), which
/// are derived from the engine's Hindu lunar algorithm, resolve to within two days of their published dates.
/// </summary>
/// <remarks>
/// <para>
/// The fixed civil dates follow the modern Nanakshahi convention. The Martyrdom of Guru Arjan Dev is authored at a fixed
/// 16 June; community calendars that follow the traditional Bikrami reckoning observe it on a date that varies by year
/// (for example around 10-11 June in 2024). That divergence is recorded in the verification report and can be overridden
/// by a community pack.
/// </para>
/// </remarks>
[TestClass]
public sealed class SikhFestivalKnownAnswerTests
{
    /// <summary>
    /// The maximum tolerated difference, in days, between a resolved lunisolar observance and its published date.
    /// </summary>
    private const int ToleranceDays = 2;

    /// <summary>
    /// Builds a service over the bundled <c>global-sikh</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-sikh");

    /// <summary>
    /// Verifies that the fixed Sikh observances resolve to their authored Gregorian dates.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, "guru-gobind-singh-jayanti", 1, 5)]
    [DataRow(2025, "guru-gobind-singh-jayanti", 1, 5)]
    [DataRow(2024, "vaisakhi", 4, 14)]
    [DataRow(2025, "vaisakhi", 4, 14)]
    [DataRow(2024, "martyrdom-of-guru-arjan-dev", 6, 16)]
    [DataRow(2024, "martyrdom-of-guru-tegh-bahadur", 11, 24)]
    [DataRow(2025, "martyrdom-of-guru-tegh-bahadur", 11, 24)]
    public void Resolve_FixedSikhObservance_YieldsAuthoredDate(int year, string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, month, day), observance.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that the lunisolar-linked Sikh observances resolve to within two days of their published dates.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The published Gregorian month.</param>
    /// <param name="day">The published Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2024, "hola-mohalla", 3, 25)]       // day after Holi (25 March 2024)
    [DataRow(2025, "hola-mohalla", 3, 14)]
    [DataRow(2023, "bandi-chhor-divas", 11, 12)] // Diwali
    [DataRow(2024, "bandi-chhor-divas", 11, 1)]
    [DataRow(2025, "bandi-chhor-divas", 10, 21)]
    [DataRow(2023, "guru-nanak-gurpurab", 11, 27)] // Kartik Purnima
    [DataRow(2024, "guru-nanak-gurpurab", 11, 15)]
    [DataRow(2025, "guru-nanak-gurpurab", 11, 5)]
    public void Resolve_LunisolarSikhObservance_IsWithinToleranceOfPublishedDate(int year, string notableDateId, int month, int day)
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
    /// Verifies that Hola Mohalla resolves to the day immediately after the catalogue's Holi anchor, the defining
    /// relationship of the observance, and preserves its authored three-day span.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023)]
    [DataRow(2024)]
    [DataRow(2025)]
    public void Resolve_HolaMohalla_FallsTheDayAfterTheHoliAnchor(int year)
    {
        NotableDateService service = CreateService();

        NotableDate holiAnchor = CommonCatalogues.ResolveSingle(service, "holi-anchor", year);
        NotableDate holaMohalla = CommonCatalogues.ResolveSingle(service, "hola-mohalla", year);

        Assert.AreEqual(holiAnchor.Date.AddDays(1), holaMohalla.Date, $"hola-mohalla {year}");
        Assert.AreEqual(3, holaMohalla.DurationDays, $"hola-mohalla {year} span");
    }
}
