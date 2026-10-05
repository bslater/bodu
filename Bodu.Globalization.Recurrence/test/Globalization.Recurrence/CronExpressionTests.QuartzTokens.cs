// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.QuartzTokens.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public partial class CronExpressionTests
{
    /// <summary>
    /// Verifies that <c>L</c> selects the last day of each month, February's in a leap year included.
    /// </summary>
    /// <param name="from">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("2026-01-15T00:00:00", "2026-01-31T09:00:00")]
    [DataRow("2026-02-01T00:00:00", "2026-02-28T09:00:00")]
    [DataRow("2024-02-01T00:00:00", "2024-02-29T09:00:00")]
    [DataRow("2026-01-31T09:00:00", "2026-02-28T09:00:00")]
    public void GetNextOccurrence_WhenLastDayOfMonth_ShouldReturnEachMonthsLastDay(string from, string expected)
    {
        CronExpression cron = CronExpression.Parse("0 9 L * *");

        Assert.AreEqual(Instant(expected), cron.GetNextOccurrence(Instant(from)));
    }

    /// <summary>
    /// Verifies that <c>L-n</c> counts back from each month's own last day, and that a month too short to hold the day
    /// is skipped.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="from">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("0 0 L-3 * *", "2026-02-01T00:00:00", "2026-02-25T00:00:00")]
    [DataRow("0 0 L-3 * *", "2024-02-01T00:00:00", "2024-02-26T00:00:00")]
    [DataRow("0 0 L-30 * *", "2026-02-01T00:00:00", "2026-03-01T00:00:00")]
    [DataRow("0 0 L-0 * *", "2026-04-01T00:00:00", "2026-04-30T00:00:00")]
    public void GetNextOccurrence_WhenDaysBeforeTheLastDay_ShouldCountBackFromTheMonthsEnd(string expression, string from, string expected)
    {
        Assert.AreEqual(Instant(expected), CronExpression.Parse(expression).GetNextOccurrence(Instant(from)));
    }

    /// <summary>
    /// Verifies that <c>nW</c> selects the weekday nearest day n without leaving the month: a Saturday the 1st moves
    /// forward to the Monday, a Sunday the last moves back to the Friday, and a month without day n is skipped.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="from">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("0 9 1W * *", "2026-02-01T00:00:00", "2026-02-02T09:00:00")]
    [DataRow("0 9 1W * *", "2026-08-01T00:00:00", "2026-08-03T09:00:00")]
    [DataRow("0 9 31W * *", "2026-05-01T00:00:00", "2026-05-29T09:00:00")]
    [DataRow("0 9 31W * *", "2026-06-01T00:00:00", "2026-07-31T09:00:00")]
    [DataRow("0 9 15W * *", "2026-03-01T00:00:00", "2026-03-16T09:00:00")]
    public void GetNextOccurrence_WhenNearestWeekdayToADay_ShouldStayWithinTheMonth(string expression, string from, string expected)
    {
        Assert.AreEqual(Instant(expected), CronExpression.Parse(expression).GetNextOccurrence(Instant(from)));
    }

    /// <summary>
    /// Verifies that <c>LW</c> and <c>L-nW</c> move a last day, or the day before it, that falls on a weekend to the
    /// nearest weekday of the same month.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="from">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("0 9 LW * *", "2026-01-01T00:00:00", "2026-01-30T09:00:00")]
    [DataRow("0 9 LW * *", "2026-05-01T00:00:00", "2026-05-29T09:00:00")]
    [DataRow("0 9 LW * *", "2026-06-01T00:00:00", "2026-06-30T09:00:00")]
    [DataRow("0 9 L-1W * *", "2026-11-01T00:00:00", "2026-11-30T09:00:00")]
    public void GetNextOccurrence_WhenWeekdayNearestTheLastDay_ShouldMoveAWeekendToAWeekday(string expression, string from, string expected)
    {
        Assert.AreEqual(Instant(expected), CronExpression.Parse(expression).GetNextOccurrence(Instant(from)));
    }

    /// <summary>
    /// Verifies that <c>dL</c> selects the last day of the month that falls on weekday d, whether d is a number, a name,
    /// or the seven that also stands for Sunday.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="from">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("0 9 * * 5L", "2026-01-01T00:00:00", "2026-01-30T09:00:00")]
    [DataRow("0 9 * * FRIL", "2026-01-31T00:00:00", "2026-02-27T09:00:00")]
    [DataRow("0 9 * * 7L", "2026-01-01T00:00:00", "2026-01-25T09:00:00")]
    [DataRow("0 9 * * sunl", "2026-01-01T00:00:00", "2026-01-25T09:00:00")]
    public void GetNextOccurrence_WhenLastWeekdayOfTheMonth_ShouldReturnTheLastSuchDay(string expression, string from, string expected)
    {
        Assert.AreEqual(Instant(expected), CronExpression.Parse(expression).GetNextOccurrence(Instant(from)));
    }

    /// <summary>
    /// Verifies that <c>d#k</c> selects the k-th weekday d of the month, and passes over months that have no k-th.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="from">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("0 9 * * 2#3", "2026-01-01T00:00:00", "2026-01-20T09:00:00")]
    [DataRow("0 9 * * 2#3", "2026-01-21T00:00:00", "2026-02-17T09:00:00")]
    [DataRow("0 9 * * 1#5", "2026-04-01T00:00:00", "2026-06-29T09:00:00")]
    [DataRow("0 9 * * MON#1", "2026-06-02T00:00:00", "2026-07-06T09:00:00")]
    public void GetNextOccurrence_WhenNthWeekdayOfTheMonth_ShouldSkipMonthsWithoutIt(string expression, string from, string expected)
    {
        Assert.AreEqual(Instant(expected), CronExpression.Parse(expression).GetNextOccurrence(Instant(from)));
    }

    /// <summary>
    /// Verifies that each token answers the previous occurrence as it answers the next, in a month before the bound.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="before">The instant the search works back from.</param>
    /// <param name="expected">The expected previous occurrence.</param>
    [TestMethod]
    [DataRow("0 9 L * *", "2026-03-15T00:00:00", "2026-02-28T09:00:00")]
    [DataRow("0 0 L-30 * *", "2026-03-01T00:00:00", "2026-01-01T00:00:00")]
    [DataRow("0 9 1W * *", "2026-08-05T00:00:00", "2026-08-03T09:00:00")]
    [DataRow("0 9 LW * *", "2026-02-15T00:00:00", "2026-01-30T09:00:00")]
    [DataRow("0 9 * * 5L", "2026-03-01T00:00:00", "2026-02-27T09:00:00")]
    [DataRow("0 9 * * 1#5", "2026-06-01T00:00:00", "2026-03-30T09:00:00")]
    public void GetPreviousOccurrence_WhenQuartzToken_ShouldReturnTheLatestSelectedDay(string expression, string before, string expected)
    {
        Assert.AreEqual(Instant(expected), CronExpression.Parse(expression).GetPreviousOccurrence(Instant(before)));
    }

    /// <summary>
    /// Verifies that <c>L-nW</c> moves a day counted back to the 1st of the month forward to the Monday when the 1st is
    /// a Sunday, and passes over a month too short to hold the day.
    /// </summary>
    [TestMethod]
    public void GetNextOccurrence_WhenWeekdayNearestADayCountedBackToTheFirst_ShouldStayWithinTheMonth()
    {
        CronExpression cron = CronExpression.Parse("0 9 L-30W * *");

        Assert.AreEqual(Instant("2026-01-01T09:00:00"), cron.GetNextOccurrence(Instant("2025-12-31T12:00:00")));
        Assert.AreEqual(Instant("2026-03-02T09:00:00"), cron.GetNextOccurrence(Instant("2026-01-01T09:00:00")));
    }

    /// <summary>
    /// Verifies that a token in one day field and values in the other combine by union, as two restricted day fields do,
    /// so <c>0 0 L * 1</c> fires on the last day of the month and on every Monday, and <c>0 0 15 * FRIL</c> on the 15th
    /// and on the last Friday.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="from">The instant the search starts from.</param>
    /// <param name="expected">The expected next occurrence.</param>
    [TestMethod]
    [DataRow("0 0 L * 1", "2026-01-27T00:00:00", "2026-01-31T00:00:00")]
    [DataRow("0 0 L * 1", "2026-01-31T00:00:00", "2026-02-02T00:00:00")]
    [DataRow("0 0 15 * FRIL", "2026-01-01T00:00:00", "2026-01-15T00:00:00")]
    [DataRow("0 0 15 * FRIL", "2026-01-15T00:00:00", "2026-01-30T00:00:00")]
    public void GetNextOccurrence_WhenTokenAndValuesBothRestrict_ShouldUseUnion(string expression, string from, string expected)
    {
        Assert.AreEqual(Instant(expected), CronExpression.Parse(expression).GetNextOccurrence(Instant(from)));
    }

    /// <summary>
    /// Verifies that <c>?</c> in either day field is the same schedule as <c>*</c>.
    /// </summary>
    /// <param name="withQuestionMark">The expression using <c>?</c>.</param>
    /// <param name="withStar">The same expression using <c>*</c>.</param>
    [TestMethod]
    [DataRow("0 0 ? * MON", "0 0 * * MON")]
    [DataRow("0 0 1 * ?", "0 0 1 * *")]
    [DataRow("0 0 ? * ?", "0 0 * * *")]
    [DataRow("0 0 0 ? * 5L", "0 0 0 * * 5L")]
    public void Parse_WhenQuestionMark_ShouldEqualStar(string withQuestionMark, string withStar)
    {
        CronExpression left = CronExpression.Parse(withQuestionMark);
        CronExpression right = CronExpression.Parse(withStar);

        Assert.AreEqual(right, left);
        Assert.AreEqual(right.ToString(), left.ToString());
    }

    /// <summary>
    /// Verifies that seeded schedules using every token, alone and beside values in the other day field, answer the
    /// next and previous occurrence, with and without <c>inclusive</c>, as a day-by-day reading of the tokens does.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetNextOccurrence_WhenSeededTokenSchedules_ShouldMatchTheReference()
    {
        var random = new Random(5545);
        for (int schedule = 0; schedule < 240; schedule++)
        {
            (string text, CronTokenReference reference) = RandomTokenSchedule(random);
            CronExpression cron = CronExpression.Parse(text);

            Assert.AreEqual(cron, CronExpression.Parse(cron.ToString()), $"'{text}' did not survive '{cron}'");

            for (int probe = 0; probe < 12; probe++)
            {
                DateTime at = new DateTime(1996, 1, 1).AddSeconds(random.NextInt64(0, 40L * 365 * 86400));
                foreach (bool inclusive in new[] { false, true })
                {
                    DateTime? next = reference.Next(at, inclusive);
                    DateTime? previous = reference.Previous(at, inclusive);

                    Assert.AreEqual(next, cron.GetNextOccurrence(at, inclusive), $"'{text}' next from {at:s}, inclusive {inclusive}");
                    Assert.AreEqual(previous, cron.GetPreviousOccurrence(at, inclusive), $"'{text}' previous from {at:s}, inclusive {inclusive}");

                    if (next is DateTime onNext)
                    {
                        Assert.AreEqual(onNext, cron.GetNextOccurrence(onNext, inclusive: true), $"'{text}' next on {onNext:s}");
                        Assert.AreEqual(reference.Next(onNext, false), cron.GetNextOccurrence(onNext), $"'{text}' next after {onNext:s}");
                    }

                    if (previous is DateTime onPrevious)
                    {
                        Assert.AreEqual(onPrevious, cron.GetPreviousOccurrence(onPrevious, inclusive: true), $"'{text}' previous on {onPrevious:s}");
                        Assert.AreEqual(reference.Previous(onPrevious, false), cron.GetPreviousOccurrence(onPrevious), $"'{text}' previous before {onPrevious:s}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Composes a seeded schedule that uses at least one Quartz token, as cron text and as the reference reading of it.
    /// </summary>
    /// <param name="random">The seeded source.</param>
    /// <returns>The cron text and its reference.</returns>
    private static (string Text, CronTokenReference Reference) RandomTokenSchedule(Random random)
    {
        int[] seconds = random.Next(3) == 0 ? Pick(random, 0, 59, 2) : [0];
        int[] minutes = Pick(random, 0, 59, random.Next(1, 3));
        int[] hours = random.Next(4) == 0 ? Enumerable.Range(0, 24).ToArray() : Pick(random, 0, 23, random.Next(1, 3));
        int[] months = random.Next(2) == 0 ? Enumerable.Range(1, 12).ToArray() : Pick(random, 1, 12, random.Next(1, 4));

        string dayOfMonth = random.Next(6) switch
        {
            0 => "L",
            1 => $"L-{random.Next(0, 31)}",
            2 => random.Next(2) == 0 ? "LW" : $"L-{random.Next(0, 31)}W",
            3 => $"{random.Next(1, 32)}W",
            4 => random.Next(2) == 0 ? "*" : "?",
            _ => string.Join(',', Pick(random, 1, 31, random.Next(1, 3))),
        };

        string[] names = ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"];
        int weekday = random.Next(8);
        string day = random.Next(3) == 0 ? names[weekday % 7] : weekday.ToString(CultureInfo.InvariantCulture);
        string dayOfWeek = random.Next(5) switch
        {
            0 => $"{day}L",
            1 => $"{day}#{random.Next(1, 6)}",
            2 => string.Join(',', Pick(random, 0, 7, random.Next(1, 3))),
            _ => random.Next(2) == 0 ? "*" : "?",
        };

        // Keep at least one token in play, so the sweep spends its schedules on the tokens.
        if (!dayOfMonth.Contains('L', StringComparison.Ordinal) && !dayOfMonth.Contains('W', StringComparison.Ordinal)
            && !dayOfWeek.Contains('L', StringComparison.Ordinal) && !dayOfWeek.Contains('#', StringComparison.Ordinal))
        {
            dayOfWeek = $"{day}#{random.Next(1, 6)}";
        }

        string time = $"{string.Join(',', minutes)} {(hours.Length == 24 ? "*" : string.Join(',', hours))}";
        string monthText = months.Length == 12 ? "*" : string.Join(',', months);
        string text = seconds.Length == 1 && seconds[0] == 0 && random.Next(2) == 0
            ? $"{time} {dayOfMonth} {monthText} {dayOfWeek}"
            : $"{string.Join(',', seconds)} {time} {dayOfMonth} {monthText} {dayOfWeek}";
        if (text.Split(' ').Length == 5)
        {
            seconds = [0];
        }

        return (text, new CronTokenReference(seconds, minutes, hours, months, dayOfMonth, dayOfWeek));
    }

    /// <summary>
    /// Picks distinct values from a range in ascending order.
    /// </summary>
    /// <param name="random">The seeded source.</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="max">The inclusive maximum.</param>
    /// <param name="count">The number of values.</param>
    /// <returns>The values.</returns>
    private static int[] Pick(Random random, int min, int max, int count) =>
        Enumerable.Range(min, max - min + 1).OrderBy(_ => random.Next()).Take(count).Order().ToArray();

    /// <summary>
    /// Reads an instant written as <c>yyyy-MM-ddTHH:mm:ss</c>.
    /// </summary>
    /// <param name="text">The instant text.</param>
    /// <returns>The instant.</returns>
    private static DateTime Instant(string text) =>
        DateTime.ParseExact(text, "s", CultureInfo.InvariantCulture);
}
