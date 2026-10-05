// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.WithWeekStart.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.WithWeekStart(DayOfWeek)" /> rejects a value that is not a defined
    /// <see cref="DayOfWeek" />, rather than building a rule that writes <c>WKST=SA</c> but counts weeks from another
    /// day.
    /// </summary>
    /// <param name="day">The undefined day value.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(9)]
    public void WithWeekStart_WhenDayIsUndefined_ShouldThrowArgumentOutOfRangeException(int day)
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.WithWeekStart((DayOfWeek)day);
        });

        Assert.AreEqual("weekStart", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a rule built with any defined week start reads back from its text with that week start, and equal
    /// to the rule that wrote it.
    /// </summary>
    /// <param name="day">The week start.</param>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    [DataRow(DayOfWeek.Friday)]
    [DataRow(DayOfWeek.Saturday)]
    public void WithWeekStart_WhenDayIsDefined_ShouldBuildARuleThatReadsBack(DayOfWeek day)
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
            .WithWeekStart(day)
            .WithInterval(2)
            .ByDay(DayOfWeek.Monday, DayOfWeek.Sunday)
            .Build();

        RecurrenceRule reread = RecurrenceRule.Parse(rule.ToString());

        Assert.AreEqual(day, reread.WeekStart);
        Assert.AreEqual(rule, reread);
    }
}
