// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.Builder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that a fluently built rule equals the same rule parsed from text.
    /// </summary>
    [TestMethod]
    public void Build_WhenConfiguredFluently_ShouldEqualParsedRule()
    {
        RecurrenceRule built = new RecurrenceRuleBuilder(RecurrenceFrequency.Weekly)
            .WithInterval(2)
            .WithCount(10)
            .WithWeekStart(DayOfWeek.Sunday)
            .ByDay(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
            .Build();

        RecurrenceRule parsed = RecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;COUNT=10;BYDAY=MO,WE,FR;WKST=SU");

        Assert.AreEqual(parsed, built);
    }

    /// <summary>
    /// Verifies that supplying an interval below one throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void WithInterval_WhenZero_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily).WithInterval(0);
        });
    }

    /// <summary>
    /// Verifies that setting a count clears a previously set <c>UNTIL</c> bound.
    /// </summary>
    [TestMethod]
    public void WithCount_WhenUntilAlreadySet_ShouldClearUntil()
    {
        RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Daily)
            .WithUntil(new DateTime(2026, 12, 31))
            .WithCount(5)
            .Build();

        Assert.AreEqual(5, rule.Count);
        Assert.IsNull(rule.Until);
    }

    /// <summary>
    /// Verifies that an out-of-range <c>BYMONTH</c> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ByMonth_WhenOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly).ByMonth(13);
        });
    }
}
