// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.InitialState.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class MutableNotableDateRuleOverrideProviderTests
{
    /// <summary>
    /// Verifies that a freshly constructed <see cref="MutableNotableDateRuleOverrideProvider" /> reports no additions.
    /// </summary>
    [TestMethod]
    public void GetAdditions_WhenFreshlyConstructed_ShouldReturnEmpty()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        Assert.AreEqual(0, provider.GetAdditions().Count());
    }

    /// <summary>
    /// Verifies that a freshly constructed <see cref="MutableNotableDateRuleOverrideProvider" /> reports no removals.
    /// </summary>
    [TestMethod]
    public void GetRemovals_WhenFreshlyConstructed_ShouldReturnEmpty()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        Assert.AreEqual(0, provider.GetRemovals().Count());
    }

    /// <summary>
    /// Verifies that no <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> event is raised before any
    /// mutation is performed.
    /// </summary>
    [TestMethod]
    public void Changed_WhenNoMutationPerformed_ShouldNotBeRaised()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        var changedCount = 0;
        provider.Changed += (_, _) => changedCount++;

        _ = provider.GetAdditions().ToList();
        _ = provider.GetRemovals().ToList();

        Assert.AreEqual(0, changedCount);
    }
}
