// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

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
        var expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        var matches = CreateService()
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
        var expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);

        var matches = CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "NZ")
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {year}");

        var deltaDays = Math.Abs(matches[0].Date.DayNumber - expectedDate.DayNumber);
        Assert.IsTrue(deltaDays <= 2, $"{notableDateId} {year}: resolved {matches[0].Date}, expected within 2 days of {expectedDate}");
    }

    /// <summary>
    /// Verifies that every supported Hindu festival resolves to within two days of its published date across several
    /// years, including the 2023 intercalary (adhika maasa) year, confirming the solar-anchored month selection.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_EveryHinduFestival_IsWithinToleranceAcrossYears()
    {
        (int Year, string Id, string Expected)[] knownAnswers =
        {
            (2024, "ram-navami", "2024-04-17"), (2024, "vasant-panchami", "2024-02-14"),
            (2024, "maha-shivaratri", "2024-03-08"), (2024, "holi", "2024-03-25"),
            (2024, "raksha-bandhan", "2024-08-19"), (2024, "janmashtami", "2024-08-26"),
            (2024, "ganesh-chaturthi", "2024-09-07"), (2024, "navaratri", "2024-10-03"),
            (2024, "dussehra", "2024-10-12"), (2024, "karva-chauth", "2024-10-20"),
            (2024, "diwali", "2024-11-01"),
            (2025, "vasant-panchami", "2025-02-02"), (2025, "maha-shivaratri", "2025-02-26"),
            (2025, "holi", "2025-03-14"), (2025, "ram-navami", "2025-04-06"),
            (2025, "raksha-bandhan", "2025-08-09"), (2025, "janmashtami", "2025-08-16"),
            (2025, "navaratri", "2025-09-22"), (2025, "dussehra", "2025-10-02"),
            (2025, "diwali", "2025-10-21"),
            (2023, "holi", "2023-03-08"), (2023, "raksha-bandhan", "2023-08-31"),
            (2023, "janmashtami", "2023-09-07"), (2023, "dussehra", "2023-10-24"),
            (2023, "diwali", "2023-11-12"),
        };

        NotableDateService service = CreateService();
        List<string> failures = new();

        foreach ((var year, var id, var expected) in knownAnswers)
        {
            var expectedDate = DateOnly.Parse(expected, CultureInfo.InvariantCulture);
            var matches = service
                .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "NZ")
                .Where(r => r.NotableDateId == id)
                .ToList();

            if (matches.Count != 1)
            {
                failures.Add($"{id} {year}: expected one result, found {matches.Count}");
                continue;
            }

            var deltaDays = Math.Abs(matches[0].Date.DayNumber - expectedDate.DayNumber);
            if (deltaDays > 2)
                failures.Add($"{id} {year}: resolved {matches[0].Date}, expected within 2 days of {expectedDate} (off by {deltaDays})");
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures));
    }
}
