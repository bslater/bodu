// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedStatesKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the ported United States federal static notable-date definitions against known answers: the nth-weekday
/// holidays, fixed holidays, and the in-lieu weekend observation (Saturday on the preceding Friday, Sunday on the
/// following Monday).
/// </summary>
[TestClass]
public sealed class UnitedStatesKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the ported United States federal fixture.
    /// </summary>
    /// <returns>A service for the United States fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("region-us.xml");

    /// <summary>
    /// Verifies that each United States federal holiday resolves to its known date (and observed flag) for the
    /// requested year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2026, "new-years-day", "2026-01-01", false)]
    [DataRow(2026, "mlk-day", "2026-01-19", false)]
    [DataRow(2026, "washingtons-birthday", "2026-02-16", false)]
    [DataRow(2026, "memorial-day", "2026-05-25", false)]
    [DataRow(2026, "juneteenth", "2026-06-19", false)]
    [DataRow(2026, "independence-day", "2026-07-03", true)]
    [DataRow(2026, "labor-day", "2026-09-07", false)]
    [DataRow(2026, "columbus-day", "2026-10-12", false)]
    [DataRow(2026, "veterans-day", "2026-11-11", false)]
    [DataRow(2026, "thanksgiving", "2026-11-26", false)]
    [DataRow(2026, "christmas-day", "2026-12-25", false)]
    [DataRow(2021, "independence-day", "2021-07-05", true)]
    [DataRow(2021, "christmas-day", "2021-12-24", true)]
    [DataRow(2022, "christmas-day", "2022-12-26", true)]
    public void Resolve_FederalHoliday_MatchesKnownAnswer(int year, string notableDateId, string expected, bool isObserved)
    {
        var expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        var matches = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "US")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {year}");
        Assert.AreEqual(expectedDate, matches[0].Date, "emitted date");
        Assert.AreEqual(isObserved, matches[0].IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that Juneteenth, a federal holiday only from 2021, does not resolve in earlier years.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenJuneteenthBefore2021_ReturnsNoResult()
    {
        var count = CreateService()
            .Resolve(new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31)), "US")
            .Count(r => r.NotableDateId == "juneteenth");

        Assert.AreEqual(0, count);
    }
}
