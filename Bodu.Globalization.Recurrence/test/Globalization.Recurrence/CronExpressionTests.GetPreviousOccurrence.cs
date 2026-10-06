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

    /// <summary>
    /// Verifies that a search from the first instant of the calendar returns <see langword="null" /> rather than
    /// throwing, in either layout, inclusively or not, and with a Quartz day token.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="inclusive">Whether an occurrence equal to the instant counts.</param>
    [TestMethod]
    [DataRow("* * * * *", CronFormat.Standard, false)]
    [DataRow("* * * * * *", CronFormat.WithSeconds, false)]
    [DataRow("0 0 L * *", CronFormat.Standard, false)]
    [DataRow("0 0 L * *", CronFormat.Standard, true)]
    public void GetPreviousOccurrence_WhenBeforeIsTheFirstInstant_ShouldReturnNull(string expression, CronFormat format, bool inclusive)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.IsNull(cron.GetPreviousOccurrence(DateTime.MinValue, inclusive));
    }

    /// <summary>
    /// Verifies that an inclusive search from the first instant of the calendar returns that instant when it matches.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenInclusiveAtTheFirstInstantThatMatches_ShouldReturnIt()
    {
        CronExpression cron = CronExpression.Parse("* * * * *");

        Assert.AreEqual(DateTime.MinValue, cron.GetPreviousOccurrence(DateTime.MinValue, inclusive: true));
    }

    /// <summary>
    /// Verifies that a search whose previous occurrence would fall before the first instant of the calendar returns
    /// <see langword="null" /> rather than throwing, whichever field's step would leave the calendar.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="before">The instant the search starts from.</param>
    [TestMethod]
    [DataRow("0 0 1 2 *", CronFormat.Standard, "0001-01-15T00:00:00")]
    [DataRow("0 0 2 * *", CronFormat.Standard, "0001-01-01T12:00:00")]
    [DataRow("0 12 * * *", CronFormat.Standard, "0001-01-01T11:00:00")]
    [DataRow("30 * * * *", CronFormat.Standard, "0001-01-01T00:15:00")]
    [DataRow("30 * * * * *", CronFormat.WithSeconds, "0001-01-01T00:00:15")]
    [DataRow("0 0 L 2 *", CronFormat.Standard, "0001-01-15T00:00:00")]
    [DataRow("0 0 15W * *", CronFormat.Standard, "0001-01-12T00:00:00")]
    public void GetPreviousOccurrence_WhenTheOccurrenceWouldPrecedeTheCalendar_ShouldReturnNull(string expression, CronFormat format, string before)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.IsNull(cron.GetPreviousOccurrence(Instant(before)));
    }

    /// <summary>
    /// Verifies that a search for an expression that never matches returns <see langword="null" /> rather than
    /// throwing when it starts close enough to the start of the calendar for its horizon to reach past it.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="before">The instant the search starts from.</param>
    [TestMethod]
    [DataRow("0 0 30 2 *", CronFormat.Standard, "0013-12-31T00:00:00")]
    [DataRow("0 0 0 30 2 *", CronFormat.WithSeconds, "0013-12-31T00:00:00")]
    [DataRow("0 0 30W 2 *", CronFormat.Standard, "0013-12-31T00:00:00")]
    [DataRow("0 0 30 2 *", CronFormat.Standard, "0007-06-15T00:00:00")]
    [DataRow("0 0 30 2 *", CronFormat.Standard, "0401-12-31T00:00:00")]
    [DataRow("0 0 30W 2 *", CronFormat.Standard, "0401-12-31T00:00:00")]
    [DataRow("0 0 30 2 *", CronFormat.Standard, "0300-01-01T00:00:00")]
    public void GetPreviousOccurrence_WhenTheSearchForANeverMatchingExpressionReachesTheStart_ShouldReturnNull(string expression, CronFormat format, string before)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.IsNull(cron.GetPreviousOccurrence(Instant(before)));
    }

    /// <summary>
    /// Verifies that an occurrence on the first day of the calendar is still found, down to its first minute or
    /// second.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="before">The instant the search starts from.</param>
    /// <param name="expected">The expected previous occurrence.</param>
    [TestMethod]
    [DataRow("0 0 1 1 *", CronFormat.Standard, "0001-06-01T00:00:00", "0001-01-01T00:00:00")]
    [DataRow("0 0 0 1 1 *", CronFormat.WithSeconds, "0001-01-01T00:00:01", "0001-01-01T00:00:00")]
    [DataRow("0 0 1W 1 *", CronFormat.Standard, "0001-01-02T00:00:00", "0001-01-01T00:00:00")]
    [DataRow("0 0 29 2 *", CronFormat.Standard, "0004-12-31T00:00:00", "0004-02-29T00:00:00")]
    public void GetPreviousOccurrence_WhenTheOccurrenceIsEarlyInTheCalendar_ShouldReturnIt(string expression, CronFormat format, string before, string expected)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.AreEqual(Instant(expected), cron.GetPreviousOccurrence(Instant(before)));
    }

    /// <summary>
    /// Verifies that the search finds an occurrence decades back: the 29th of February on given weekdays recurs at gaps
    /// of up to forty years, the non-leap 2100 included.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="format">The field layout.</param>
    /// <param name="before">The instant the search starts from.</param>
    /// <param name="expected">The expected previous occurrence.</param>
    [TestMethod]
    [DataRow("0 0 29 2 */5", CronFormat.Standard, "2032-01-01T00:00:00", "2008-02-29T00:00:00")]
    [DataRow("0 0 0 29 2 */5", CronFormat.WithSeconds, "2032-01-01T00:00:00", "2008-02-29T00:00:00")]
    [DataRow("0 0 * 2 MON#5", CronFormat.Standard, "2044-01-01T00:00:00", "2016-02-29T00:00:00")]
    [DataRow("0 0 * 2 MON#5", CronFormat.Standard, "2112-01-01T00:00:00", "2072-02-29T00:00:00")]
    public void GetPreviousOccurrence_WhenTheOccurrenceIsDecadesBack_ShouldReturnIt(string expression, CronFormat format, string before, string expected)
    {
        CronExpression cron = CronExpression.Parse(expression, format);

        Assert.AreEqual(Instant(expected), cron.GetPreviousOccurrence(Instant(before)));
    }

    /// <summary>
    /// Verifies that an expression that never matches returns <see langword="null" /> once the search has covered a
    /// whole 400-year cycle of the calendar.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    [TestMethod]
    [DataRow("0 0 30 2 *")]
    [DataRow("0 0 31 4,6,9,11 *")]
    [DataRow("0 0 30W 2 *")]
    public void GetPreviousOccurrence_WhenTheExpressionNeverMatches_ShouldReturnNull(string expression)
    {
        CronExpression cron = CronExpression.Parse(expression);

        Assert.IsNull(cron.GetPreviousOccurrence(Instant("2026-03-10T00:00:00")));
    }
}
