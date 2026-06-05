// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionServiceTests.AdjusterCallbacks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies <see cref="AdjustmentAction.ReplaceWithNamedDate" /> behaviour through the public range pipeline.
/// When the adjustment fires, the substitute date is fetched by the target rule's name; when the named target is
/// missing, the adjustment falls back to the original anchor date.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionServiceAdjusterCallbacksTests
{
    /// <summary>
    /// Verifies that a <see cref="AdjustmentAction.ReplaceWithNamedDate" /> adjustment resolves to the date emitted
    /// for the named target rule.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenReplaceWithNamedDateActivates_ShouldUseTargetRuleDate()
    {
        NotableDateRule target = new()
        {
            Name = "Substitute Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 6,
            Day = 20,
        };

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

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(target, redirect)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 30));

        // The redirect rule should be adjusted onto Substitute Day's date (20 June 2026), and the AdjustmentReason
        // should reflect the original anchor (1 June 2026).
        NotableDate? adjusted = resolved.FirstOrDefault(n => n.Name == "Redirected Day" && n.WasAdjusted);

        Assert.IsNotNull(adjusted);
        Assert.AreEqual(new DateTime(2026, 6, 20), adjusted!.Date);
        Assert.AreEqual(new DateTime(2026, 6, 1), adjusted.AdjustmentReason!.OriginalDate);
    }

    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.ReplaceWithNamedDate" /> falls back to the original anchor date
    /// when no rule with the supplied target name exists in the effective rule set.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenReplaceWithNamedDateTargetIsMissing_ShouldFallBackToOriginalDate()
    {
        NotableDateRule redirect = new()
        {
            Name = "Orphan Redirect",
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
                TargetRuleName = "Missing Target",
            }],
        };

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(redirect)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> resolved = service.GetNotableDates(
            new DateTime(2026, 6, 1),
            new DateTime(2026, 6, 30));

        // With no target rule found, the adjustment resolves to the original date (1 June). Because the adjusted
        // date equals the anchor, the adjustment processor suppresses the duplicate and emits only the base entry.
        Assert.AreEqual(1, resolved.Count);
        Assert.IsFalse(resolved[0].WasAdjusted);
    }

    private sealed class InMemoryRuleProvider
        : INotableDateRuleProvider
    {
        private readonly IReadOnlyList<NotableDateRule> _rules;

        public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }
}
