// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePlannerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Verifies the static eligibility and candidate-year decisions made by <see cref="NotableDateRangePlanner.Plan" />.
/// These checks exercise the planner's territory, calendar, and filter eligibility logic which is otherwise hidden
/// behind a private helper.
/// </summary>
[TestClass]
public sealed class NotableDateRangePlannerTests
{
    /// <summary>
    /// Verifies that a rule whose <see cref="NotableDateRule.TerritoryCode" /> overlaps the requested territory is admitted to
    /// <see cref="NotableDateRangePlan.EligibleRules" />.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRuleTerritoryMatchesRequest_ShouldIncludeRule()
    {
        NotableDateRule rule = BuildFixedRule("Anzac Day", 4, 25, territory: "AU,NZ");
        NotableDateRangePlanner planner = BuildPlanner(rule);
        NotableDateRangeRequest request = new(
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 30),
            territoryCode: "AU");

        NotableDateRangePlan plan = planner.Plan(request);

        Assert.AreEqual(1, plan.EligibleRules.Count);
        Assert.AreEqual("Anzac Day", plan.EligibleRules[0].Rule.Name);
    }

    /// <summary>
    /// Verifies that a rule whose authored territory does not intersect the requested territory is excluded.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRuleTerritoryDoesNotMatchRequest_ShouldExcludeRule()
    {
        NotableDateRule rule = BuildFixedRule("Australia Day", 1, 26, territory: "AU");
        NotableDateRangePlanner planner = BuildPlanner(rule);
        NotableDateRangeRequest request = new(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            territoryCode: "US");

        NotableDateRangePlan plan = planner.Plan(request);

        Assert.AreEqual(0, plan.EligibleRules.Count);
    }

    /// <summary>
    /// Verifies that a rule with no authored territory matches any requested territory.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRuleTerritoryIsUnset_ShouldMatchAnyRequestedTerritory()
    {
        NotableDateRule rule = BuildFixedRule("Global Holiday", 5, 1);
        NotableDateRangePlanner planner = BuildPlanner(rule);
        NotableDateRangeRequest request = new(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            territoryCode: "FR");

        NotableDateRangePlan plan = planner.Plan(request);

        Assert.AreEqual(1, plan.EligibleRules.Count);
    }

    /// <summary>
    /// Verifies that the planner uses parent / child territory containment when matching — a rule scoped to <c>AU</c>
    /// applies when the request targets the subdivision <c>AU-NSW</c>.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRuleTerritoryContainsRequestedSubdivision_ShouldIncludeRule()
    {
        NotableDateRule rule = BuildFixedRule("Federal Holiday", 6, 1, territory: "AU");
        NotableDateRangePlanner planner = BuildPlanner(rule);
        NotableDateRangeRequest request = new(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 30),
            territoryCode: "AU-NSW");

        NotableDateRangePlan plan = planner.Plan(request);

        Assert.AreEqual(1, plan.EligibleRules.Count);
    }

    /// <summary>
    /// Verifies that a rule whose <see cref="NotableDateRule.CalendarType" /> conflicts with the requested calendar is excluded.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRuleCalendarConflictsWithRequest_ShouldExcludeRule()
    {
        NotableDateRule rule = BuildFixedRule(
            "Lunar Festival",
            1,
            1,
            calendarType: typeof(System.Globalization.ChineseLunisolarCalendar));
        NotableDateRangePlanner planner = BuildPlanner(rule);
        NotableDateRangeRequest request = new(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            calendarType: typeof(System.Globalization.HebrewCalendar));

        NotableDateRangePlan plan = planner.Plan(request);

        Assert.AreEqual(0, plan.EligibleRules.Count);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangePlan.CandidateYears" /> spans every civil year inside the request window.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRequestSpansMultipleYears_ShouldEnumerateEveryYearAsCandidate()
    {
        NotableDateRule rule = BuildFixedRule("Multi-Year", 7, 1);
        NotableDateRangePlanner planner = BuildPlanner(rule);
        NotableDateRangeRequest request = new(
            new DateTime(2024, 1, 1),
            new DateTime(2026, 12, 31));

        NotableDateRangePlan plan = planner.Plan(request);

        CollectionAssert.AreEqual(new[] { 2024, 2025, 2026 }, plan.CandidateYears.ToList());
    }

    /// <summary>
    /// Verifies that a request window starting in early January admits the previous civil year into
    /// <see cref="NotableDateRangePlan.FringeYears" /> for the fringe pass to scan.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRequestStartsNearYearBoundary_ShouldIncludePreviousYearAsFringe()
    {
        NotableDateRule rule = BuildFixedRule("Fringe-Eligible", 12, 31);
        NotableDateRangePlanner planner = BuildPlanner(rule);
        NotableDateRangeRequest request = new(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 5));

        NotableDateRangePlan plan = planner.Plan(request);

        CollectionAssert.Contains(plan.FringeYears.ToList(), 2025);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangePlanner" /> rejects a negative fringe distance during construction.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFringeDaysIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var analysis = RuleStaticAnalysis.Build(Array.Empty<NotableDateRule>());

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new NotableDateRangePlanner(analysis, fringeDays: -1);
        });

        Assert.AreEqual("fringeDays", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateRangePlanner.Plan" /> rejects a <see langword="null" /> request.
    /// </summary>
    [TestMethod]
    public void Plan_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        NotableDateRangePlanner planner = BuildPlanner();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = planner.Plan(null!);
        });
    }

    private static NotableDateRule BuildFixedRule(
        string name,
        int month,
        int day,
        string? territory = null,
        Type? calendarType = null) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            TerritoryCode = territory,
            CalendarType = calendarType,
        };

    private static NotableDateRangePlanner BuildPlanner(params NotableDateRule[] rules)
    {
        var analysis = RuleStaticAnalysis.Build(rules);
        return new NotableDateRangePlanner(analysis);
    }
}
