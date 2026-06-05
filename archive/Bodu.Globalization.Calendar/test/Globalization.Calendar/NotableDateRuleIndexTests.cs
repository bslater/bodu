// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleIndexTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the candidate resolution and disambiguation behavior of <see cref="NotableDateRuleIndex" />.
/// </summary>
[TestClass]
public sealed class NotableDateRuleIndexTests
{
    /// <summary>
    /// Verifies that a name-only reference resolves uniquely when a single rule carries that name.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleCandidate_ShouldReturnUnique()
    {
        NotableDateRuleIndex index = new([Rule("Easter", "western", null, null)]);

        RuleReferenceResult result = index.Resolve(NotableDateRuleReference.ForName("Easter"), null, null);

        Assert.AreEqual(RuleReferenceMatch.Unique, result.Match);
        Assert.AreEqual("western", result.Rule!.RuleName);
    }

    /// <summary>
    /// Verifies that a reference whose name matches no rule resolves to <see cref="RuleReferenceMatch.None" />.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenNameAbsent_ShouldReturnNone()
    {
        NotableDateRuleIndex index = new([Rule("Easter", "western", null, null)]);

        RuleReferenceResult result = index.Resolve(NotableDateRuleReference.ForName("Diwali"), null, null);

        Assert.AreEqual(RuleReferenceMatch.None, result.Match);
        Assert.IsNull(result.Rule);
    }

    /// <summary>
    /// Verifies that two same-name variants that cannot be separated by context resolve as ambiguous.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultipleVariantsAndNoDisambiguatingContext_ShouldReturnAmbiguous()
    {
        NotableDateRuleIndex index = new(
        [
            Rule("Easter", "western", null, null),
            Rule("Easter", "orthodox", null, null),
        ]);

        RuleReferenceResult result = index.Resolve(NotableDateRuleReference.ForName("Easter"), null, null);

        Assert.AreEqual(RuleReferenceMatch.Ambiguous, result.Match);
        Assert.AreEqual(2, result.CandidateCount);
        Assert.IsNull(result.Rule);
    }

    /// <summary>
    /// Verifies that a name-only reference is disambiguated by the requesting context's territory.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenVariantsDifferByTerritory_ShouldDisambiguateByContext()
    {
        NotableDateRuleIndex index = new(
        [
            Rule("National Day", "a", "AU", null),
            Rule("National Day", "b", "NZ", null),
        ]);

        RuleReferenceResult result = index.Resolve(NotableDateRuleReference.ForName("National Day"), "AU", null);

        Assert.AreEqual(RuleReferenceMatch.Unique, result.Match);
        Assert.AreEqual("AU", result.Rule!.TerritoryCode);
    }

    /// <summary>
    /// Verifies that an explicit rule-name filter on the reference selects a single variant directly.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenReferenceFiltersByRuleName_ShouldReturnUnique()
    {
        NotableDateRuleIndex index = new(
        [
            Rule("Easter", "western", null, null),
            Rule("Easter", "orthodox", null, null),
        ]);

        RuleReferenceResult result = index.Resolve(new NotableDateRuleReference("Easter", RuleName: "orthodox"), null, null);

        Assert.AreEqual(RuleReferenceMatch.Unique, result.Match);
        Assert.AreEqual("orthodox", result.Rule!.RuleName);
    }

    /// <summary>
    /// Verifies that an exact identity look-up returns the matching rule.
    /// </summary>
    [TestMethod]
    public void TryGetByIdentity_WhenExactMatch_ShouldReturnRule()
    {
        NotableDateRule orthodox = Rule("Easter", "orthodox", null, null);
        NotableDateRuleIndex index = new([Rule("Easter", "western", null, null), orthodox]);

        var found = index.TryGetByIdentity(new NotableDateRuleIdentity("Easter", "orthodox", null, null), out NotableDateRule? rule);

        Assert.IsTrue(found);
        Assert.AreSame(orthodox, rule);
    }

    /// <summary>
    /// Verifies that constructing an index from a null rule list throws.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRulesNull_ShouldThrowExactlyArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDateRuleIndex(null!);
        });
    }

    /// <summary>
    /// Creates a minimal <see cref="NotableDateRule" /> carrying the supplied identity components.
    /// </summary>
    /// <param name="name">The canonical name.</param>
    /// <param name="ruleName">The rule-level variant, or <see langword="null" />.</param>
    /// <param name="territory">The territory code, or <see langword="null" />.</param>
    /// <param name="calendar">The calendar type, or <see langword="null" />.</param>
    /// <returns>The constructed rule.</returns>
    private static NotableDateRule Rule(string name, string? ruleName, string? territory, Type? calendar) =>
        new()
        {
            Name = name,
            RuleName = ruleName,
            TerritoryCode = territory,
            CalendarType = calendar,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
        };
}
