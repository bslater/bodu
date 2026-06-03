// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.V2;

namespace Bodu.Globalization.Calendar.V2.Data;

/// <summary>
/// Verifies that the migrated Europe resource pack resolves United Kingdom, French, and German holidays to their known
/// dates, exercising the British bank-holiday weekend roll and conflict-aware substitution, Easter-derived offsets, and
/// the continental no-substitution convention.
/// </summary>
[TestClass]
public sealed class EuropeCalendarDataTests
{
    /// <summary>
    /// Verifies that each European holiday resolves to its known emitted date and observed flag.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // United Kingdom: bank-holiday weekend roll, Easter offsets, Mothering Sunday, conflict-aware Christmas/Boxing.
    [DataRow("GB", 2023, "new-years-day", "2023-01-02", true)]
    [DataRow("GB", 2024, "good-friday", "2024-03-29", false)]
    [DataRow("GB", 2024, "mothering-sunday", "2024-03-10", false)]
    [DataRow("GB", 2024, "early-may-bank-holiday", "2024-05-06", false)]
    [DataRow("GB", 2024, "spring-bank-holiday", "2024-05-27", false)]
    [DataRow("GB-ENG", 2024, "summer-bank-holiday", "2024-08-26", false)]
    [DataRow("GB", 2021, "christmas-day", "2021-12-27", true)]
    [DataRow("GB", 2021, "boxing-day", "2021-12-28", true)]

    // France: Easter offsets and fixed nationals, no weekend substitution (Bastille Day on a Sunday stays put).
    [DataRow("FR", 2024, "easter-monday", "2024-04-01", false)]
    [DataRow("FR", 2024, "ascension-day", "2024-05-09", false)]
    [DataRow("FR", 2024, "whit-monday", "2024-05-20", false)]
    [DataRow("FR", 2024, "bastille-day", "2024-07-14", false)]
    [DataRow("FR", 2024, "armistice-day", "2024-11-11", false)]

    // Germany: Easter offsets, fixed nationals, and the weekday-near Repentance Day.
    [DataRow("DE", 2024, "ascension-day", "2024-05-09", false)]
    [DataRow("DE", 2024, "corpus-christi", "2024-05-30", false)]
    [DataRow("DE", 2024, "german-unity-day", "2024-10-03", false)]
    [DataRow("DE", 2024, "repentance-day", "2024-11-20", false)]
    public void Resolve_EuropeanHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        List<NotableDate> matches = EuropeCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");
        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), matches[0].Date, "emitted date");
        Assert.AreEqual(isObserved, matches[0].IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that a Scotland-scoped holiday does not resolve for an England query, confirming subdivision filtering.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenScottishHolidayQueriedForEngland_ReturnsNoResult()
    {
        int count = EuropeCalendarData.CreateService("GB-ENG")
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "GB-ENG")
            .Count(r => r.NotableDateId == "saint-andrews-day");

        Assert.AreEqual(0, count);
    }
}
