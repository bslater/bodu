// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.GetOccurrences.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Verifies that a daily rule bounded by a count yields consecutive days from the start.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenDailyWithCount_ShouldYieldConsecutiveDays()
    {
        DateTime[] actual = Occurrences("FREQ=DAILY;COUNT=5", new DateTime(1997, 9, 2, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 3, 9, 0, 0),
                new DateTime(1997, 9, 4, 9, 0, 0),
                new DateTime(1997, 9, 5, 9, 0, 0),
                new DateTime(1997, 9, 6, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a weekly rule with a <c>BYDAY</c> list yields the named weekdays in order.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWeeklyByDay_ShouldYieldNamedWeekdays()
    {
        DateTime[] actual = Occurrences("FREQ=WEEKLY;COUNT=4;BYDAY=TU,TH", new DateTime(1997, 9, 2, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 4, 9, 0, 0),
                new DateTime(1997, 9, 9, 9, 0, 0),
                new DateTime(1997, 9, 11, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that an every-other-week rule advances by two weeks between occurrences.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenWeeklyIntervalTwo_ShouldSkipAlternateWeeks()
    {
        DateTime[] actual = Occurrences("FREQ=WEEKLY;INTERVAL=2;COUNT=3;BYDAY=MO;WKST=SU", new DateTime(1997, 9, 1, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 1, 9, 0, 0),
                new DateTime(1997, 9, 15, 9, 0, 0),
                new DateTime(1997, 9, 29, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a monthly rule selecting the last Friday resolves the negative ordinal each month.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenMonthlyLastFriday_ShouldYieldLastFridayEachMonth()
    {
        DateTime[] actual = Occurrences("FREQ=MONTHLY;COUNT=3;BYDAY=-1FR", new DateTime(1997, 9, 5, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 26, 9, 0, 0),
                new DateTime(1997, 10, 31, 9, 0, 0),
                new DateTime(1997, 11, 28, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a monthly rule with <c>BYMONTHDAY</c> yields the same day of each month.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenMonthlyByMonthDay_ShouldYieldSameDayEachMonth()
    {
        DateTime[] actual = Occurrences("FREQ=MONTHLY;COUNT=3;BYMONTHDAY=2", new DateTime(1997, 9, 2, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 10, 2, 9, 0, 0),
                new DateTime(1997, 11, 2, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a monthly <c>BYSETPOS</c> rule selects the last weekday of each month.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenMonthlyBySetPosLast_ShouldYieldLastWeekday()
    {
        DateTime[] actual = Occurrences(
            "FREQ=MONTHLY;COUNT=3;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1",
            new DateTime(1997, 9, 30, 9, 0, 0),
            100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 30, 9, 0, 0),
                new DateTime(1997, 10, 31, 9, 0, 0),
                new DateTime(1997, 11, 28, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a yearly rule with no day rule yields the same month and day each year.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenYearly_ShouldYieldSameDateEachYear()
    {
        DateTime[] actual = Occurrences("FREQ=YEARLY;COUNT=3", new DateTime(1997, 6, 10, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 6, 10, 9, 0, 0),
                new DateTime(1998, 6, 10, 9, 0, 0),
                new DateTime(1999, 6, 10, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that a yearly rule with <c>BYMONTH</c> and an ordinal <c>BYDAY</c> yields US Thanksgiving.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenYearlyByMonthAndOrdinalByDay_ShouldYieldThanksgiving()
    {
        DateTime[] actual = Occurrences("FREQ=YEARLY;BYMONTH=11;BYDAY=4TH;COUNT=3", new DateTime(1997, 11, 27, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 11, 27, 9, 0, 0),
                new DateTime(1998, 11, 26, 9, 0, 0),
                new DateTime(1999, 11, 25, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that an <c>UNTIL</c>-bounded rule stops at the bound and excludes later candidates.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenUntilBounded_ShouldStopAtBound()
    {
        DateTime[] actual = Occurrences("FREQ=DAILY;UNTIL=19970905T090000", new DateTime(1997, 9, 2, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 2, 9, 0, 0),
                new DateTime(1997, 9, 3, 9, 0, 0),
                new DateTime(1997, 9, 4, 9, 0, 0),
                new DateTime(1997, 9, 5, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that the start instant is not emitted when it does not satisfy the rule.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenStartDoesNotMatch_ShouldSkipStart()
    {
        DateTime[] actual = Occurrences("FREQ=WEEKLY;COUNT=2;BYDAY=MO", new DateTime(1997, 9, 2, 9, 0, 0), 100);

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(1997, 9, 8, 9, 0, 0),
                new DateTime(1997, 9, 15, 9, 0, 0),
            },
            actual);
    }

    /// <summary>
    /// Verifies that expanding a sub-daily rule throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenSubDaily_ShouldThrowNotSupportedException()
    {
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=HOURLY;COUNT=3");

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = rule.GetOccurrences(new DateTime(1997, 9, 2, 9, 0, 0)).ToArray();
        });
    }
}
