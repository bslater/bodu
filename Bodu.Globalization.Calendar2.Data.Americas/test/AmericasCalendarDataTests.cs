// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmericasCalendarDataTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.V2;

namespace Bodu.Globalization.Calendar.V2.Data;

/// <summary>
/// Verifies that the migrated Americas resource pack resolves United States and Canadian holidays to their known
/// dates, exercising weekend in-lieu observation, Easter-derived offsets, per-province subdivision rules, and the
/// conflict-aware Christmas/Boxing Day substitution.
/// </summary>
[TestClass]
public sealed class AmericasCalendarDataTests
{
    /// <summary>
    /// Verifies that each United States holiday resolves to its known emitted date and observed flag.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2026, "new-years-day", "2026-01-01", false)]
    [DataRow(2026, "mlk-day", "2026-01-19", false)]
    [DataRow(2024, "good-friday", "2024-03-29", false)]
    [DataRow(2024, "easter-sunday", "2024-03-31", false)]
    [DataRow(2024, "mothers-day", "2024-05-12", false)]
    [DataRow(2026, "memorial-day", "2026-05-25", false)]
    [DataRow(2021, "independence-day", "2021-07-05", true)]
    [DataRow(2026, "labor-day", "2026-09-07", false)]
    [DataRow(2024, "election-day", "2024-11-05", false)]
    [DataRow(2024, "black-friday", "2024-11-29", false)]
    [DataRow(2021, "christmas-day", "2021-12-24", true)]
    [DataRow(2022, "christmas-day", "2022-12-26", true)]
    public void Resolve_UnitedStatesHoliday_MatchesKnownAnswer(int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = Single("US", year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that each Canadian holiday resolves to its known emitted date and observed flag, including provincial
    /// rules and the conflict-aware Christmas/Boxing Day substitution.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    /// <param name="isObserved">Whether the emitted date is an in-lieu observation.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("CA", 2024, "new-years-day", "2024-01-01", false)]
    [DataRow("CA", 2024, "victoria-day", "2024-05-20", false)]
    [DataRow("CA", 2024, "canada-day", "2024-07-01", false)]
    [DataRow("CA", 2023, "canada-day", "2023-07-03", true)]
    [DataRow("CA", 2024, "thanksgiving", "2024-10-14", false)]
    [DataRow("CA", 2024, "easter-monday", "2024-04-01", false)]
    [DataRow("CA-ON", 2024, "family-day", "2024-02-19", false)]
    [DataRow("CA-QC", 2024, "fete-nationale-quebec", "2024-06-24", false)]
    [DataRow("CA", 2021, "christmas-day", "2021-12-27", true)]
    [DataRow("CA", 2021, "boxing-day", "2021-12-28", true)]

    // Restored by the entry-level migration audit: Christmas Eve (24 December), a working cultural observance.
    [DataRow("CA", 2024, "christmas-eve", "2024-12-24", false)]
    public void Resolve_CanadianHoliday_MatchesKnownAnswer(string territory, int year, string notableDateId, string expected, bool isObserved)
    {
        NotableDate match = Single(territory, year, notableDateId);

        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date, "emitted date");
        Assert.AreEqual(isObserved, match.IsObserved, "observed flag");
    }

    /// <summary>
    /// Verifies that a holiday introduced in a later year does not resolve before its first applicable year.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    [TestMethod]
    [DataRow("US", 2020, "juneteenth")]
    [DataRow("CA", 2020, "truth-and-reconciliation-day")]
    [DataRow("CA-ON", 2007, "family-day")]
    public void Resolve_WhenBeforeFirstYear_ReturnsNoResult(string territory, int year, string notableDateId)
    {
        int count = AmericasCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Count(r => r.NotableDateId == notableDateId);

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// Resolves a single year window for the requested territory and returns the one occurrence with the supplied id.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(string territory, int year, string notableDateId)
    {
        List<NotableDate> matches = AmericasCalendarData.CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.AreEqual(1, matches.Count, $"expected exactly one '{notableDateId}' for {territory} {year}");
        return matches[0];
    }
}
