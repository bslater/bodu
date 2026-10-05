// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleBuilderTests.ByDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByDay(WeekDayNum[])" /> rejects an ordinal outside zero and ±1 to
    /// ±53 at the call, naming the <c>ByDay</c> part as the other <c>BY*</c> parts name theirs.
    /// </summary>
    /// <param name="ordinal">The out-of-range ordinal.</param>
    [TestMethod]
    [DataRow(54)]
    [DataRow(-54)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void ByDay_WhenOrdinalIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int ordinal)
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.ByDay(new WeekDayNum(ordinal, DayOfWeek.Monday));
        });

        Assert.AreEqual(nameof(RecurrenceRule.ByDay), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByDay(WeekDayNum[])" /> accepts every ordinal at the edges of its
    /// range, and that each rule it builds reads back from its text unchanged.
    /// </summary>
    /// <param name="ordinal">The ordinal at an edge of the range.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(53)]
    [DataRow(-53)]
    public void ByDay_WhenOrdinalIsAtItsBounds_ShouldBuildARuleThatReadsBack(int ordinal)
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
            .ByDay(new WeekDayNum(ordinal, DayOfWeek.Monday))
            .Build();

        Assert.AreEqual(rule, RecurrenceRule.Parse(rule.ToString()));
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByDay(WeekDayNum[])" /> rejects an entry whose day is not a defined
    /// <see cref="DayOfWeek" />, rather than building a rule whose text names Saturday.
    /// </summary>
    /// <param name="day">The undefined day value.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(9)]
    public void ByDay_WhenWeekDayNumDayIsUndefined_ShouldThrowArgumentOutOfRangeException(int day)
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Monthly);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.ByDay(new WeekDayNum(1, (DayOfWeek)day));
        });

        Assert.AreEqual(nameof(RecurrenceRule.ByDay), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceRuleBuilder.ByDay(DayOfWeek[])" /> rejects a day that is not a defined
    /// <see cref="DayOfWeek" />, rather than building a rule whose text names Saturday and which selects no day at all.
    /// </summary>
    /// <param name="day">The undefined day value.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(9)]
    public void ByDay_WhenDayOfWeekIsUndefined_ShouldThrowArgumentOutOfRangeException(int day)
    {
        var builder = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = builder.ByDay(DayOfWeek.Monday, (DayOfWeek)day);
        });

        Assert.AreEqual(nameof(RecurrenceRule.ByDay), ex.ParamName);
    }
}
