// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the algorithm-backed strategies against independently known dates: the Japanese equinox holidays, Qingming,
/// Vesak, the gazetted Matariki schedule, and the verified Hindu lunisolar festivals.
/// </summary>
[TestClass]
public sealed class AlgorithmKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the algorithm fixture.
    /// </summary>
    /// <returns>A service for the algorithm fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("algorithms.xml");

    /// <summary>
    /// Verifies that each algorithm-backed concept resolves to its known date for the requested year.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("jp-vernal-equinox", 2023, "2023-03-21")]
    [DataRow("jp-vernal-equinox", 2024, "2024-03-20")]
    [DataRow("jp-vernal-equinox", 2025, "2025-03-20")]
    [DataRow("jp-autumnal-equinox", 2023, "2023-09-23")]
    [DataRow("jp-autumnal-equinox", 2024, "2024-09-22")]
    [DataRow("jp-autumnal-equinox", 2025, "2025-09-23")]
    [DataRow("qingming", 2023, "2023-04-05")]
    [DataRow("qingming", 2024, "2024-04-04")]
    [DataRow("vesak", 2023, "2023-05-05")]
    [DataRow("vesak", 2024, "2024-05-23")]
    [DataRow("matariki", 2024, "2024-06-28")]
    [DataRow("matariki", 2025, "2025-06-20")]
    public void Resolve_AlgorithmStrategy_MatchesKnownAnswer(string notableDateId, int year, string expected)
    {
        DateOnly expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        List<NotableDate> matches = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "NZ")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {year}");
        Assert.AreEqual(expectedDate, matches[0].Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that a verified Hindu lunisolar festival resolves to within two days of its known date. The lunar-phase
    /// series is evaluated in Universal Time while the festival is observed in India Standard Time, so a one-day offset
    /// is expected and within the documented tolerance of the approximation.
    /// </summary>
    /// <param name="notableDateId">The festival id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The known observed date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("diwali", 2024, "2024-11-01")]
    [DataRow("holi", 2023, "2023-03-07")]
    [DataRow("navaratri", 2022, "2022-09-26")]
    public void Resolve_HinduFestival_IsWithinToleranceOfKnownDate(string notableDateId, int year, string expected)
    {
        DateOnly expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        List<NotableDate> matches = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "NZ")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {year}");

        int deltaDays = Math.Abs(matches[0].Date.DayNumber - expectedDate.DayNumber);
        Assert.IsTrue(deltaDays <= 2, $"{notableDateId} {year}: resolved {matches[0].Date}, expected within 2 days of {expectedDate}");
    }
}
