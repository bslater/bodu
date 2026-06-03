// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AustraliaKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the ported Australian static notable-date definitions against the known answers from the v1 territory
/// catalogue: fixed holidays with weekend substitution, nth-weekday and weekday-near rules, Easter-derived dates,
/// per-subdivision rules, year-bound applicability, and the Christmas/Boxing Day conflict-avoiding substitution.
/// </summary>
[TestClass]
public sealed class AustraliaKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the ported Australian region fixture.
    /// </summary>
    /// <returns>A service for the Australian fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("region-au.xml");

    /// <summary>
    /// Verifies that the named Australian holiday resolves to its known date (and observed flag) for the requested
    /// territory and year.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an observed substitute.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("AU", 2026, "new-years-day", "2026-01-01", false)]
    [DataRow("AU", 2020, "australia-day", "2020-01-27", true)]
    [DataRow("AU", 2026, "anzac-day", "2026-04-25", false)]
    [DataRow("AU-VIC", 2026, "anzac-day", "2026-04-25", false)]
    [DataRow("AU", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("AU", 2024, "easter-monday", "2024-04-01", false)]
    [DataRow("AU", 2021, "christmas-day", "2021-12-27", true)]
    [DataRow("AU", 2021, "boxing-day", "2021-12-28", true)]
    [DataRow("AU-VIC", 2026, "labour-day", "2026-03-09", false)]
    [DataRow("AU-NSW", 2026, "labour-day", "2026-10-05", false)]
    [DataRow("AU-QLD", 2026, "labour-day", "2026-05-04", false)]
    [DataRow("AU-NSW", 2026, "kings-birthday", "2026-06-08", false)]
    [DataRow("AU-WA", 2026, "kings-birthday", "2026-09-28", false)]
    [DataRow("AU-VIC", 2026, "melbourne-cup-day", "2026-11-03", false)]
    [DataRow("AU-ACT", 2018, "reconciliation-day", "2018-05-28", false)]
    [DataRow("AU-ACT", 2019, "reconciliation-day", "2019-05-27", false)]
    [DataRow("AU-ACT", 2020, "reconciliation-day", "2020-06-01", false)]
    public void Resolve_AustralianHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        DateOnly expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        NotableDate match = SingleForYear(territory, year, notableDateId);

        Assert.AreEqual(expectedDate, match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that a holiday whose applicability begins in a later year does not resolve before that year. (ACT
    /// Reconciliation Day was introduced in 2018.)
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("AU-ACT", 2017, "reconciliation-day")]
    [DataRow("AU-QLD", 2015, "kings-birthday")]
    public void Resolve_WhenBeforeFirstYear_ReturnsNoResult(string territory, int year, string notableDateId)
    {
        int count = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Count(r => r.NotableDateId == notableDateId);

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Verifies that a national-only query does not pick up subdivision-scoped holidays. Labour Day is defined per
    /// state, so a plain <c>AU</c> query yields none.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNationalQuery_ExcludesSubdivisionHolidays()
    {
        int count = CreateService()
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "AU")
            .Count(r => r.NotableDateId == "labour-day");

        Assert.AreEqual(0, count, "Labour Day is subdivision-scoped");
    }

    /// <summary>
    /// Resolves a single year window for the requested territory and returns the one occurrence with the supplied
    /// notable-date id.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate SingleForYear(string territory, int year, string notableDateId)
    {
        List<NotableDate> matches = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");
        return matches[0];
    }
}
