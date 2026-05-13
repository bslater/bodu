// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Clone.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.Clone" /> returns a different instance.
    /// </summary>
    [TestMethod]
    public void Clone_WhenInvoked_ShouldReturnIndependentInstance()
    {
        NotableDateDocumentBuilder original = NotableDateDocumentBuilder.Create()
            .AddDate("New Year's Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(1, 1)));

        NotableDateDocumentBuilder copy = original.Clone();

        Assert.AreNotSame(original, copy);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateDocumentBuilder.Clone" /> produces a copy with the same rules as the
    /// source at the moment of cloning.
    /// </summary>
    [TestMethod]
    public void Clone_WhenSourceHasOneDate_ShouldYieldCopyWithEquivalentRule()
    {
        NotableDateDocumentBuilder original = NotableDateDocumentBuilder.Create()
            .AddDate("New Year's Day", date => date
                .AddRule(rule => rule
                    .Category(NotableDateCategory.Holiday)
                    .NonWorking()
                    .AddTag("Federal")
                    .Fixed(1, 1)));

        NotableDateDocumentBuilder copy = original.Clone();

        IReadOnlyList<NotableDateRule> originalRules = original.Build();
        IReadOnlyList<NotableDateRule> copyRules = copy.Build();

        Assert.AreEqual(originalRules.Count, copyRules.Count);
        Assert.AreEqual(originalRules[0].Name, copyRules[0].Name);
        Assert.AreEqual(originalRules[0].Strategy, copyRules[0].Strategy);
        Assert.AreEqual(originalRules[0].Month, copyRules[0].Month);
        Assert.AreEqual(originalRules[0].Day, copyRules[0].Day);
        Assert.AreEqual(originalRules[0].IsNonWorkingDay, copyRules[0].IsNonWorkingDay);
        CollectionAssert.AreEquivalent(originalRules[0].Tags.ToList(), copyRules[0].Tags.ToList());
    }

    /// <summary>
    /// Verifies that adding a notable date to the cloned document builder does not leak back into the source.
    /// </summary>
    [TestMethod]
    public void Clone_WhenCloneAddsDate_ShouldNotMutateSource()
    {
        NotableDateDocumentBuilder original = NotableDateDocumentBuilder.Create()
            .AddDate("New Year's Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(1, 1)));

        NotableDateDocumentBuilder copy = original.Clone()
            .AddDate("Christmas Day", date => date
                .AddRule(rule => rule.Category(NotableDateCategory.Holiday).Fixed(12, 25)));

        Assert.AreEqual(1, original.Build().Count);
        Assert.AreEqual(2, copy.Build().Count);
    }

    /// <summary>
    /// Verifies the template-factory pattern end-to-end: a configured rule builder is cloned per variant and
    /// each variant gets its own strategy without disturbing the shared template configuration.
    /// </summary>
    [TestMethod]
    public void Clone_TemplateFactoryWorkflow_ShouldProduceIndependentRuleVariants()
    {
        // Template: shared "Holiday + NonWorking + weekend-roll adjustment" configuration with the strategy
        // intentionally left unset so each variant can author its own.
        NotableDateRuleBuilder template = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .NonWorking()
            .AddAdjustment("weekend-roll", adj => adj
                .When(AdjustmentTrigger.IfWeekend)
                .Action(AdjustmentAction.MoveToNextWeekday));

        NotableDateRule christmas = template.Clone()
            .RuleName("Christmas Day Fixed")
            .Fixed(12, 25)
            .Build("Christmas Day");

        NotableDateRule newYears = template.Clone()
            .RuleName("New Year's Day Fixed")
            .Fixed(1, 1)
            .Build("New Year's Day");

        NotableDateRule australiaDay = template.Clone()
            .RuleName("Australia Day Fixed")
            .Territory("AU")
            .AddTag("Federal")
            .Fixed(1, 26)
            .Build("Australia Day");

        // Each variant got its own strategy and per-variant tweaks…
        Assert.AreEqual(12, christmas.Month);
        Assert.AreEqual(25, christmas.Day);
        Assert.AreEqual(1, newYears.Month);
        Assert.AreEqual(1, newYears.Day);
        Assert.AreEqual(1, australiaDay.Month);
        Assert.AreEqual(26, australiaDay.Day);
        Assert.AreEqual("AU", australiaDay.TerritoryCode);
        Assert.AreEqual(1, australiaDay.Tags.Count);

        // …while sharing the template's Holiday/NonWorking/weekend-roll baseline.
        Assert.IsNull(christmas.TerritoryCode);
        Assert.IsNull(newYears.TerritoryCode);
        Assert.AreEqual(0, christmas.Tags.Count);
        Assert.AreEqual(0, newYears.Tags.Count);
        Assert.AreEqual(1, christmas.Adjustments.Length);
        Assert.AreEqual(1, newYears.Adjustments.Length);
        Assert.AreEqual(1, australiaDay.Adjustments.Length);
        Assert.AreEqual("weekend-roll", christmas.Adjustments[0].Key);
    }

    /// <summary>
    /// Verifies that a cloned rule builder can use <see cref="NotableDateRuleBuilder.ClearStrategy" /> to swap
    /// strategies without tripping the single-strategy invariant.
    /// </summary>
    [TestMethod]
    public void Clone_WhenCloneClearsStrategyAndReauthors_ShouldNotAffectSource()
    {
        NotableDateRuleBuilder source = new NotableDateRuleBuilder()
            .Category(NotableDateCategory.Holiday)
            .Fixed(12, 25);

        NotableDateRule clonedRule = source.Clone()
            .ClearStrategy()
            .Algorithm(key: "easter-gregorian")
            .Build("Test");

        NotableDateRule sourceRule = source.Build("Test");

        Assert.AreEqual(DateResolutionStrategy.Algorithm, clonedRule.Strategy);
        Assert.AreEqual("easter-gregorian", clonedRule.AlgorithmKey);

        Assert.AreEqual(DateResolutionStrategy.Fixed, sourceRule.Strategy);
        Assert.AreEqual(12, sourceRule.Month);
        Assert.AreEqual(25, sourceRule.Day);
    }
}
