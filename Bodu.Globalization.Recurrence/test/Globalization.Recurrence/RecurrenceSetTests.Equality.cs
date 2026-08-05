// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Verifies that two sets built from the same components are equal with equal hash codes, so a consumer can
    /// detect configuration changes by comparison rather than by re-parsing text.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameComponents_ShouldBeEqualWithSameHashCode()
    {
        var first = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY;COUNT=10")],
            dates: [new DateTime(2026, 1, 3, 15, 0, 0)],
            exceptionDates: [new DateTime(2026, 1, 2, 9, 0, 0)]);
        RecurrenceSet second = RecurrenceSet.Parse(first.ToString());

        Assert.IsTrue(first.Equals(second));
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
    }

    /// <summary>
    /// Verifies that explicit and exception dates compare order-insensitively, because both are normalized to
    /// ascending order.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDatesSuppliedInDifferentOrder_ShouldBeEqual()
    {
        var first = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            dates: [new DateTime(2026, 1, 3, 15, 0, 0), new DateTime(2026, 1, 2, 15, 0, 0)],
            exceptionDates: [new DateTime(2026, 1, 6, 9, 0, 0), new DateTime(2026, 1, 5, 9, 0, 0)]);
        var second = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            dates: [new DateTime(2026, 1, 2, 15, 0, 0), new DateTime(2026, 1, 3, 15, 0, 0)],
            exceptionDates: [new DateTime(2026, 1, 5, 9, 0, 0), new DateTime(2026, 1, 6, 9, 0, 0)]);

        Assert.IsTrue(first.Equals(second));
    }

    /// <summary>
    /// Verifies that sets differing in start, rules, dates, or exception dates are not equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComponentsDiffer_ShouldNotBeEqual()
    {
        var baseline = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        Assert.IsFalse(baseline.Equals(new RecurrenceSet(
            new DateTime(2026, 1, 2, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")])));
        Assert.IsFalse(baseline.Equals(new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=WEEKLY")])));
        Assert.IsFalse(baseline.Equals(new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")],
            exceptionDates: [new DateTime(2026, 1, 2, 9, 0, 0)])));
    }

    /// <summary>
    /// Verifies that a set is not equal to <see langword="null" /> and that the <see cref="object" /> overload agrees
    /// with the typed overload.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOtherIsNullOrDifferentType_ShouldNotBeEqual()
    {
        var set = new RecurrenceSet(
            new DateTime(2026, 1, 1, 9, 0, 0),
            [RecurrenceRule.Parse("FREQ=DAILY")]);

        Assert.IsFalse(set.Equals(null));
        Assert.IsFalse(set.Equals((object?)null));
        Assert.IsFalse(set.Equals("DTSTART:20260101T090000"));
    }
}
