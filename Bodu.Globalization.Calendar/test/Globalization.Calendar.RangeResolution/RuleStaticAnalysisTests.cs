// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleStaticAnalysisTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Verifies that <see cref="RuleStaticAnalysis" /> classifies rules into the correct processing tier and computes the day-delta
/// reach envelope used by the chronological range-resolution pipeline.
/// </summary>
[TestClass]
public sealed class RuleStaticAnalysisTests
{
    private static NotableDateRule Fixed(string name, int month, int day) => new()
    {
        Name = name,
        Strategy = DateResolutionStrategy.Fixed,
        Category = NotableDateCategory.Holiday,
        Month = month,
        Day = day,
    };

    private static NotableDateRule Algorithmic(string name, string algorithmKey) => new()
    {
        Name = name,
        Strategy = DateResolutionStrategy.Algorithm,
        Category = NotableDateCategory.Holiday,
        AlgorithmKey = algorithmKey,
    };

    private static NotableDateRule Offset(string name, string anchorRuleName, int offsetDays) => new()
    {
        Name = name,
        Strategy = DateResolutionStrategy.OffsetFromAnchor,
        Category = NotableDateCategory.Holiday,
        AnchorRuleName = anchorRuleName,
        OffsetDays = offsetDays,
    };

    /// <summary>
    /// Verifies that a fixed-date rule is classified as <see cref="RuleTier.Fixed" /> with no root anchor.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedRule_ShouldClassifyAsFixedTier()
    {
        var analysis = RuleStaticAnalysis.Build(new[] { Fixed("Christmas Day", 12, 25) });

        RuleStaticProfile profile = analysis.Profiles.Single();
        Assert.AreEqual(RuleTier.Fixed, profile.Tier);
        Assert.IsNull(profile.RootAnchorRuleName);
        Assert.AreEqual(0, profile.OffsetFromRoot);
    }

    /// <summary>
    /// Verifies that an algorithmic rule is classified as <see cref="RuleTier.Algorithmic" /> with itself as the root anchor.
    /// </summary>
    [TestMethod]
    public void Build_WhenAlgorithmicRule_ShouldClassifyAsAlgorithmicTierWithSelfAsRootAnchor()
    {
        var analysis = RuleStaticAnalysis.Build(new[] { Algorithmic("Easter Sunday", "easter-sunday") });

        RuleStaticProfile profile = analysis.Profiles.Single();
        Assert.AreEqual(RuleTier.Algorithmic, profile.Tier);
        Assert.AreEqual("Easter Sunday", profile.RootAnchorRuleName);
        Assert.AreEqual(0, profile.OffsetFromRoot);
    }

    /// <summary>
    /// Verifies that an offset rule whose anchor is fixed is classified as <see cref="RuleTier.OffsetFromFixed" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetRuleAnchoredToFixedRule_ShouldClassifyAsOffsetFromFixedTier()
    {
        NotableDateRule[] rules =
        {
            Fixed("Christmas Day", 12, 25),
            Offset("Boxing Day", "Christmas Day", 1),
        };

        var analysis = RuleStaticAnalysis.Build(rules);

        RuleStaticProfile boxing = analysis.Profiles.Single(p => p.Rule.Name == "Boxing Day");
        Assert.AreEqual(RuleTier.OffsetFromFixed, boxing.Tier);
        Assert.AreEqual("Christmas Day", boxing.RootAnchorRuleName);
        Assert.AreEqual(1, boxing.OffsetFromRoot);
    }

    /// <summary>
    /// Verifies that an offset rule whose anchor is algorithmic is classified as <see cref="RuleTier.OffsetFromAlgorithmic" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetRuleAnchoredToAlgorithmicRule_ShouldClassifyAsOffsetFromAlgorithmicTier()
    {
        NotableDateRule[] rules =
        {
            Algorithmic("Easter Sunday", "easter-sunday"),
            Offset("Start of Lent", "Easter Sunday", -46),
        };

        var analysis = RuleStaticAnalysis.Build(rules);

        RuleStaticProfile lent = analysis.Profiles.Single(p => p.Rule.Name == "Start of Lent");
        Assert.AreEqual(RuleTier.OffsetFromAlgorithmic, lent.Tier);
        Assert.AreEqual("Easter Sunday", lent.RootAnchorRuleName);
        Assert.AreEqual(-46, lent.OffsetFromRoot);
    }

    /// <summary>
    /// Verifies that a chain of offset rules collapses to the transitive root anchor and aggregate offset.
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetChainResolvesToAlgorithmicRoot_ShouldFlattenToRootAnchorAndAggregateOffset()
    {
        NotableDateRule[] rules =
        {
            Algorithmic("Easter Sunday", "easter-sunday"),
            Offset("Easter Monday", "Easter Sunday", 1),
            Offset("Day After Easter Monday", "Easter Monday", 1),
        };

        var analysis = RuleStaticAnalysis.Build(rules);

        RuleStaticProfile descendant = analysis.Profiles.Single(p => p.Rule.Name == "Day After Easter Monday");
        Assert.AreEqual(RuleTier.OffsetFromAlgorithmic, descendant.Tier);
        Assert.AreEqual("Easter Sunday", descendant.RootAnchorRuleName);
        Assert.AreEqual(2, descendant.OffsetFromRoot);
    }

    /// <summary>
    /// Verifies that <see cref="RuleStaticAnalysis.GetDependents" /> returns every rule whose root anchor is the supplied name.
    /// </summary>
    [TestMethod]
    public void GetDependents_WhenAnchorHasOffsetDescendants_ShouldReturnAllDescendants()
    {
        NotableDateRule[] rules =
        {
            Algorithmic("Easter Sunday", "easter-sunday"),
            Offset("Start of Lent", "Easter Sunday", -46),
            Offset("Palm Sunday", "Easter Sunday", -7),
            Offset("Easter Monday", "Easter Sunday", 1),
        };

        var analysis = RuleStaticAnalysis.Build(rules);

        IReadOnlyList<RuleStaticProfile> dependents = analysis.GetDependents("Easter Sunday");
        CollectionAssert.AreEquivalent(
            new[] { "Easter Sunday", "Start of Lent", "Palm Sunday", "Easter Monday" },
            dependents.Select(d => d.Rule.Name).ToArray());
    }

    /// <summary>
    /// Verifies that per-rule reach is captured on the profile so the pipeline can size fringe scans by individual adjustment
    /// behaviour rather than by a single global envelope.
    /// </summary>
    [TestMethod]
    public void Build_WhenRuleHasForwardRollingAdjustment_ShouldCapturePerRuleForwardReach()
    {
        NotableDateRule rollForward = Fixed("New Year's Day", 1, 1) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "weekend-roll",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
                IsNonWorkingDay = true,
            }),
        };

        NotableDateRule longSpan = Fixed("Festival Week", 6, 1) with
        {
            DurationDays = 7,
        };

        var analysis = RuleStaticAnalysis.Build(new[] { rollForward, longSpan });

        RuleStaticProfile rollProfile = analysis.Profiles.Single(p => p.Rule.Name == "New Year's Day");
        RuleStaticProfile spanProfile = analysis.Profiles.Single(p => p.Rule.Name == "Festival Week");

        // MoveToNextNonWorkingDay is estimated at +7 forward reach in this prototype.
        Assert.IsTrue(rollProfile.MaxObservedReach >= 7, $"Expected MaxObservedReach >= 7 for adjustment, got {rollProfile.MaxObservedReach}.");
        Assert.AreEqual(0, rollProfile.MinObservedReach);

        // Festival Week has no adjustment but a 7-day span so MaxObservedReach reflects DurationDays - 1.
        Assert.AreEqual(6, spanProfile.MaxObservedReach);
        Assert.AreEqual(0, spanProfile.MinObservedReach);
    }

    /// <summary>
    /// Verifies that the <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" /> property overrides the action's default reach
    /// estimate so an author-declared envelope flows into <see cref="RuleStaticAnalysis.GlobalFringeReach" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenAdjustmentDeclaresExplicitMaxReach_ShouldHonourItForGlobalFringeReach()
    {
        NotableDateRule custom = Fixed("Custom Reach Holiday", 7, 1) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "big-shift",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.Custom,
                HandlerKey = "test-handler",
                MaxAdjustmentReachDays = 90,
            }),
        };

        var analysis = RuleStaticAnalysis.Build(new[] { custom });

        Assert.AreEqual(90, analysis.GlobalFringeReach,
            "Author-declared MaxAdjustmentReachDays should set the analysis's global fringe reach.");

        RuleStaticProfile profile = analysis.Profiles.Single();
        Assert.AreEqual(90, profile.MaxObservedReach);
        Assert.AreEqual(-90, profile.MinObservedReach);
    }

    /// <summary>
    /// Verifies that an unconfigured custom adjustment falls back to the conservative ±31 day default envelope.
    /// </summary>
    [TestMethod]
    public void Build_WhenCustomAdjustmentDoesNotDeclareReach_ShouldUseDefaultEnvelope()
    {
        NotableDateRule custom = Fixed("Default Reach Holiday", 7, 1) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "default-shift",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.Custom,
                HandlerKey = "test-handler",
            }),
        };

        var analysis = RuleStaticAnalysis.Build(new[] { custom });

        Assert.AreEqual(31, analysis.GlobalFringeReach);
    }

    /// <summary>
    /// Verifies that an explicit reach declaration on a built-in action overrides the action's heuristic estimate so authors can
    /// declare a tighter envelope when they know the actual reach.
    /// </summary>
    [TestMethod]
    public void Build_WhenAddDaysActionHasExplicitTighterMaxReach_ShouldHonourTheDeclaration()
    {
        // AddDays with OffsetDays = 60 would heuristically yield (0, 60). The explicit MaxAdjustmentReachDays = 5 forces the
        // envelope to (-5, 5), which is what the author actually wants for fringe sizing.
        NotableDateRule misdeclared = Fixed("Bounded Shift", 7, 1) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "tight",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.AddDays,
                OffsetDays = 60,
                MaxAdjustmentReachDays = 5,
            }),
        };

        var analysis = RuleStaticAnalysis.Build(new[] { misdeclared });

        Assert.AreEqual(5, analysis.GlobalFringeReach);
    }

    /// <summary>
    /// Verifies that an offset rule referencing a missing anchor degrades safely to the <see cref="RuleTier.Fixed" /> tier so the
    /// analyser never throws on partially-authored rule sets.
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetRuleReferencesMissingAnchor_ShouldDegradeToFixedTier()
    {
        var analysis = RuleStaticAnalysis.Build(new[]
        {
            Offset("Orphaned", "Does Not Exist", 1),
        });

        RuleStaticProfile profile = analysis.Profiles.Single();
        Assert.AreEqual(RuleTier.Fixed, profile.Tier);
        Assert.IsNull(profile.RootAnchorRuleName);
    }

    /// <summary>
    /// Verifies that <see cref="RuleStaticAnalysis.Build" /> throws <see cref="ArgumentNullException" /> when the rule list is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Build_WhenRulesIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RuleStaticAnalysis.Build(null!);
        });

        Assert.AreEqual("rules", ex.ParamName);
    }
}
