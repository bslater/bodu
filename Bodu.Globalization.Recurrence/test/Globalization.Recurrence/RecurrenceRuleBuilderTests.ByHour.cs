// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.ByHour.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByHour(HourSet)" /> rejects an empty set, naming the
    /// <c>ByHour</c> part, since a rule part that is present selects at least one value.
    /// </summary>
    [TestMethod]
    public void ByHour_WhenSetIsEmpty_ShouldThrowArgumentException()
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.ByHour(HourSet.Empty);
        });

        Assert.AreEqual(nameof(RecurrenceRule.ByHour), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByHour(HourSet)" /> writes the set's values in ascending
    /// order, whatever order its text listed them in, so the rule equals the one parsed from that text.
    /// </summary>
    [TestMethod]
    public void ByHour_WhenSetIsGiven_ShouldBuildTheRuleItsAscendingValuesParseTo()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily)
            .ByHour(HourSet.Parse("17,9-11"))
            .Build();

        Assert.AreEqual(RecurrenceRule.Parse("FREQ=DAILY;BYHOUR=9,10,11,17"), rule);
        Assert.AreEqual("FREQ=DAILY;BYHOUR=9,10,11,17", rule.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByHour(HourSet)" /> builds the same rule as the
    /// <see langword="int" /> overload given every value of the set's domain.
    /// </summary>
    [TestMethod]
    public void ByHour_WhenSetIsAll_ShouldBuildTheRuleTheIntOverloadBuilds()
    {
        RecurrenceRule fromSet = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily).ByHour(HourSet.All).Build();
        RecurrenceRule fromValues = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily).ByHour([.. HourSet.All]).Build();

        Assert.AreEqual(fromValues, fromSet);
    }
}
