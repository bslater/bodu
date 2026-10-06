// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.GetNextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies that a weekday-morning expression returns the next matching nine-o'clock instant.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenWeekdayMorning_ShouldReturnNextNineAm()
    {
        CronExpression cron = CronExpression.Parse("0 9 * * 1-5");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2026, 1, 1, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 9, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a daily expression returns the next matching instant.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetNextOccurrence_WhenDailyNineAm_ShouldReturnNextNineAm()
    {
        CronExpression cron = CronExpression.Parse("0 9 * * *");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2026, 1, 1, 10, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 2, 9, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a step expression advances to the next aligned minute.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenEveryFifteenMinutes_ShouldReturnNextQuarter()
    {
        CronExpression cron = CronExpression.Parse("*/15 * * * *");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2026, 1, 1, 0, 7, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 0, 15, 0), next);
    }

    /// <summary>
    /// Verifies that the <c>@hourly</c> macro expands to the top of the next hour.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenHourlyMacro_ShouldReturnTopOfHour()
    {
        CronExpression cron = CronExpression.Parse("@hourly");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2026, 1, 1, 0, 30, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 1, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a six-field expression matches on the seconds field.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenWithSeconds_ShouldMatchSecondsField()
    {
        CronExpression cron = CronExpression.Parse("15 30 9 * * *", CronFormat.WithSeconds);

        DateTime? next = cron.GetNextOccurrence(new DateTime(2026, 1, 1, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 1, 9, 30, 15), next);
    }

    /// <summary>
    /// Verifies that a February 29th expression skips forward to the next leap year.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenLeapDay_ShouldSkipToNextLeapYear()
    {
        CronExpression cron = CronExpression.Parse("0 0 29 2 *");

        DateTime? next = cron.GetNextOccurrence(new DateTime(2026, 1, 1, 0, 0, 0));

        Assert.AreEqual(new DateTime(2028, 2, 29, 0, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a search from the last instant of the calendar returns <see langword="null" /> rather than
    /// throwing, in either layout, inclusively or not, and with a Quartz day token.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="inclusive">Whether an occurrence equal to the instant counts.</param>
    [TestMethod]
    [DataRow("* * * * *", CronFormat.Standard, false)]
    [DataRow("* * * * *", CronFormat.Standard, true)]
    [DataRow("* * * * * *", CronFormat.WithSeconds, false)]
    [DataRow("* * * * * *", CronFormat.WithSeconds, true)]
    [DataRow("0 0 L * *", CronFormat.Standard, false)]
    public void GetNextOccurrence_WhenAfterIsTheLastInstant_ShouldReturnNull(string expression, CronFormat format, bool inclusive)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.IsNull(cron.GetNextOccurrence(DateTime.MaxValue, inclusive));
    }

    /// <summary>
    /// Verifies that a search whose next occurrence would fall after the last instant of the calendar returns
    /// <see langword="null" /> rather than throwing, whichever field's step would leave the calendar.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="after">The instant the search starts from.</param>
    [TestMethod]
    [DataRow("0 0 29 2 *", CronFormat.Standard, "9996-03-01T00:00:00")]
    [DataRow("0 0 1 1 *", CronFormat.Standard, "9999-02-01T00:00:00")]
    [DataRow("0 0 1 * *", CronFormat.Standard, "9999-12-15T00:00:00")]
    [DataRow("0 0 31 12 *", CronFormat.Standard, "9999-12-31T00:00:01")]
    [DataRow("0 * * * *", CronFormat.Standard, "9999-12-31T23:00:30")]
    [DataRow("0 * * * * *", CronFormat.WithSeconds, "9999-12-31T23:59:00")]
    [DataRow("0 0 L 2 *", CronFormat.Standard, "9999-03-01T00:00:00")]
    [DataRow("0 0 15W * *", CronFormat.Standard, "9999-12-16T00:00:00")]
    public void GetNextOccurrence_WhenTheOccurrenceWouldFollowTheCalendar_ShouldReturnNull(string expression, CronFormat format, string after)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.IsNull(cron.GetNextOccurrence(Instant(after)));
    }

    /// <summary>
    /// Verifies that a search for an expression that never matches returns <see langword="null" /> rather than
    /// throwing when it starts close enough to the end of the calendar for its horizon to reach past it.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="after">The instant the search starts from.</param>
    [TestMethod]
    [DataRow("0 0 30 2 *", CronFormat.Standard, "9987-01-01T00:00:00")]
    [DataRow("0 0 0 30 2 *", CronFormat.WithSeconds, "9987-01-01T00:00:00")]
    [DataRow("0 0 30W 2 *", CronFormat.Standard, "9987-01-01T00:00:00")]
    [DataRow("0 0 30 2 *", CronFormat.Standard, "9993-06-15T00:00:00")]
    public void GetNextOccurrence_WhenTheSearchForANeverMatchingExpressionReachesTheEnd_ShouldReturnNull(string expression, CronFormat format, string after)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.IsNull(cron.GetNextOccurrence(Instant(after)));
    }

    /// <summary>
    /// Verifies that an occurrence on the last day of the calendar is still found, up to its last minute or second.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="after">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("59 23 31 12 *", CronFormat.Standard, "9999-12-31T00:00:00", "9999-12-31T23:59:00")]
    [DataRow("59 59 23 31 12 *", CronFormat.WithSeconds, "9999-12-31T00:00:00", "9999-12-31T23:59:59")]
    [DataRow("0 23 L 12 *", CronFormat.Standard, "9999-12-01T00:00:00", "9999-12-31T23:00:00")]
    [DataRow("0 0 29 2 *", CronFormat.Standard, "9996-01-01T00:00:00", "9996-02-29T00:00:00")]
    public void GetNextOccurrence_WhenTheOccurrenceIsLateInTheCalendar_ShouldReturnIt(string expression, CronFormat format, string after, string expected)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.AreEqual(Instant(expected), cron.GetNextOccurrence(Instant(after)));
    }
}
