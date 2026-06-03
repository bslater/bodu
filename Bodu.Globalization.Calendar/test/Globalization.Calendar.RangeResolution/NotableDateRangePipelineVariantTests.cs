// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineVariantTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Integration tests for the chronological range pipeline that exercise offset rules anchored to one of several
/// same-name variants, confirming the pipeline binds to the authored variant rather than the first-encountered
/// candidate and degrades safely when the anchor reference is ambiguous.
/// </summary>
[TestClass]
public sealed class NotableDateRangePipelineVariantTests
{
    /// <summary>
    /// Verifies that an offset rule that authors <see cref="NotableDateRule.AnchorRuleVariant" /> resolves against that
    /// specific same-name anchor variant end-to-end through the range pipeline.
    /// </summary>
    [TestMethod]
    public void ResolveInRange_WhenOffsetAnchorVariantAuthored_ShouldBindToThatVariant()
    {
        NotableDateRule early = Fixed("Spring Festival", "early", 3, 10);
        NotableDateRule late = Fixed("Spring Festival", "late", 3, 20);
        NotableDateRule eve = new()
        {
            Name = "Festival Eve",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Holiday,
            AnchorRuleName = "Spring Festival",
            AnchorRuleVariant = "late",
            OffsetDays = -1,
        };

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(early, late, eve)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31));

        Assert.IsTrue(resolved.Any(n => n.Name == "Festival Eve" && n.Date == new DateTime(2026, 3, 19)),
            "Festival Eve should bind to the 'late' Spring Festival variant (20 Mar) minus one day = 19 Mar.");
        Assert.IsFalse(resolved.Any(n => n.Name == "Festival Eve" && n.Date == new DateTime(2026, 3, 9)),
            "Festival Eve must not bind to the 'early' variant (10 Mar).");
    }

    /// <summary>
    /// Verifies that an offset rule whose anchor name is ambiguous (no authored variant or disambiguating context) does
    /// not emit an arbitrarily-bound date when strict validation is disabled.
    /// </summary>
    [TestMethod]
    public void ResolveInRange_WhenOffsetAnchorAmbiguous_ShouldNotEmitArbitraryDate()
    {
        NotableDateRule early = Fixed("Spring Festival", "early", 3, 10);
        NotableDateRule late = Fixed("Spring Festival", "late", 3, 20);
        NotableDateRule eve = new()
        {
            Name = "Festival Eve",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Holiday,
            AnchorRuleName = "Spring Festival",
            OffsetDays = -1,
        };

        NotableDateService service = new(
            ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(early, late, eve)],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31));

        Assert.IsFalse(resolved.Any(n => n.Name == "Festival Eve"),
            "An ambiguous anchor must not silently bind Festival Eve to an arbitrary same-name variant.");
    }

    /// <summary>
    /// Verifies that constructing a service with strict validation enabled surfaces an ambiguous offset anchor as an
    /// error rather than silently binding to a candidate.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOffsetAnchorAmbiguousAndValidationEnabled_ShouldThrow()
    {
        NotableDateRule early = Fixed("Spring Festival", "early", 3, 10);
        NotableDateRule late = Fixed("Spring Festival", "late", 3, 20);
        NotableDateRule eve = new()
        {
            Name = "Festival Eve",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Holiday,
            AnchorRuleName = "Spring Festival",
            OffsetDays = -1,
        };

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new NotableDateService(
                ruleProviders: [(INotableDateRuleProvider)new InMemoryRuleProvider(early, late, eve)],
                workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
                options: new NotableDateServiceOptions { ValidateRules = true });
        });

        Assert.IsTrue(ex.Message.Contains("Spring Festival", StringComparison.Ordinal));
    }

    private static NotableDateRule Fixed(string name, string? variant, int month, int day) => new()
    {
        Name = name,
        RuleName = variant,
        Strategy = DateResolutionStrategy.Fixed,
        Category = NotableDateCategory.Holiday,
        Month = month,
        Day = day,
    };

    /// <summary>
    /// In-memory rule provider used by the variant integration tests.
    /// </summary>
    private sealed class InMemoryRuleProvider
        : INotableDateRuleProvider
    {
        private readonly IReadOnlyList<NotableDateRule> _rules;

        public InMemoryRuleProvider(params NotableDateRule[] rules)
        {
            _rules = rules;
        }

        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }
}
