// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableNotableDateRuleOverrideProviderTests.AddRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class MutableNotableDateRuleOverrideProviderTests
{
    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.AddRule" /> exposes a freshly added rule
    /// through <see cref="MutableNotableDateRuleOverrideProvider.GetAdditions" />.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenRuleSupplied_ShouldAppearInGetAdditions()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        NotableDateRule rule = Fixed("Company Day");

        provider.AddRule(rule);

        var additions = provider.GetAdditions().ToList();
        Assert.AreEqual(1, additions.Count);
        Assert.AreSame(rule, additions[0]);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.AddRule" /> throws when the rule is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenRuleIsNull_ShouldThrowArgumentNullException()
    {
        MutableNotableDateRuleOverrideProvider provider = new();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            provider.AddRule(null!);
        });

        Assert.AreEqual("rule", ex.ParamName);
    }

    /// <summary>
    /// Verifies that multiple <see cref="MutableNotableDateRuleOverrideProvider.AddRule" /> calls preserve insertion
    /// order in <see cref="MutableNotableDateRuleOverrideProvider.GetAdditions" />.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenCalledMultipleTimes_ShouldPreserveInsertionOrder()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        NotableDateRule first = Fixed("First", 1, 1);
        NotableDateRule second = Fixed("Second", 2, 1);
        NotableDateRule third = Fixed("Third", 3, 1);

        provider.AddRule(first);
        provider.AddRule(second);
        provider.AddRule(third);

        var additions = provider.GetAdditions().ToList();
        CollectionAssert.AreEqual(new[] { first, second, third }, additions);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.AddRule" /> raises
    /// <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> exactly once per call.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenCalled_ShouldRaiseChangedExactlyOnce()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        var changedCount = 0;
        provider.Changed += (_, _) => changedCount++;

        provider.AddRule(Fixed("X"));

        Assert.AreEqual(1, changedCount);
    }

    /// <summary>
    /// Verifies that <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> receives this provider as the
    /// sender and <see cref="EventArgs.Empty" /> as the payload.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenCalled_ShouldRaiseChangedWithProviderAsSender()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        object? capturedSender = null;
        EventArgs? capturedArgs = null;
        provider.Changed += (sender, args) =>
        {
            capturedSender = sender;
            capturedArgs = args;
        };

        provider.AddRule(Fixed("X"));

        Assert.AreSame(provider, capturedSender);
        Assert.AreSame(EventArgs.Empty, capturedArgs);
    }

    /// <summary>
    /// Verifies that the same <see cref="NotableDateRule" /> instance may be added multiple times and each
    /// occurrence appears in the addition list.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenSameInstanceAddedTwice_ShouldAppearTwiceInGetAdditions()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        NotableDateRule rule = Fixed("Repeat");

        provider.AddRule(rule);
        provider.AddRule(rule);

        Assert.AreEqual(2, provider.GetAdditions().Count());
    }

    /// <summary>
    /// Verifies that a previously captured <see cref="MutableNotableDateRuleOverrideProvider.GetAdditions" />
    /// enumerator continues to yield the same snapshot even when the provider is subsequently mutated.
    /// </summary>
    [TestMethod]
    public void AddRule_WhenMutatedAfterGetAdditions_ShouldNotInvalidateEarlierSnapshot()
    {
        MutableNotableDateRuleOverrideProvider provider = new();
        provider.AddRule(Fixed("First"));

        IEnumerable<NotableDateRule> snapshot = provider.GetAdditions();

        provider.AddRule(Fixed("Second"));

        Assert.AreEqual(1, snapshot.Count(), "Snapshot must remain stable under concurrent mutation.");
    }
}
