// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.Offsets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies that the next-occurrence query interprets the wall-clock time in the argument's own non-UTC offset
    /// and returns the occurrence carrying that offset, so a UTC-only test matrix can never mask an offset
    /// regression.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterHasNonUtcOffset_ShouldAnswerInThatOffset()
    {
        CronExpression cron = CronExpression.Parse("0 9 * * *");
        var after = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(10));

        DateTimeOffset? next = cron.GetNextOccurrence(after);

        Assert.AreEqual(new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.FromHours(10)), next);
        Assert.AreEqual(TimeSpan.FromHours(10), next!.Value.Offset);
    }

    /// <summary>
    /// Verifies that the previous-occurrence query interprets the wall-clock time in the argument's own negative,
    /// half-hour offset and returns the occurrence carrying that offset.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeHasNonUtcOffset_ShouldAnswerInThatOffset()
    {
        CronExpression cron = CronExpression.Parse("0 9 * * *");
        var before = new DateTimeOffset(2026, 1, 1, 12, 0, 0, new TimeSpan(-5, -30, 0));

        DateTimeOffset? previous = cron.GetPreviousOccurrence(before);

        Assert.AreEqual(new DateTimeOffset(2026, 1, 1, 9, 0, 0, new TimeSpan(-5, -30, 0)), previous);
        Assert.AreEqual(new TimeSpan(-5, -30, 0), previous!.Value.Offset);
    }

    /// <summary>
    /// Verifies that the wall-clock answer for a daily expression does not depend on the offset the evaluation
    /// instant is supplied in: the same wall-clock query in two offsets yields the same wall-clock occurrence.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenSameWallClockInDifferentOffsets_ShouldAnswerSameWallClock()
    {
        CronExpression cron = CronExpression.Parse("0 9 * * *");
        var utc = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var local = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(10));

        DateTimeOffset? fromUtc = cron.GetNextOccurrence(utc);
        DateTimeOffset? fromLocal = cron.GetNextOccurrence(local);

        Assert.AreEqual(fromUtc!.Value.DateTime, fromLocal!.Value.DateTime);
    }

    /// <summary>
    /// Verifies that the next-occurrence query returns <see langword="null" /> rather than throwing when the
    /// occurrence's wall-clock time exists but its UTC instant would follow the last instant a
    /// <see cref="DateTimeOffset" /> can hold.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenTheOccurrenceFollowsTheLastUtcInstant_ShouldReturnNull()
    {
        CronExpression cron = CronExpression.Parse("0 20 * * *");
        var after = new DateTimeOffset(9999, 12, 31, 10, 0, 0, TimeSpan.FromHours(-5));

        Assert.IsNull(cron.GetNextOccurrence(after));
    }

    /// <summary>
    /// Verifies that the next-occurrence query returns <see langword="null" /> for
    /// <see cref="DateTimeOffset.MaxValue" />.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterIsTheLastOffsetInstant_ShouldReturnNull()
    {
        CronExpression cron = CronExpression.Parse("* * * * *");

        Assert.IsNull(cron.GetNextOccurrence(DateTimeOffset.MaxValue));
    }

    /// <summary>
    /// Verifies that the next-occurrence query still returns an occurrence late on the last day whose UTC instant a
    /// <see cref="DateTimeOffset" /> can hold.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenTheOccurrenceIsJustInsideTheLastUtcDay_ShouldReturnIt()
    {
        CronExpression cron = CronExpression.Parse("0 18 * * *");
        var after = new DateTimeOffset(9999, 12, 31, 10, 0, 0, TimeSpan.FromHours(-5));

        Assert.AreEqual(new DateTimeOffset(9999, 12, 31, 18, 0, 0, TimeSpan.FromHours(-5)), cron.GetNextOccurrence(after));
    }

    /// <summary>
    /// Verifies that the previous-occurrence query returns <see langword="null" /> rather than throwing when the
    /// occurrence's wall-clock time exists but its UTC instant would precede the first instant a
    /// <see cref="DateTimeOffset" /> can hold.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenTheOccurrencePrecedesTheFirstUtcInstant_ShouldReturnNull()
    {
        CronExpression cron = CronExpression.Parse("0 3 * * *");
        var before = new DateTimeOffset(1, 1, 1, 10, 0, 0, TimeSpan.FromHours(5));

        Assert.IsNull(cron.GetPreviousOccurrence(before));
    }

    /// <summary>
    /// Verifies that the previous-occurrence query returns <see langword="null" /> for
    /// <see cref="DateTimeOffset.MinValue" />.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeIsTheFirstOffsetInstant_ShouldReturnNull()
    {
        CronExpression cron = CronExpression.Parse("* * * * *");

        Assert.IsNull(cron.GetPreviousOccurrence(DateTimeOffset.MinValue));
    }

    /// <summary>
    /// Verifies that the previous-occurrence query still returns an occurrence early on the first day whose UTC
    /// instant a <see cref="DateTimeOffset" /> can hold.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenTheOccurrenceIsJustInsideTheFirstUtcDay_ShouldReturnIt()
    {
        CronExpression cron = CronExpression.Parse("0 6 * * *");
        var before = new DateTimeOffset(1, 1, 1, 10, 0, 0, TimeSpan.FromHours(5));

        Assert.AreEqual(new DateTimeOffset(1, 1, 1, 6, 0, 0, TimeSpan.FromHours(5)), cron.GetPreviousOccurrence(before));
    }
}
