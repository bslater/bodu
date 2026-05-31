// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.RemoveRule.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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

        var removals = provider.GetRemovals().ToList();
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

        var removals = provider.GetRemovals().ToList();
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
        var changedCount = 0;
        provider.Changed += (_, _) => changedCount++;

        provider.RemoveRule("X");

        Assert.AreEqual(1, changedCount);
    }

    /// <summary>
    /// Verifies the scope-binding contract of <see cref="MutableNotableDateRuleOverrideProvider.RemoveRule" /> across
    /// every combination of year and territory scoping.
    /// </summary>
    /// <param name="fromYear">The <c>fromYear</c> argument under test.</param>
    /// <param name="toYear">The <c>toYear</c> argument under test.</param>
    /// <param name="territoryCode">The <c>territoryCode</c> argument under test.</param>
    [TestMethod]
    [DataRow(null, null, null)]
    [DataRow(2026, null, null)]
    [DataRow(null, 2030, null)]
    [DataRow(2026, 2030, null)]
    [DataRow(null, null, "AU-NSW")]
    [DataRow(2026, 2030, "AU-NSW")]
    [DataRow(null, null, "")]
    public void RemoveRule_WhenScopedByVariousCombinations_ShouldRecordScopeUnchanged(int? fromYear, int? toYear, string? territoryCode)
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        provider.RemoveRule("Target", fromYear, toYear, territoryCode);

        RuleRemoval removal = provider.GetRemovals().Single();
        Assert.AreEqual("Target", removal.RuleName);
        Assert.AreEqual(fromYear, removal.FromYear);
        Assert.AreEqual(toYear, removal.ToYear);
        Assert.AreEqual(territoryCode, removal.TerritoryCode);
    }

    /// <summary>
    /// Verifies that multiple removals authored against the same rule name with differing scopes are kept distinct in
    /// the snapshot — the provider composes by union, not by replacement.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenSameNameWithDifferentScopes_ShouldKeepAllRemovals()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        provider.RemoveRule("Boxing Day", fromYear: 2026, toYear: 2026);
        provider.RemoveRule("Boxing Day", territoryCode: "AU-NT");
        provider.RemoveRule("Boxing Day", fromYear: 2030, toYear: 2030, territoryCode: "AU-VIC");

        var removals = provider.GetRemovals().ToList();

        Assert.AreEqual(3, removals.Count);
        Assert.IsTrue(removals.Any(r => r.FromYear == 2026 && r.ToYear == 2026 && r.TerritoryCode is null));
        Assert.IsTrue(removals.Any(r => r.FromYear is null && r.TerritoryCode == "AU-NT"));
        Assert.IsTrue(removals.Any(r => r.FromYear == 2030 && r.TerritoryCode == "AU-VIC"));
    }

    /// <summary>
    /// Verifies that a previously captured <see cref="MutableNotableDateRuleOverrideProvider.GetRemovals" /> enumerator
    /// continues to yield the same snapshot even when the provider is subsequently mutated.
    /// </summary>
    [TestMethod]
    public void RemoveRule_WhenMutatedAfterGetRemovals_ShouldNotInvalidateEarlierSnapshot()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        provider.RemoveRule("First");

        IEnumerable<RuleRemoval> snapshot = provider.GetRemovals();

        provider.RemoveRule("Second");

        Assert.AreEqual(1, snapshot.Count(), "Snapshot must remain stable under concurrent mutation.");
    }
}
