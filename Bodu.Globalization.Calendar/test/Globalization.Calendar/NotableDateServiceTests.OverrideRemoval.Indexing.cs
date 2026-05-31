// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.OverrideRemoval.Indexing.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Exercises override-removal lookup at scale. The pipeline indexes <see cref="RuleRemoval" /> entries by rule name
/// (ordinal-ignore-case) so the per-rule×year×territory suppression check is O(matches-for-that-name) rather than
/// O(total-removals). This partial pins the semantics under that indexing.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that with a large number of override removals across many rule names, removals targeting a specific
    /// rule still suppress it for matching years and territories while leaving other rules unaffected.
    /// </summary>
    [TestMethod]
    public void OverrideRemoval_WhenManyRemovalsAcrossManyNames_ShouldSuppressOnlyMatchingRule()
    {
        NotableDateRule targeted = Fixed("Targeted Holiday", 6, 15, territory: "AU");
        NotableDateRule untouched = Fixed("Untouched Holiday", 6, 16, territory: "AU");

        // 1000 unrelated removals across distinct rule names so the linear-scan baseline would have to skip every
        // one before finding the targeted removal.
        RuleRemoval[] removals = Enumerable.Range(0, 1000)
            .Select(i => new RuleRemoval($"Unrelated-{i}"))
            .Concat([new RuleRemoval("Targeted Holiday", FromYear: 2026, ToYear: 2026)])
            .ToArray();

        TestOverrideProvider provider = new(removals, Array.Empty<NotableDateRule>());

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(targeted, untouched)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)provider],
            });

        IReadOnlyList<NotableDate> in2026 = service.GetNotableDates(2026, "AU");
        IReadOnlyList<NotableDate> in2027 = service.GetNotableDates(2027, "AU");

        Assert.IsFalse(in2026.Any(n => n.Name == "Targeted Holiday"), "Targeted removal must suppress the rule for 2026.");
        Assert.IsTrue(in2026.Any(n => n.Name == "Untouched Holiday"), "Other rules must not be suppressed by the targeted removal.");
        Assert.IsTrue(in2027.Any(n => n.Name == "Targeted Holiday"), "Removal year-scope must release the rule for 2027.");
    }

    /// <summary>
    /// Verifies that rule-name lookup is case-insensitive so a removal authored with mixed case still matches a
    /// canonically-named rule.
    /// </summary>
    [TestMethod]
    public void OverrideRemoval_WhenRemovalNameCaseDiffersFromRule_ShouldStillSuppress()
    {
        NotableDateRule rule = Fixed("Mixed Case Holiday", 6, 15, territory: "AU");
        TestOverrideProvider provider = new(
            removals: [new RuleRemoval("MIXED case HOLIDAY")],
            additions: Array.Empty<NotableDateRule>());

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(rule)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = [(INotableDateRuleOverrideProvider)provider],
            });

        Assert.IsFalse(service.GetNotableDates(2026, "AU").Any(n => n.Name == "Mixed Case Holiday"));
    }
}
