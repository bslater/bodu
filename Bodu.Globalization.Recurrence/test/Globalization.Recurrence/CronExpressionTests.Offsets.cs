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
}
