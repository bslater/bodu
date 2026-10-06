// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.BySecond.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.BySecond(SecondSet)" /> rejects an empty set, naming the
    /// <c>BySecond</c> part, since a rule part that is present selects at least one value.
    /// </summary>
    [TestMethod]
    public void BySecond_WhenSetIsEmpty_ShouldThrowArgumentException()
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Minutely);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.BySecond(SecondSet.Empty);
        });

        Assert.AreEqual(nameof(RecurrenceRule.BySecond), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.BySecond(SecondSet)" /> writes the set's values in ascending
    /// order, whatever order its text listed them in, so the rule equals the one parsed from that text.
    /// </summary>
    [TestMethod]
    public void BySecond_WhenSetIsGiven_ShouldBuildTheRuleItsAscendingValuesParseTo()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Minutely)
            .BySecond(SecondSet.Parse("45,0,30,15"))
            .Build();

        Assert.AreEqual(RecurrenceRule.Parse("FREQ=MINUTELY;BYSECOND=0,15,30,45"), rule);
        Assert.AreEqual("FREQ=MINUTELY;BYSECOND=0,15,30,45", rule.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.BySecond(SecondSet)" /> builds the same rule as the
    /// <see langword="int" /> overload given every value of the set's domain.
    /// </summary>
    [TestMethod]
    public void BySecond_WhenSetIsAll_ShouldBuildTheRuleTheIntOverloadBuilds()
    {
        RecurrenceRule fromSet = new RecurrenceRuleBuilder(RecurrenceFrequency.Minutely).BySecond(SecondSet.All).Build();
        RecurrenceRule fromValues = new RecurrenceRuleBuilder(RecurrenceFrequency.Minutely).BySecond([.. SecondSet.All]).Build();

        Assert.AreEqual(fromValues, fromSet);
    }
}
