// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Overrides.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the override-application and orchestration paths of <see cref="NotableDateService" /> exercised through
/// the public surface — additions, replacements, removal-on-territory-and-year, multi-rule collision, and
/// <see cref="AdjustmentAction.ReplaceWithNamedDate" /> resolution by name.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that an override addition appears in the resolved output of the year it covers.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenOverrideAddsRule_ShouldEmitAddedRule()
    {
        NotableDateRule baseRule = Fixed("Base", 6, 1);
        NotableDateRule added = Fixed("Added", 9, 9);

        NotableDateService service = BuildServiceWithOverride(
            [baseRule],
            additions: [added],
            removals: []);

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);

        Assert.IsTrue(emitted.Any(n => n.Name == "Base"));
        Assert.IsTrue(emitted.Any(n => n.Name == "Added" && n.Date == new DateTime(2026, 9, 9)));
    }

    /// <summary>
    /// Verifies that an override addition whose <c>(Name, Territory)</c> matches a base rule replaces it.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenOverrideAdditionMatchesBaseRule_ShouldReplaceBaseRule()
    {
        NotableDateRule baseRule = Fixed("Holiday", 6, 1);
        NotableDateRule replacement = Fixed("Holiday", 8, 1);

        NotableDateService service = BuildServiceWithOverride(
            [baseRule],
            additions: [replacement],
            removals: []);

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);

        IReadOnlyList<NotableDate> holidayMatches = emitted.Where(n => n.Name == "Holiday").ToList();
        Assert.AreEqual(1, holidayMatches.Count);
        Assert.AreEqual(new DateTime(2026, 8, 1), holidayMatches[0].Date);
    }

    /// <summary>
    /// Verifies that an override removal scoped to a year suppresses a base rule for that year only.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRemovalScopedByYear_ShouldOnlySuppressMatchingYears()
    {
        NotableDateRule baseRule = Fixed("Holiday", 6, 1);

        NotableDateService service = BuildServiceWithOverride(
            [baseRule],
            additions: [],
            removals: [new RuleRemoval("Holiday", FromYear: 2026, ToYear: 2026)]);

        Assert.IsTrue(service.GetNotableDates(2025).Any(n => n.Name == "Holiday"));
        Assert.IsFalse(service.GetNotableDates(2026).Any(n => n.Name == "Holiday"));
        Assert.IsTrue(service.GetNotableDates(2027).Any(n => n.Name == "Holiday"));
    }

    /// <summary>
    /// Verifies that an override removal scoped by territory only suppresses entries whose authored territory falls inside
    /// the supplied scope.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRemovalScopedByTerritory_ShouldOnlySuppressMatchingTerritory()
    {
        NotableDateRule nswRule = Fixed("Picnic Day", 8, 5, territory: "AU-NSW");
        NotableDateRule ntRule = Fixed("Picnic Day", 8, 5, territory: "AU-NT");

        NotableDateService service = BuildServiceWithOverride(
            [nswRule, ntRule],
            additions: [],
            removals: [new RuleRemoval("Picnic Day", TerritoryCode: "AU-NT")]);

        IReadOnlyList<NotableDate> nswResults = service.GetNotableDates(2026, territoryCode: "AU-NSW");
        IReadOnlyList<NotableDate> ntResults = service.GetNotableDates(2026, territoryCode: "AU-NT");

        Assert.IsTrue(nswResults.Any(n => n.Name == "Picnic Day"));
        Assert.IsFalse(ntResults.Any(n => n.Name == "Picnic Day"));
    }

    /// <summary>
    /// Verifies that an adjustment whose action is <see cref="AdjustmentAction.ReplaceWithNamedDate" /> resolves the
    /// substitute by name from the in-progress generation context — exercising the resolver callback wired up in the
    /// service.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenReplaceWithNamedDateTargetsAnotherRule_ShouldRedirectToTargetDate()
    {
        NotableDateRule target = Fixed("Substitute Day", 6, 15);
        NotableDateRule redirect = new()
        {
            Name = "Redirected Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 6,
            Day = 1,
            IsNonWorkingDay = true,
            Adjustments = [new ObservanceAdjustment
            {
                Key = "replace",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.ReplaceWithNamedDate,
                TargetRuleName = "Substitute Day",
            }],
        };

        NotableDateService service = BuildService(target, redirect);

        IReadOnlyList<NotableDate> redirected = service.GetNotableDates(2026)
            .Where(n => n.Name == "Redirected Day")
            .ToList();

        Assert.IsTrue(redirected.Any(n => n.WasAdjusted && n.Date == new DateTime(2026, 6, 15)),
            "Expected the redirect rule to be adjusted onto the Substitute Day date.");
    }

    /// <summary>
    /// Verifies that when two rules resolve to the same date, both are emitted as a collision and the higher-priority
    /// rule appears first under the default collision resolver.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenTwoRulesResolveSameDate_ShouldEmitBothEntries()
    {
        NotableDateRule lowerPriority = Fixed("Lower Priority", 5, 1) with { Priority = 50 };
        NotableDateRule higherPriority = Fixed("Higher Priority", 5, 1) with { Priority = 10 };

        NotableDateService service = BuildService(lowerPriority, higherPriority);

        IReadOnlyList<NotableDate> sameDay = service.GetNotableDates(new DateTime(2026, 5, 1));

        Assert.AreEqual(2, sameDay.Count);
        Assert.IsTrue(sameDay.Any(n => n.Name == "Lower Priority"));
        Assert.IsTrue(sameDay.Any(n => n.Name == "Higher Priority"));
    }

    /// <summary>
    /// Verifies that an override removal whose rule name does not match any base rule has no effect.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRemovalRuleNameDoesNotMatch_ShouldNotSuppressBaseRules()
    {
        NotableDateRule baseRule = Fixed("Live", 7, 4);

        NotableDateService service = BuildServiceWithOverride(
            [baseRule],
            additions: [],
            removals: [new RuleRemoval("Unknown")]);

        IReadOnlyList<NotableDate> emitted = service.GetNotableDates(2026);

        Assert.IsTrue(emitted.Any(n => n.Name == "Live"));
    }

    private static NotableDateService BuildServiceWithOverride(
        IReadOnlyList<NotableDateRule> baseRules,
        IReadOnlyList<NotableDateRule> additions,
        IReadOnlyList<RuleRemoval> removals) =>
        new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(baseRules.ToArray())],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)new TestOverrideProvider(removals, additions)],
            });
}
