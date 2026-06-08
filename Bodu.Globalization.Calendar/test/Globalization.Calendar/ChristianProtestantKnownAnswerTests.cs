// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChristianProtestantKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the floating Protestant observances authored in the bundled <c>christian-protestant</c> catalogue:
/// Transfiguration Sunday (the Last Sunday after Epiphany, authored as <see cref="OffsetFromRuleStrategy" /> 49 days
/// before the imported Western Easter anchor) and the two weekday-near Sundays, Reformation Sunday (the Sunday on or
/// before 31 October) and All Saints' Sunday (the first Sunday on or after 1 November). All three are deterministic;
/// expected dates are computed from the published Western Easter Sundays and the civil calendar for 2023-2027.
/// </summary>
[TestClass]
public sealed class ChristianProtestantKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the bundled <c>christian-protestant</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("christian-protestant");

    /// <summary>
    /// Verifies that Transfiguration Sunday resolves to 49 days before Western Easter (the Last Sunday after Epiphany)
    /// for each year 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 2, 19)] // Western Easter 9 Apr - 49
    [DataRow(2024, 2, 11)] // Western Easter 31 Mar - 49
    [DataRow(2025, 3, 2)]  // Western Easter 20 Apr - 49
    [DataRow(2026, 2, 15)] // Western Easter 5 Apr - 49
    [DataRow(2027, 2, 7)]  // Western Easter 28 Mar - 49
    public void Resolve_TransfigurationSunday_MatchesFortyNineDaysBeforeWesternEaster(int year, int month, int day)
    {
        NotableDate feast = CommonCatalogues.ResolveSingle(CreateService(), "transfiguration-sunday", year);

        Assert.AreEqual(new DateOnly(year, month, day), feast.Date, $"transfiguration-sunday {year}");
        Assert.AreEqual(DayOfWeek.Sunday, feast.Date.DayOfWeek, $"transfiguration-sunday {year} must be a Sunday");
    }

    /// <summary>
    /// Verifies that Reformation Sunday resolves to the Sunday on or before 31 October for each year 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 10, 29)]
    [DataRow(2024, 10, 27)]
    [DataRow(2025, 10, 26)]
    [DataRow(2026, 10, 25)]
    [DataRow(2027, 10, 31)] // 31 October 2027 is itself a Sunday.
    public void Resolve_ReformationSunday_MatchesSundayOnOrBefore31October(int year, int month, int day)
    {
        NotableDate feast = CommonCatalogues.ResolveSingle(CreateService(), "reformation-sunday", year);

        Assert.AreEqual(new DateOnly(year, month, day), feast.Date, $"reformation-sunday {year}");
        Assert.AreEqual(DayOfWeek.Sunday, feast.Date.DayOfWeek, $"reformation-sunday {year} must be a Sunday");
        Assert.IsLessThanOrEqualTo(new DateOnly(year, 10, 31), feast.Date, $"reformation-sunday {year} must fall on or before 31 October");
    }

    /// <summary>
    /// Verifies that All Saints' Sunday resolves to the first Sunday on or after 1 November for each year 2023-2027.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2023, 11, 5)]
    [DataRow(2024, 11, 3)]
    [DataRow(2025, 11, 2)]
    [DataRow(2026, 11, 1)] // 1 November 2026 is itself a Sunday.
    [DataRow(2027, 11, 7)]
    public void Resolve_AllSaintsSunday_MatchesFirstSundayOnOrAfter1November(int year, int month, int day)
    {
        NotableDate feast = CommonCatalogues.ResolveSingle(CreateService(), "all-saints-sunday", year);

        Assert.AreEqual(new DateOnly(year, month, day), feast.Date, $"all-saints-sunday {year}");
        Assert.AreEqual(DayOfWeek.Sunday, feast.Date.DayOfWeek, $"all-saints-sunday {year} must be a Sunday");
        Assert.IsGreaterThanOrEqualTo(new DateOnly(year, 11, 1), feast.Date, $"all-saints-sunday {year} must fall on or after 1 November");
    }

    /// <summary>
    /// Verifies that the fixed Protestant observances resolve to their authored Gregorian dates.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("reformation-day", 10, 31)]
    [DataRow("aldersgate-day", 5, 24)]
    public void Resolve_FixedProtestantObservance_YieldsAuthoredDate(string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, 2025);

        Assert.AreEqual(new DateOnly(2025, month, day), observance.Date, notableDateId);
    }
}
