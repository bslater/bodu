// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Contains unit tests for the <see cref="CronExpression" /> type.
/// </summary>
[TestClass]
public sealed class CronExpressionTests
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
    /// Verifies that the canonical text round-trips through <see cref="CronExpression.Parse(string)" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenReparsed_ShouldRoundTrip()
    {
        CronExpression cron = CronExpression.Parse("0 9 * * 1-5");

        CronExpression reparsed = CronExpression.Parse(cron.ToString());

        Assert.AreEqual(cron, reparsed);
    }

    /// <summary>
    /// Verifies that a named-field expression matches the same instants as its numeric form.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNamedFields_ShouldEqualNumericForm()
    {
        CronExpression named = CronExpression.Parse("0 0 1 JAN MON");
        CronExpression numeric = CronExpression.Parse("0 0 1 1 1");

        Assert.AreEqual(numeric, named);
    }

    /// <summary>
    /// Verifies that a Quartz extension token throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuartzToken_ShouldThrowNotSupportedException()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = CronExpression.Parse("0 0 L * *");
        });
    }

    /// <summary>
    /// Verifies that an expression with the wrong field count fails to parse.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenWrongFieldCount_ShouldReturnFalse()
    {
        bool parsed = CronExpression.TryParse("0 0 * *", out CronExpression? result);

        Assert.IsFalse(parsed);
        Assert.IsNull(result);
    }
}
