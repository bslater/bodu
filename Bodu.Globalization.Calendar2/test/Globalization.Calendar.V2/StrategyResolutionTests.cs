// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that every calculation strategy resolves real notable dates to their known Gregorian dates through the v2
/// engine end to end (parse, validate, resolve).
/// </summary>
[TestClass]
public sealed class StrategyResolutionTests
{
    /// <summary>
    /// Builds a resolver over the strategies fixture, which exercises one real holiday per strategy family.
    /// </summary>
    /// <returns>A resolver for the strategies fixture.</returns>
    private static NotableDateService CreateResolver() =>
        NotableDateFixtures.Resolver("strategies.xml");

    /// <summary>
    /// Verifies that a fixed-date rule resolves United States Independence Day to 4 July.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Resolve_Fixed_ResolvesIndependenceDay()
    {
        AssertResolvesOn(new DateOnly(2026, 7, 4), "US", "us-independence-day", "us-fixed");
    }

    /// <summary>
    /// Verifies that a DayOfWeekInMonth rule resolves Thanksgiving to the fourth Thursday of November.
    /// </summary>
    [TestMethod]
    public void Resolve_DayOfWeekInMonth_ResolvesFourthThursdayThanksgiving()
    {
        AssertResolvesOn(new DateOnly(2024, 11, 28), "US", "us-thanksgiving", "us-4th-thu-nov");
        AssertResolvesOn(new DateOnly(2026, 11, 26), "US", "us-thanksgiving", "us-4th-thu-nov");
    }

    /// <summary>
    /// Verifies that a DayOfWeekInMonth rule resolves Labor Day to the first Monday of September.
    /// </summary>
    [TestMethod]
    public void Resolve_DayOfWeekInMonth_ResolvesFirstMondayLaborDay()
    {
        AssertResolvesOn(new DateOnly(2024, 9, 2), "US", "us-labor-day", "us-1st-mon-sep");
        AssertResolvesOn(new DateOnly(2026, 9, 7), "US", "us-labor-day", "us-1st-mon-sep");
    }

    /// <summary>
    /// Verifies that a DayOfWeekInMonth rule with the <c>Last</c> ordinal resolves Memorial Day to the last Monday of
    /// May.
    /// </summary>
    [TestMethod]
    public void Resolve_DayOfWeekInMonth_ResolvesLastMondayMemorialDay()
    {
        AssertResolvesOn(new DateOnly(2024, 5, 27), "US", "us-memorial-day", "us-last-mon-may");
        AssertResolvesOn(new DateOnly(2026, 5, 25), "US", "us-memorial-day", "us-last-mon-may");
    }

    /// <summary>
    /// Verifies that a DayOfWeekInMonth rule resolves Martin Luther King Jr. Day to the third Monday of January.
    /// </summary>
    [TestMethod]
    public void Resolve_DayOfWeekInMonth_ResolvesThirdMondayMlkDay()
    {
        AssertResolvesOn(new DateOnly(2024, 1, 15), "US", "us-mlk-day", "us-3rd-mon-jan");
        AssertResolvesOn(new DateOnly(2026, 1, 19), "US", "us-mlk-day", "us-3rd-mon-jan");
    }

    /// <summary>
    /// Verifies that a RelativeWeekdayInMonth rule resolves Election Day to the Tuesday after the first Monday of
    /// November.
    /// </summary>
    [TestMethod]
    public void Resolve_RelativeWeekdayInMonth_ResolvesElectionDay()
    {
        AssertResolvesOn(new DateOnly(2024, 11, 5), "US", "us-election-day", "us-tue-after-1st-mon-nov");
        AssertResolvesOn(new DateOnly(2026, 11, 3), "US", "us-election-day", "us-tue-after-1st-mon-nov");
    }

    /// <summary>
    /// Verifies that a WeekdayNearDate rule resolves Victoria Day to the Monday on or before 24 May.
    /// </summary>
    [TestMethod]
    public void Resolve_WeekdayNearDate_ResolvesVictoriaDay()
    {
        AssertResolvesOn(new DateOnly(2024, 5, 20), "CA", "ca-victoria-day", "ca-mon-onorbefore-may-24");
        AssertResolvesOn(new DateOnly(2026, 5, 18), "CA", "ca-victoria-day", "ca-mon-onorbefore-may-24");
    }

    /// <summary>
    /// Verifies that an algorithm rule resolves Western Easter Sunday to its known date.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Resolve_Algorithm_ResolvesWesternEaster()
    {
        AssertResolvesOn(new DateOnly(2024, 3, 31), "US", "easter-sunday", "western");
    }

    /// <summary>
    /// Verifies that an algorithm rule resolves Orthodox Easter Sunday to its known Gregorian date.
    /// </summary>
    [TestMethod]
    public void Resolve_Algorithm_ResolvesOrthodoxEaster()
    {
        AssertResolvesOn(new DateOnly(2024, 5, 5), "US", "orthodox-easter", "orthodox");
    }

    /// <summary>
    /// Verifies that an OffsetFromRule rule resolves Good Friday to two days before Western Easter.
    /// </summary>
    [TestMethod]
    public void Resolve_OffsetFromRule_ResolvesGoodFriday()
    {
        AssertResolvesOn(new DateOnly(2024, 3, 29), "US", "good-friday", "western-minus-2");
    }

    /// <summary>
    /// Verifies that an OffsetFromRule rule resolves Easter Monday to one day after Western Easter.
    /// </summary>
    [TestMethod]
    public void Resolve_OffsetFromRule_ResolvesEasterMonday()
    {
        AssertResolvesOn(new DateOnly(2024, 4, 1), "US", "easter-monday", "western-plus-1");
    }

    /// <summary>
    /// Verifies Western Easter Sunday across a multi-year table of published dates.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected Easter Sunday date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2020, "2020-04-12")]
    [DataRow(2021, "2021-04-04")]
    [DataRow(2022, "2022-04-17")]
    [DataRow(2023, "2023-04-09")]
    [DataRow(2024, "2024-03-31")]
    [DataRow(2025, "2025-04-20")]
    [DataRow(2026, "2026-04-05")]
    [DataRow(2027, "2027-03-28")]
    [DataRow(2028, "2028-04-16")]
    [DataRow(2029, "2029-04-01")]
    [DataRow(2030, "2030-04-21")]
    public void Resolve_Algorithm_WesternEaster_MatchesKnownTable(int year, string expected)
    {
        DateOnly date = DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        AssertResolvesOn(date, "US", "easter-sunday", "western");
    }

    /// <summary>
    /// Verifies Orthodox Easter Sunday (Gregorian date) across a multi-year table of published dates.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="expected">The expected Orthodox Easter Sunday Gregorian date in ISO format.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2022, "2022-04-24")]
    [DataRow(2023, "2023-04-16")]
    [DataRow(2024, "2024-05-05")]
    [DataRow(2025, "2025-04-20")]
    [DataRow(2026, "2026-04-12")]
    public void Resolve_Algorithm_OrthodoxEaster_MatchesKnownTable(int year, string expected)
    {
        DateOnly date = DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        AssertResolvesOn(date, "US", "orthodox-easter", "orthodox");
    }

    /// <summary>
    /// Asserts that the resolver returns the expected notable date and rule, emitted on the supplied date, for the
    /// requested territory.
    /// </summary>
    /// <param name="date">The expected emitted date.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="notableDateId">The expected notable-date id.</param>
    /// <param name="ruleId">The expected rule id.</param>
    private static void AssertResolvesOn(DateOnly date, string territory, string notableDateId, string ruleId)
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(date, territory);
        NotableDate? match = results.FirstOrDefault(r => r.NotableDateId == notableDateId);

        Assert.IsNotNull(match, $"Expected '{notableDateId}' on {date:yyyy-MM-dd} for {territory}.");
        Assert.AreEqual(date, match.Date, "emitted date");
        Assert.AreEqual(ruleId, match.RuleId, "rule id");
        Assert.IsFalse(match.IsObserved, "no adjustment expected");
    }
}
