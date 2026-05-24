// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.Clear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class MutableNotableDateRuleOverrideProviderTests
{
    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.Clear" /> drops every previously authored
    /// addition and removal.
    /// </summary>
    [TestMethod]
    public void Clear_WhenAdditionsAndRemovalsArePresent_ShouldEmptyBothCollections()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        provider.AddRule(Fixed("A"));
        provider.AddRule(Fixed("B"));
        provider.RemoveRule("Boxing Day");

        provider.Clear();

        Assert.AreEqual(0, provider.GetAdditions().Count());
        Assert.AreEqual(0, provider.GetRemovals().Count());
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.Clear" /> raises
    /// <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> exactly once regardless of how many entries
    /// were dropped.
    /// </summary>
    [TestMethod]
    public void Clear_WhenMultipleEntriesPresent_ShouldRaiseChangedExactlyOnce()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        provider.AddRule(Fixed("A"));
        provider.AddRule(Fixed("B"));
        provider.RemoveRule("X");
        provider.RemoveRule("Y");

        int changedCount = 0;
        provider.Changed += (_, _) => changedCount++;

        provider.Clear();

        Assert.AreEqual(1, changedCount);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.Clear" /> on an empty provider still raises
    /// <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> exactly once.
    /// </summary>
    [TestMethod]
    public void Clear_WhenEmpty_ShouldStillRaiseChangedOnce()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        int changedCount = 0;
        provider.Changed += (_, _) => changedCount++;

        provider.Clear();

        Assert.AreEqual(1, changedCount);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.Clear" /> leaves the provider in a usable state
    /// for further mutations.
    /// </summary>
    [TestMethod]
    public void Clear_WhenFollowedByAddRule_ShouldExposeNewAddition()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        provider.AddRule(Fixed("Old"));
        provider.Clear();

        NotableDateRule newRule = Fixed("New");
        provider.AddRule(newRule);

        Assert.AreSame(newRule, provider.GetAdditions().Single());
    }
}
