// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.ByMonthDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMonthDay(DayOfMonthSet)" /> rejects an empty set, naming the
    /// <c>ByMonthDay</c> part, since a rule part that is present selects at least one value.
    /// </summary>
    [TestMethod]
    public void ByMonthDay_WhenSetIsEmpty_ShouldThrowArgumentException()
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = builder.ByMonthDay(DayOfMonthSet.Empty);
        });

        Assert.AreEqual(nameof(RecurrenceRule.ByMonthDay), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMonthDay(DayOfMonthSet)" /> writes the set's values in ascending
    /// order, whatever order its text listed them in, so the rule equals the one parsed from that text.
    /// </summary>
    [TestMethod]
    public void ByMonthDay_WhenSetIsGiven_ShouldBuildTheRuleItsAscendingValuesParseTo()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly)
            .ByMonthDay(DayOfMonthSet.Parse("31,1,15"))
            .Build();

        Assert.AreEqual(RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=1,15,31"), rule);
        Assert.AreEqual("FREQ=MONTHLY;BYMONTHDAY=1,15,31", rule.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByMonthDay(DayOfMonthSet)" /> builds the same rule as the
    /// <see langword="int" /> overload given every value of the set's domain.
    /// </summary>
    [TestMethod]
    public void ByMonthDay_WhenSetIsAll_ShouldBuildTheRuleTheIntOverloadBuilds()
    {
        RecurrenceRule fromSet = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly).ByMonthDay(DayOfMonthSet.All).Build();
        RecurrenceRule fromValues = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly).ByMonthDay([.. DayOfMonthSet.All]).Build();

        Assert.AreEqual(fromValues, fromSet);
    }
}
