// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.ByMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMonth(MonthSet)" /> rejects an empty set, naming the
    /// <c>ByMonth</c> part, since a rule part that is present selects at least one value.
    /// </summary>
    [TestMethod]
    public void ByMonth_WhenSetIsEmpty_ShouldThrowArgumentException()
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.ByMonth(MonthSet.Empty);
        });

        Assert.AreEqual(nameof(RecurrenceRule.ByMonth), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMonth(MonthSet)" /> writes the set's values in ascending
    /// order, whatever order its text listed them in, so the rule equals the one parsed from that text.
    /// </summary>
    [TestMethod]
    public void ByMonth_WhenSetIsGiven_ShouldBuildTheRuleItsAscendingValuesParseTo()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
            .ByMonth(MonthSet.Parse("12,3,6,9"))
            .Build();

        Assert.AreEqual(RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=3,6,9,12"), rule);
        Assert.AreEqual("FREQ=YEARLY;BYMONTH=3,6,9,12", rule.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMonth(MonthSet)" /> builds the same rule as the
    /// <see langword="int" /> overload given every value of the set's domain.
    /// </summary>
    [TestMethod]
    public void ByMonth_WhenSetIsAll_ShouldBuildTheRuleTheIntOverloadBuilds()
    {
        RecurrenceRule fromSet = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly).ByMonth(MonthSet.All).Build();
        RecurrenceRule fromValues = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly).ByMonth([.. MonthSet.All]).Build();

        Assert.AreEqual(fromValues, fromSet);
    }
}
