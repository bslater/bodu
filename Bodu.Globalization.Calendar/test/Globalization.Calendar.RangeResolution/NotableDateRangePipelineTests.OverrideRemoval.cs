// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineTests.OverrideRemoval.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Verifies the behaviour of <see cref="NotableDateRangePipeline" /> when one or more <see cref="RuleRemoval" />
/// entries are configured via the constructor. These tests exercise the private <c>IsRemovedByOverride</c> branch
/// through the public <see cref="NotableDateRangePipeline.Resolve" /> entry point.
/// </summary>
[TestClass]
public sealed class NotableDateRangePipelineTestsOverrideRemoval
{
    /// <summary>
    /// Verifies that an unconditional removal suppresses a rule across every year and territory.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRemovalMatchesNameUnconditionally_ShouldSuppressRule()
    {
        NotableDateRule keep = BuildFixedRule("Keep", 1, 1);
        NotableDateRule drop = BuildFixedRule("Drop", 7, 4);

        NotableDateRangePipeline pipeline = BuildPipeline(
            rules: new[] { keep, drop },
            removals: new[] { new RuleRemoval("Drop") });

        IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31)));

        Assert.IsTrue(emitted.Any(n => n.Name == "Keep"), "Keep should remain.");
        Assert.IsFalse(emitted.Any(n => n.Name == "Drop"), "Drop should be suppressed by the removal.");
    }

    /// <summary>
    /// Verifies that a removal scoped by year range only suppresses the rule for years inside the range.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRemovalScopedByYearRange_ShouldOnlySuppressInsideRange()
    {
        NotableDateRule rule = BuildFixedRule("Scoped", 7, 4);

        NotableDateRangePipeline pipeline = BuildPipeline(
            rules: new[] { rule },
            removals: new[] { new RuleRemoval("Scoped", FromYear: 2025, ToYear: 2026) });

        IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
            new DateTime(2024, 1, 1),
            new DateTime(2027, 12, 31)));

        var emittedYears = emitted
            .Where(n => n.Name == "Scoped")
            .Select(n => n.Date.Year)
            .OrderBy(y => y)
            .ToList();

        CollectionAssert.AreEqual(new[] { 2024, 2027 }, emittedYears);
    }

    /// <summary>
    /// Verifies that a removal scoped by territory only suppresses entries whose territory falls inside the scope.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRemovalScopedByTerritory_ShouldOnlySuppressMatchingTerritory()
    {
        NotableDateRule rule = new()
        {
            Name = "Shared Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 4,
            Day = 25,
            TerritoryCode = "AU,NZ",
            IsNonWorkingDay = true,
        };

        NotableDateRangePipeline pipeline = BuildPipeline(
            rules: new[] { rule },
            removals: new[] { new RuleRemoval("Shared Holiday", TerritoryCode: "AU") });

        IReadOnlyList<NotableDate> emittedAu = pipeline.Resolve(new NotableDateRangeRequest(
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 30),
            territoryCode: "AU"));

        IReadOnlyList<NotableDate> emittedNz = pipeline.Resolve(new NotableDateRangeRequest(
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 30),
            territoryCode: "NZ"));

        Assert.IsFalse(emittedAu.Any(n => n.Name == "Shared Holiday"), "AU should be suppressed.");
        Assert.IsTrue(emittedNz.Any(n => n.Name == "Shared Holiday"), "NZ should still emit.");
    }

    /// <summary>
    /// Verifies that a removal whose <c>RuleName</c> does not match any base rule has no observable effect.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRemovalRuleNameDoesNotMatch_ShouldNotSuppressAnyRule()
    {
        NotableDateRule rule = BuildFixedRule("Live", 6, 1);

        NotableDateRangePipeline pipeline = BuildPipeline(
            rules: new[] { rule },
            removals: new[] { new RuleRemoval("Different") });

        IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 30)));

        Assert.IsTrue(emitted.Any(n => n.Name == "Live"));
    }

    /// <summary>
    /// Verifies that a removal scoped by territory does not suppress a territory-neutral rule, because the rule
    /// materialises without a territory and the removal requires a territory match.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenRemovalScopedByTerritoryButRuleIsTerritoryNeutral_ShouldNotSuppress()
    {
        NotableDateRule rule = BuildFixedRule("Global", 3, 3);

        NotableDateRangePipeline pipeline = BuildPipeline(
            rules: new[] { rule },
            removals: new[] { new RuleRemoval("Global", TerritoryCode: "AU") });

        IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31)));

        Assert.IsTrue(emitted.Any(n => n.Name == "Global"));
    }

    private static NotableDateRule BuildFixedRule(string name, int month, int day) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
        };

    private static NotableDateRangePipeline BuildPipeline(
        IReadOnlyList<NotableDateRule> rules,
        IReadOnlyList<RuleRemoval> removals)
    {
        var analysis = RuleStaticAnalysis.Build(rules);
        NotableDateRuleResolver resolver = new(rules);

        return new NotableDateRangePipeline(
            analysis,
            resolver,
            WeekPattern.Weekdays,
            handlerRegistry: null,
            overrideRemovals: removals);
    }
}
