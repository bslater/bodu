// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.GetNextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that the next occurrence of a daily rule is the earliest expansion after the query instant.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenDaily_ShouldReturnEarliestLaterOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        DateTime? next = rule.GetNextOccurrence(start, new DateTime(2026, 1, 4, 12, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 5, 9, 0, 0), next);
    }

    /// <summary>
    /// Verifies that a query exactly on an occurrence is excluded by default and returned with the inclusive flag.
    /// </summary>
    /// <remarks>
    /// This is the flag the documented due-ness recipe turns on, so it is asserted in both directions rather than
    /// only in its default position.
    /// </remarks>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterEqualsOccurrence_ShouldHonorInclusiveFlag()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);
        var occurrence = new DateTime(2026, 1, 3, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 4, 9, 0, 0), rule.GetNextOccurrence(start, occurrence));
        Assert.AreEqual(occurrence, rule.GetNextOccurrence(start, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that the series start is answered for a query before it, and is itself subject to the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterPrecedesSeriesStart_ShouldReturnSeriesStart()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.AreEqual(start, rule.GetNextOccurrence(start, start.AddDays(-1)));
        Assert.AreEqual(start.AddDays(1), rule.GetNextOccurrence(start, start));
        Assert.AreEqual(start, rule.GetNextOccurrence(start, start, inclusive: true));
    }

    /// <summary>
    /// Verifies that a weekly rule skips to the next selected weekday rather than the next calendar week.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenWeeklyWithByDay_ShouldReturnNextSelectedWeekday()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,WE,FR");
        var start = new DateTime(2026, 1, 5, 9, 0, 0);   // a Monday

        // Queried on the Monday, the next selection is that week's Wednesday.
        Assert.AreEqual(new DateTime(2026, 1, 7, 9, 0, 0), rule.GetNextOccurrence(start, start));

        // Queried after the Friday, the next selection is the following Monday.
        Assert.AreEqual(
            new DateTime(2026, 1, 12, 9, 0, 0),
            rule.GetNextOccurrence(start, new DateTime(2026, 1, 9, 12, 0, 0)));
    }

    /// <summary>
    /// Verifies that a monthly rule anchored on a day that some months lack skips those months rather than clamping
    /// the date back into them.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenMonthlyOnThirtyFirst_ShouldSkipShorterMonths()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=31");
        var start = new DateTime(2026, 1, 1, 0, 0, 0);

        // February, April, June, September and November have no 31st, so January is followed by March.
        Assert.AreEqual(
            new DateTime(2026, 3, 31, 0, 0, 0),
            rule.GetNextOccurrence(start, new DateTime(2026, 1, 31, 0, 0, 0)));
    }

    /// <summary>
    /// Verifies that a count-bounded rule reports no next occurrence once its final occurrence has passed.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterCountBoundedSeriesEnds_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY;COUNT=3");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 3, 9, 0, 0), rule.GetNextOccurrence(start, new DateTime(2026, 1, 2, 12, 0, 0)));
        Assert.IsNull(rule.GetNextOccurrence(start, new DateTime(2026, 1, 3, 12, 0, 0)));
    }

    /// <summary>
    /// Verifies that an <c>UNTIL</c>-bounded rule reports no next occurrence past its bound.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenAfterUntilBound_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY;UNTIL=20260103T090000");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 3, 9, 0, 0), rule.GetNextOccurrence(start, new DateTime(2026, 1, 2, 12, 0, 0)));
        Assert.IsNull(rule.GetNextOccurrence(start, new DateTime(2026, 1, 3, 12, 0, 0)));
    }

    /// <summary>
    /// Verifies that a rule selecting a date no year contains reports no next occurrence rather than searching without
    /// end.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenRuleCanNeverMatch_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30");

        Assert.IsNull(rule.GetNextOccurrence(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1)));
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> overload answers the same wall clock as its
    /// <see cref="DateTime" /> counterpart and carries the query's offset onto the result.
    /// </summary>
    /// <remarks>
    /// The library resolves no time zone, so the offset must ride along unchanged rather than being converted.
    /// </remarks>
    [TestMethod]
    public void GetNextOccurrence_ForDateTimeOffset_ShouldPreserveOffset()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var offset = TimeSpan.FromHours(10);
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, offset);

        DateTimeOffset? next = rule.GetNextOccurrence(start, new DateTimeOffset(2026, 1, 4, 12, 0, 0, offset));

        Assert.AreEqual(new DateTimeOffset(2026, 1, 5, 9, 0, 0, offset), next);
        Assert.AreEqual(offset, next!.Value.Offset);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> overload honors the inclusive flag identically to the
    /// <see cref="DateTime" /> overload.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_ForDateTimeOffset_ShouldHonorInclusiveFlag()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var offset = TimeSpan.FromHours(-5);
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, offset);
        var occurrence = new DateTimeOffset(2026, 1, 3, 9, 0, 0, offset);

        Assert.AreEqual(new DateTimeOffset(2026, 1, 4, 9, 0, 0, offset), rule.GetNextOccurrence(start, occurrence));
        Assert.AreEqual(occurrence, rule.GetNextOccurrence(start, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that the same wall-clock series is produced whatever offset the arguments carry, so the offset
    /// selects no different occurrences.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_ForDateTimeOffset_WhenOffsetsDiffer_ShouldSelectTheSameWallClock()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO");

        foreach (TimeSpan offset in new[] { TimeSpan.Zero, TimeSpan.FromHours(10), TimeSpan.FromHours(-8) })
        {
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, offset);
            DateTimeOffset? next = rule.GetNextOccurrence(start, new DateTimeOffset(2026, 1, 7, 0, 0, 0, offset));

            Assert.AreEqual(new DateTimeOffset(2026, 1, 12, 9, 0, 0, offset), next, $"offset {offset}");
        }
    }

    /// <summary>
    /// Verifies that a series anchored decades before the query instant answers its next occurrence, including a
    /// <c>BYWEEKNO</c> week that begins in the calendar year before the one it is numbered in and sub-daily series
    /// whose limits allow only some times of day.
    /// </summary>
    /// <param name="rule">The recurrence-rule text.</param>
    /// <param name="start">The series start, in sortable format.</param>
    /// <param name="after">The query instant, in sortable format.</param>
    /// <param name="expected">The expected next occurrence, in sortable format.</param>
    [TestMethod]
    [DataRow("FREQ=DAILY", "1990-03-15T09:30:00", "2026-10-04T12:00:00", "2026-10-05T09:30:00", DisplayName = "daily since 1990")]
    [DataRow("FREQ=WEEKLY;BYDAY=MO", "1993-02-08T09:00:00", "2026-10-04T12:00:00", "2026-10-05T09:00:00", DisplayName = "Mondays since 1993")]
    [DataRow("FREQ=MONTHLY;BYMONTHDAY=-1", "1975-01-31T08:00:00", "2026-10-04T00:00:00", "2026-10-31T08:00:00", DisplayName = "month ends since 1975")]
    [DataRow("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=29", "1960-02-29T00:00:00", "2026-10-04T00:00:00", "2028-02-29T00:00:00", DisplayName = "leap days since 1960")]
    [DataRow("FREQ=YEARLY;BYWEEKNO=1;BYDAY=MO", "1990-01-01T10:00:00", "2025-12-20T00:00:00", "2025-12-29T10:00:00", DisplayName = "week one beginning in December")]
    [DataRow("FREQ=MINUTELY;INTERVAL=15", "1990-03-15T09:00:00", "2026-10-04T23:07:00", "2026-10-04T23:15:00", DisplayName = "quarter hours since 1990")]
    [DataRow("FREQ=HOURLY;INTERVAL=5;BYHOUR=0,12", "1985-01-01T00:00:00", "2026-10-04T00:00:00", "2026-10-05T12:00:00", DisplayName = "every fifth hour at midnight or noon since 1985")]
    [DataRow("FREQ=SECONDLY;BYHOUR=9;BYMINUTE=30;BYSECOND=0", "2000-01-01T00:00:00", "2026-10-04T10:00:00", "2026-10-05T09:30:00", DisplayName = "one second a day since 2000")]
    public void GetNextOccurrence_WhenStartIsDecadesEarlier_ShouldReturnTheNextOccurrence(string rule, string start, string after, string expected)
    {
        DateTime? next = RecurrenceRule.Parse(rule).GetNextOccurrence(Instant(start), Instant(after));

        Assert.AreEqual(Instant(expected), next);
    }

    /// <summary>
    /// Verifies that a sub-daily rule allowing a single time of day reaches it from every half hour of the day, whether
    /// that time is the first or the last of the day or falls anywhere between.
    /// </summary>
    /// <param name="rule">The recurrence-rule text.</param>
    /// <param name="timeOfDay">The one time of day the rule allows, formatted <c>HH:mm:ss</c>.</param>
    [TestMethod]
    [DataRow("FREQ=MINUTELY;BYHOUR=0;BYMINUTE=0", "00:00:00", DisplayName = "the first minute of the day")]
    [DataRow("FREQ=MINUTELY;BYHOUR=1;BYMINUTE=3", "01:03:00", DisplayName = "minute 63 of the day")]
    [DataRow("FREQ=MINUTELY;BYHOUR=1;BYMINUTE=4", "01:04:00", DisplayName = "minute 64 of the day")]
    [DataRow("FREQ=MINUTELY;BYHOUR=2;BYMINUTE=7", "02:07:00", DisplayName = "minute 127 of the day")]
    [DataRow("FREQ=MINUTELY;BYHOUR=23;BYMINUTE=59", "23:59:00", DisplayName = "the last minute of the day")]
    [DataRow("FREQ=SECONDLY;BYHOUR=0;BYMINUTE=1;BYSECOND=3", "00:01:03", DisplayName = "second 63 of the day")]
    [DataRow("FREQ=SECONDLY;BYHOUR=12;BYMINUTE=0;BYSECOND=0", "12:00:00", DisplayName = "noon, second 43,200 of the day")]
    [DataRow("FREQ=SECONDLY;BYHOUR=23;BYMINUTE=59;BYSECOND=59", "23:59:59", DisplayName = "the last second of the day")]
    public void GetNextOccurrence_WhenSubDailyRuleAllowsOneTimeOfDay_ShouldReachItFromAnywhereInTheDay(string rule, string timeOfDay)
    {
        var start = new DateTime(2020, 1, 1);
        TimeSpan time = TimeSpan.Parse(timeOfDay, System.Globalization.CultureInfo.InvariantCulture);
        RecurrenceRule parsed = RecurrenceRule.Parse(rule);

        for (int halfHour = 0; halfHour < 48; halfHour++)
        {
            DateTime after = new DateTime(2020, 1, 2).AddMinutes(30 * halfHour).AddSeconds(17);
            DateTime sameDay = after.Date.Add(time);
            DateTime expected = sameDay > after ? sameDay : sameDay.AddDays(1);

            Assert.AreEqual(expected, parsed.GetNextOccurrence(start, after), $"{rule} after {after:s}");
        }
    }

    /// <summary>
    /// Verifies that the offset-preserving next occurrence of a series anchored decades earlier is found from a query
    /// expressed in another offset, and carries the offset of the series start.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenStartIsDecadesEarlier_ForDateTimeOffset_ShouldReturnTheNextOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTimeOffset(1990, 3, 15, 9, 30, 0, TimeSpan.FromHours(10));

        // 23:00 UTC on 4 October is 09:00 on 5 October at +10:00, half an hour before that day's occurrence.
        DateTimeOffset? next = rule.GetNextOccurrence(start, new DateTimeOffset(2026, 10, 4, 23, 0, 0, TimeSpan.Zero));

        Assert.AreEqual(new DateTimeOffset(2026, 10, 5, 9, 30, 0, TimeSpan.FromHours(10)), next);
        Assert.AreEqual(TimeSpan.FromHours(10), next?.Offset);
    }
}
