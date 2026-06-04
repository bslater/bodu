// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UmmAlQuraCalendarKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the year-by-year known-answer table for the Saudi Umm al-Qura Islamic observances authored in
/// <c>umm-al-qura.xml</c>: each festival is a <see cref="FixedDateStrategy" /> expressed in the
/// <see cref="CalendarSystem.UmmAlQura" /> calendar with a calendar-year sweep, projected onto the Gregorian year
/// through the service. Expected dates come from the published Umm al-Qura calendar (ummulqura.org.sa) and are
/// reproduced by the base class library's <see cref="System.Globalization.UmAlQuraCalendar" />, ported from the v1
/// <c>global-islamic-umm-al-qura.xml</c> resource tests.
/// </summary>
[TestClass]
public sealed class UmmAlQuraCalendarKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the Umm al-Qura fixture.
    /// </summary>
    /// <returns>A service for the Umm al-Qura fixture.</returns>
    private static NotableDateService CreateService() =>
        NotableDateFixtures.Resolver("umm-al-qura.xml");

    /// <summary>
    /// Resolves the occurrences of a named observance whose anchor falls in the supplied Gregorian year.
    /// </summary>
    /// <param name="service">The service to resolve through.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The occurrences anchored in the requested year.</returns>
    private static List<NotableDate> ResolveForYear(NotableDateService service, string notableDateId, int year) =>
        service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .Where(r => r.NotableDateId == notableDateId && r.Date.Year == year)
            .ToList();

    /// <summary>
    /// Verifies that all six Umm al-Qura observances declared in <c>umm-al-qura.xml</c> resolve for a representative
    /// Gregorian year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_WhenLoadingUmmAlQura_ResolvesAllSixObservances()
    {
        NotableDateService service = CreateService();

        HashSet<string> ids = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Select(r => r.NotableDateId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsTrue(ids.Contains("ramadan"));
        Assert.IsTrue(ids.Contains("eid-al-fitr"));
        Assert.IsTrue(ids.Contains("eid-al-adha"));
        Assert.IsTrue(ids.Contains("islamic-new-year"));
        Assert.IsTrue(ids.Contains("day-of-ashura"));
        Assert.IsTrue(ids.Contains("mawlid-al-nabi"));
    }

    /// <summary>
    /// Verifies that each Umm al-Qura observance resolves to its Saudi-official Gregorian date for representative years
    /// (2023-2025, Hijri 1444-1447), reproduced by the base class library's
    /// <see cref="System.Globalization.UmAlQuraCalendar" />.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedMonth">The expected Gregorian month.</param>
    /// <param name="expectedDay">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // 2023 (Hijri 1444 / start of 1445)
    [DataRow(2023, "ramadan", 3, 23)]
    [DataRow(2023, "eid-al-fitr", 4, 21)]
    [DataRow(2023, "eid-al-adha", 6, 28)]
    [DataRow(2023, "islamic-new-year", 7, 19)]
    [DataRow(2023, "day-of-ashura", 7, 28)]
    [DataRow(2023, "mawlid-al-nabi", 9, 27)]

    // 2024 (Hijri 1445 / start of 1446)
    [DataRow(2024, "ramadan", 3, 11)]
    [DataRow(2024, "eid-al-fitr", 4, 10)]
    [DataRow(2024, "eid-al-adha", 6, 16)]
    [DataRow(2024, "islamic-new-year", 7, 7)]
    [DataRow(2024, "day-of-ashura", 7, 16)]
    [DataRow(2024, "mawlid-al-nabi", 9, 15)]

    // 2025 (Hijri 1446 / start of 1447)
    [DataRow(2025, "ramadan", 3, 1)]
    [DataRow(2025, "eid-al-fitr", 3, 30)]
    [DataRow(2025, "eid-al-adha", 6, 6)]
    [DataRow(2025, "islamic-new-year", 6, 26)]
    [DataRow(2025, "day-of-ashura", 7, 5)]
    [DataRow(2025, "mawlid-al-nabi", 9, 4)]
    public void Resolve_UmmAlQuraObservance_YieldsSaudiOfficialDate(int year, string notableDateId, int expectedMonth, int expectedDay)
    {
        List<NotableDate> matches = ResolveForYear(CreateService(), notableDateId, year);

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' anchored in {year}");
        Assert.AreEqual(new DateOnly(year, expectedMonth, expectedDay), matches[0].Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that the multi-day Umm al-Qura observances preserve their authored duration across the resolution
    /// pipeline.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedDuration">The expected authored duration in days.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("ramadan", 30)]
    [DataRow("eid-al-fitr", 3)]
    [DataRow("eid-al-adha", 4)]
    [DataRow("islamic-new-year", 1)]
    [DataRow("day-of-ashura", 1)]
    [DataRow("mawlid-al-nabi", 1)]
    public void Resolve_UmmAlQuraObservance_PreservesAuthoredDurationDays(string notableDateId, int expectedDuration)
    {
        List<NotableDate> matches = ResolveForYear(CreateService(), notableDateId, 2024);

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' anchored in 2024");
        Assert.AreEqual(expectedDuration, matches[0].DurationDays, notableDateId);
    }
}
