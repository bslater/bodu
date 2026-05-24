// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the runtime-mutation contract, snapshot semantics, and event-raising behaviour of
/// <see cref="MutableNotableDateRuleOverrideProvider" />.
/// </summary>
[TestClass]
public sealed partial class MutableNotableDateRuleOverrideProviderTests
{
    /// <summary>
    /// Builds a simple fixed-month rule used by tests that do not care about the rule shape itself.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <returns>A new <see cref="NotableDateRule" /> with the supplied identity.</returns>
    private static NotableDateRule Fixed(string name, int month = 1, int day = 1) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
        };

    /// <summary>
    /// Verifies that raising <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> with no subscribers does
    /// not throw — i.e. the provider null-checks the event field correctly.
    /// </summary>
    [TestMethod]
    public void Mutations_WhenNoChangedSubscribers_ShouldNotThrow()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        provider.AddRule(Fixed("X"));
        provider.RemoveRule("Y");
        provider.Clear();
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.GetAdditions" /> and
    /// <see cref="MutableNotableDateRuleOverrideProvider.GetRemovals" /> return independent snapshots — mutating one
    /// list does not perturb a previously captured snapshot of the other.
    /// </summary>
    [TestMethod]
    public void GetAdditionsAndGetRemovals_WhenMutatedAcrossEachOther_ShouldYieldIndependentSnapshots()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        provider.AddRule(Fixed("Initial Add"));
        provider.RemoveRule("Initial Remove");

        IEnumerable<NotableDateRule> additionsSnapshot = provider.GetAdditions();
        IEnumerable<RuleRemoval> removalsSnapshot = provider.GetRemovals();

        provider.AddRule(Fixed("Late Add"));
        provider.RemoveRule("Late Remove");

        Assert.AreEqual(1, additionsSnapshot.Count(), "Additions snapshot must not see the late-added rule.");
        Assert.AreEqual(1, removalsSnapshot.Count(), "Removals snapshot must not see the late-added removal.");
    }
}
