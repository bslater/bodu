// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.GetPreviousOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that the previous occurrence of a daily rule is the most recent expansion before the query instant.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenDaily_ShouldReturnMostRecentOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        DateTime? previous = rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 5, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 4, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that a query exactly on an occurrence is excluded by default and included with the inclusive flag.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeEqualsOccurrence_ShouldHonorInclusiveFlag()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);
        var occurrence = new DateTime(2026, 1, 3, 9, 0, 0);

        Assert.AreEqual(new DateTime(2026, 1, 2, 9, 0, 0), rule.GetPreviousOccurrence(start, occurrence));
        Assert.AreEqual(occurrence, rule.GetPreviousOccurrence(start, occurrence, inclusive: true));
    }

    /// <summary>
    /// Verifies that no occurrence precedes the series start, and that the start itself is answered only inclusively.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenBeforeSeriesStart_ShouldReturnNull()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        Assert.IsNull(rule.GetPreviousOccurrence(start, start.AddDays(-1)));
        Assert.IsNull(rule.GetPreviousOccurrence(start, start));
        Assert.AreEqual(start, rule.GetPreviousOccurrence(start, start, inclusive: true));
    }

    /// <summary>
    /// Verifies that a count-bounded rule answers its final occurrence for any later query instant.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenAfterCountBoundedSeriesEnds_ShouldReturnFinalOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY;COUNT=3");
        var start = new DateTime(2026, 1, 1, 9, 0, 0);

        DateTime? previous = rule.GetPreviousOccurrence(start, new DateTime(2026, 6, 1, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 3, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that a weekly <c>BYDAY</c> rule answers the most recent matching weekday.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenWeeklyByDay_ShouldReturnMostRecentMatchingWeekday()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO,FR");
        var start = new DateTime(2026, 1, 5, 9, 0, 0);

        DateTime? previous = rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 14, 0, 0, 0));

        Assert.AreEqual(new DateTime(2026, 1, 12, 9, 0, 0), previous);
    }

    /// <summary>
    /// Verifies that the offset overload preserves the start's offset on the answered occurrence.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenStartHasOffset_ShouldPreserveStartOffset()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.FromHours(10));

        DateTimeOffset? previous = rule.GetPreviousOccurrence(start, new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.FromHours(10)));

        Assert.AreEqual(new DateTimeOffset(2026, 1, 4, 9, 0, 0, TimeSpan.FromHours(10)), previous);
        Assert.AreEqual(TimeSpan.FromHours(10), previous!.Value.Offset);
    }

    /// <summary>
    /// Verifies the due-ness recipe coalesces missed occurrences: a daily schedule evaluated five days late answers
    /// the same due boolean as one evaluated a minute late.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenOccurrencesWereMissed_ShouldCoalesceToSingleDueAnswer()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTime(2026, 1, 1, 2, 0, 0);
        var lastCompleted = new DateTime(2026, 1, 2, 2, 0, 0);

        bool dueShortlyAfter = lastCompleted < rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 3, 2, 1, 0), inclusive: true);
        bool dueMuchLater = lastCompleted < rule.GetPreviousOccurrence(start, new DateTime(2026, 1, 8, 2, 0, 0), inclusive: true);

        Assert.IsTrue(dueShortlyAfter);
        Assert.AreEqual(dueShortlyAfter, dueMuchLater);
    }

    /// <summary>
    /// Verifies that a series anchored decades before the query instant answers its previous occurrence, including a
    /// <c>BYWEEKNO</c> week that begins in the calendar year before the one it is numbered in and sub-daily series
    /// whose limits allow only some times of day or whose <c>UNTIL</c> is itself an occurrence.
    /// </summary>
    /// <param name="rule">The recurrence-rule text.</param>
    /// <param name="start">The series start, in sortable format.</param>
    /// <param name="before">The query instant, in sortable format.</param>
    /// <param name="expected">The expected previous occurrence, in sortable format.</param>
    [TestMethod]
    [DataRow("FREQ=DAILY", "1990-03-15T09:30:00", "2026-10-04T12:00:00", "2026-10-04T09:30:00", DisplayName = "daily since 1990")]
    [DataRow("FREQ=WEEKLY;BYDAY=MO", "1993-02-08T09:00:00", "2026-10-04T12:00:00", "2026-09-28T09:00:00", DisplayName = "Mondays since 1993")]
    [DataRow("FREQ=MONTHLY;BYMONTHDAY=-1", "1975-01-31T08:00:00", "2026-10-04T00:00:00", "2026-09-30T08:00:00", DisplayName = "month ends since 1975")]
    [DataRow("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=29", "1960-02-29T00:00:00", "2026-10-04T00:00:00", "2024-02-29T00:00:00", DisplayName = "leap days since 1960")]
    [DataRow("FREQ=YEARLY;BYWEEKNO=1;BYDAY=MO", "1990-01-01T10:00:00", "2026-12-31T00:00:00", "2025-12-29T10:00:00", DisplayName = "week one beginning in December")]
    [DataRow("FREQ=DAILY;UNTIL=20100630T000000", "2000-01-01T09:00:00", "2026-10-04T00:00:00", "2010-06-29T09:00:00", DisplayName = "daily until long before the query")]
    [DataRow("FREQ=MINUTELY;INTERVAL=15", "1990-03-15T09:00:00", "2026-10-04T23:07:00", "2026-10-04T23:00:00", DisplayName = "quarter hours since 1990")]
    [DataRow("FREQ=HOURLY;INTERVAL=5;BYHOUR=0,12", "1985-01-01T00:00:00", "2026-10-04T00:00:00", "2026-10-03T00:00:00", DisplayName = "every fifth hour at midnight or noon since 1985")]
    [DataRow("FREQ=SECONDLY;BYHOUR=9;BYMINUTE=30;BYSECOND=0", "2000-01-01T00:00:00", "2026-10-04T10:00:00", "2026-10-04T09:30:00", DisplayName = "one second a day since 2000")]
    [DataRow("FREQ=MINUTELY;INTERVAL=45;UNTIL=20100630T000000", "2000-01-01T09:00:00", "2026-10-04T00:00:00", "2010-06-30T00:00:00", DisplayName = "every 45 minutes until an occurrence long before the query")]
    public void GetPreviousOccurrence_WhenStartIsDecadesEarlier_ShouldReturnThePreviousOccurrence(string rule, string start, string before, string expected)
    {
        DateTime? previous = RecurrenceRule.Parse(rule).GetPreviousOccurrence(Instant(start), Instant(before));

        Assert.AreEqual(Instant(expected), previous);
    }

    /// <summary>
    /// Verifies that a sub-daily rule allowing a single time of day reaches it back from every half hour of the day,
    /// whether that time is the first or the last of the day or falls anywhere between.
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
    public void GetPreviousOccurrence_WhenSubDailyRuleAllowsOneTimeOfDay_ShouldReachItFromAnywhereInTheDay(string rule, string timeOfDay)
    {
        var start = new DateTime(2020, 1, 1);
        TimeSpan time = TimeSpan.Parse(timeOfDay, System.Globalization.CultureInfo.InvariantCulture);
        RecurrenceRule parsed = RecurrenceRule.Parse(rule);

        for (int halfHour = 0; halfHour < 48; halfHour++)
        {
            DateTime before = new DateTime(2020, 1, 3).AddMinutes(30 * halfHour).AddSeconds(17);
            DateTime sameDay = before.Date.Add(time);
            DateTime expected = sameDay < before ? sameDay : sameDay.AddDays(-1);

            Assert.AreEqual(expected, parsed.GetPreviousOccurrence(start, before), $"{rule} before {before:s}");
        }
    }

    /// <summary>
    /// Verifies that the offset-preserving previous occurrence of a series anchored decades earlier is found from a
    /// query expressed in another offset, and carries the offset of the series start.
    /// </summary>
    [TestMethod]
    public void GetPreviousOccurrence_WhenStartIsDecadesEarlier_ForDateTimeOffset_ShouldReturnThePreviousOccurrence()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=DAILY");
        var start = new DateTimeOffset(1990, 3, 15, 9, 30, 0, TimeSpan.FromHours(10));

        // 23:00 UTC on 4 October is 09:00 on 5 October at +10:00, half an hour before that day's occurrence.
        DateTimeOffset? previous = rule.GetPreviousOccurrence(start, new DateTimeOffset(2026, 10, 4, 23, 0, 0, TimeSpan.Zero));

        Assert.AreEqual(new DateTimeOffset(2026, 10, 4, 9, 30, 0, TimeSpan.FromHours(10)), previous);
        Assert.AreEqual(TimeSpan.FromHours(10), previous?.Offset);
    }
}
