// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.RemoveRule.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class MutableNotableDateRuleOverrideProviderTests
{
    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> exposes a <see cref="RuleRemoval" />
    /// with the supplied scope through <see cref="MutableNotableDateRuleOverrideProvider.GetRemovals" />.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenNameSupplied_ShouldAppearInGetRemovals()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        provider.RemoveRule("Boxing Day");

        List<RuleRemoval> removals = provider.GetRemovals().ToList();
        Assert.AreEqual(1, removals.Count);
        Assert.AreEqual("Boxing Day", removals[0].RuleName);
        Assert.IsNull(removals[0].FromYear);
        Assert.IsNull(removals[0].ToYear);
        Assert.IsNull(removals[0].TerritoryCode);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> records all supplied scope
    /// arguments on the materialised <see cref="RuleRemoval" />.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenScopedByYearAndTerritory_ShouldPropagateScope()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        provider.RemoveRule("Picnic Day", fromYear: 2026, toYear: 2030, territoryCode: "AU-NT");

        RuleRemoval removal = provider.GetRemovals().Single();
        Assert.AreEqual("Picnic Day", removal.RuleName);
        Assert.AreEqual(2026, removal.FromYear);
        Assert.AreEqual(2030, removal.ToYear);
        Assert.AreEqual("AU-NT", removal.TerritoryCode);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> throws when the rule name is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            provider.RemoveRule(null!);
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> throws when the rule name is
    /// empty.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            provider.RemoveRule(string.Empty);
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> throws when the rule name is
    /// whitespace.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenNameIsWhitespace_ShouldThrowArgumentException()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            provider.RemoveRule("   ");
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that multiple <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> calls preserve
    /// insertion order.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenCalledMultipleTimes_ShouldPreserveInsertionOrder()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        provider.RemoveRule("A");
        provider.RemoveRule("B");
        provider.RemoveRule("C");

        List<RuleRemoval> removals = provider.GetRemovals().ToList();
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, removals.Select(r => r.RuleName).ToList());
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> raises
    /// <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> exactly once per call.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenCalled_ShouldRaiseChangedExactlyOnce()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        int changedCount = 0;
        provider.Changed += (_, _) => changedCount++;

        provider.RemoveRule("X");

        Assert.AreEqual(1, changedCount);
    }
}
