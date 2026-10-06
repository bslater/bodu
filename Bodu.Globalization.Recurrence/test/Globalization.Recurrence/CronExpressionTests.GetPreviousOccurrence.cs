// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.GetPreviousOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies that the previous-occurrence search returns the most recent matching instant.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenWeekdayMorning_ShouldReturnPriorNineAm()
    {
        CronExpression cron = CronExpression.Parse("0 9 * * 1-5");

        DateTime? previous = cron.GetPreviousOccurrence(new DateTime(2026, 1, 1, 12, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 9, 0, 0), previous);
    }
}
