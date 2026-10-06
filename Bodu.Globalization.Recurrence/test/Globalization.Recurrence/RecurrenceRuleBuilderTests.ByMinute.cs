// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.ByMinute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMinute(MinuteSet)" /> rejects an empty set, naming the
    /// <c>ByMinute</c> part, since a rule part that is present selects at least one value.
    /// </summary>
    [TestMethod]
    public void ByMinute_WhenSetIsEmpty_ShouldThrowArgumentException()
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Hourly);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.ByMinute(MinuteSet.Empty);
        });

        Assert.AreEqual(nameof(RecurrenceRule.ByMinute), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMinute(MinuteSet)" /> writes the set's values in ascending
    /// order, whatever order its text listed them in, so the rule equals the one parsed from that text.
    /// </summary>
    [TestMethod]
    public void ByMinute_WhenSetIsGiven_ShouldBuildTheRuleItsAscendingValuesParseTo()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Hourly)
            .ByMinute(MinuteSet.Parse("45,0-1"))
            .Build();

        Assert.AreEqual(RecurrenceRule.Parse("FREQ=HOURLY;BYMINUTE=0,1,45"), rule);
        Assert.AreEqual("FREQ=HOURLY;BYMINUTE=0,1,45", rule.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMinute(MinuteSet)" /> builds the same rule as the
    /// <see langword="int" /> overload given every value of the set's domain.
    /// </summary>
    [TestMethod]
    public void ByMinute_WhenSetIsAll_ShouldBuildTheRuleTheIntOverloadBuilds()
    {
        RecurrenceRule fromSet = new RecurrenceRuleBuilder(RecurrenceFrequency.Hourly).ByMinute(MinuteSet.All).Build();
        RecurrenceRule fromValues = new RecurrenceRuleBuilder(RecurrenceFrequency.Hourly).ByMinute([.. MinuteSet.All]).Build();

        Assert.AreEqual(fromValues, fromSet);
    }
}
