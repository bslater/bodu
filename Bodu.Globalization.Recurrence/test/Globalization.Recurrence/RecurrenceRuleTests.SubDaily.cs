// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.SubDaily.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>The number of occurrences the reference-model tests compare at most per rule.</summary>
    private const int ReferenceModelMaximum = 400;

    /// <summary>
    /// Gets the RFC 5545 §3.8.5.3 examples at the sub-daily frequencies, with the occurrences the standard lists.
    /// </summary>
    /// <value>The example rows.</value>
    /// <remarks>
    /// The first example states <c>UNTIL</c> in UTC against a start in New York's zone. This library compares
    /// <c>UNTIL</c> as a wall clock, which gives the three occurrences the RFC lists; resolving the zone would end the
    /// series at 13:00 local time, after two.
    /// </remarks>
    public static IEnumerable<object[]> Rfc5545SubDailyExamples
    {
        get
        {
            DateTime T(int hour, int minute) => new(1997, 9, 2, hour, minute, 0);

            RRuleExpansionKat[] rows =
            [
                new("every 3 hours from 9:00 AM to 5:00 PM on a specific day", "FREQ=HOURLY;INTERVAL=3;UNTIL=19970902T170000Z", T(9, 0), 10,
                    [T(9, 0), T(12, 0), T(15, 0)]),
                new("every 15 minutes for 6 occurrences", "FREQ=MINUTELY;INTERVAL=15;COUNT=6", T(9, 0), 10,
                    [T(9, 0), T(9, 15), T(9, 30), T(9, 45), T(10, 0), T(10, 15)]),
                new("every hour and a half for 4 occurrences", "FREQ=MINUTELY;INTERVAL=90;COUNT=4", T(9, 0), 10,
                    [T(9, 0), T(10, 30), T(12, 0), T(13, 30)]),
            ];

            foreach (RRuleExpansionKat row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Gets sub-daily rules that exercise each expansion and limit, with the occurrences python-dateutil produces for
    /// them.
    /// </summary>
    /// <value>The rows.</value>
    public static IEnumerable<object[]> SubDailyExpansionsAndLimits
    {
        get
        {
            DateTime T(int y, int mo, int d, int h, int mi, int s = 0) => new(y, mo, d, h, mi, s);

            RRuleExpansionKat[] rows =
            [
                new("hourly takes its minute and second from the start", "FREQ=HOURLY;COUNT=3", T(2020, 1, 1, 9, 30, 15), 10,
                    [T(2020, 1, 1, 9, 30, 15), T(2020, 1, 1, 10, 30, 15), T(2020, 1, 1, 11, 30, 15)]),
                new("BYMINUTE and BYSECOND expand an hourly period", "FREQ=HOURLY;BYMINUTE=0,30;BYSECOND=0,15;COUNT=6", T(2020, 1, 1, 9, 10), 10,
                    [T(2020, 1, 1, 9, 30), T(2020, 1, 1, 9, 30, 15), T(2020, 1, 1, 10, 0), T(2020, 1, 1, 10, 0, 15), T(2020, 1, 1, 10, 30), T(2020, 1, 1, 10, 30, 15)]),
                new("BYSECOND expands a minutely period", "FREQ=MINUTELY;BYSECOND=0,30;COUNT=4", T(2020, 1, 1, 9, 0, 10), 10,
                    [T(2020, 1, 1, 9, 0, 30), T(2020, 1, 1, 9, 1), T(2020, 1, 1, 9, 1, 30), T(2020, 1, 1, 9, 2)]),
                new("secondly steps by its interval", "FREQ=SECONDLY;INTERVAL=20;COUNT=4", T(2020, 1, 1, 9, 0, 50), 10,
                    [T(2020, 1, 1, 9, 0, 50), T(2020, 1, 1, 9, 1, 10), T(2020, 1, 1, 9, 1, 30), T(2020, 1, 1, 9, 1, 50)]),
                new("BYHOUR limits every fifth hour to midnight and noon", "FREQ=HOURLY;INTERVAL=5;BYHOUR=0,12;COUNT=4", T(2020, 1, 1, 0, 0), 10,
                    [T(2020, 1, 1, 0, 0), T(2020, 1, 3, 12, 0), T(2020, 1, 6, 0, 0), T(2020, 1, 8, 12, 0)]),
                new("BYMINUTE limits every seventh minute to the hour", "FREQ=MINUTELY;INTERVAL=7;BYMINUTE=0;COUNT=3", T(2020, 1, 1, 0, 0), 10,
                    [T(2020, 1, 1, 0, 0), T(2020, 1, 1, 7, 0), T(2020, 1, 1, 14, 0)]),
                new("BYSECOND limits every seventh second to the minute", "FREQ=SECONDLY;INTERVAL=7;BYSECOND=0;COUNT=3", T(2020, 1, 1, 0, 0), 10,
                    [T(2020, 1, 1, 0, 0), T(2020, 1, 1, 0, 7), T(2020, 1, 1, 0, 14)]),
                new("BYDAY limits to weekends", "FREQ=HOURLY;INTERVAL=6;BYDAY=SA,SU;COUNT=5", T(2020, 1, 3, 18, 0), 10,
                    [T(2020, 1, 4, 0, 0), T(2020, 1, 4, 6, 0), T(2020, 1, 4, 12, 0), T(2020, 1, 4, 18, 0), T(2020, 1, 5, 0, 0)]),
                new("BYMONTH and BYMONTHDAY limit to leap days", "FREQ=HOURLY;BYMONTH=2;BYMONTHDAY=29;BYHOUR=9;COUNT=2", T(2021, 1, 1, 0, 0), 10,
                    [T(2024, 2, 29, 9, 0), T(2028, 2, 29, 9, 0)]),
                new("BYYEARDAY limits to the last day of the year", "FREQ=MINUTELY;INTERVAL=30;BYYEARDAY=-1;COUNT=3", T(2020, 6, 1, 0, 0), 10,
                    [T(2020, 12, 31, 0, 0), T(2020, 12, 31, 0, 30), T(2020, 12, 31, 1, 0)]),
                new("a BYDAY ordinal has no meaning below a month", "FREQ=HOURLY;INTERVAL=12;BYDAY=1SA;COUNT=3", T(2033, 10, 1, 22, 27, 55), 10,
                    [T(2033, 10, 1, 22, 27, 55), T(2033, 10, 8, 10, 27, 55), T(2033, 10, 8, 22, 27, 55)]),
                new("BYSETPOS selects within each period", "FREQ=HOURLY;BYMINUTE=0,15,30,45;BYSETPOS=-1;COUNT=3", T(2020, 1, 1, 9, 0), 10,
                    [T(2020, 1, 1, 9, 45), T(2020, 1, 1, 10, 45), T(2020, 1, 1, 11, 45)]),
                new("one second a day", "FREQ=SECONDLY;BYHOUR=9;BYMINUTE=30;BYSECOND=0;COUNT=3", T(2000, 1, 1, 0, 0), 10,
                    [T(2000, 1, 1, 9, 30), T(2000, 1, 2, 9, 30), T(2000, 1, 3, 9, 30)]),
            ];

            foreach (RRuleExpansionKat row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Gets seeded, randomly composed sub-daily rules for the reference-model tests, each with the horizon the model
    /// enumerates it to.
    /// </summary>
    /// <value>
    /// One row per rule, at every sub-daily frequency, with intervals that do and do not divide a day, every limit and
    /// expansion, <c>BYSETPOS</c>, <c>COUNT</c> and <c>UNTIL</c>, and starts of both <see cref="DateTimeKind.Utc" /> and
    /// unspecified kind. The horizon covers at most 40,000 periods and 30 years.
    /// </value>
    public static IEnumerable<object[]> RandomSubDailyRules
    {
        get
        {
            var random = new Random(5545);
            string[] frequencies = ["HOURLY", "MINUTELY", "SECONDLY"];
            long[] units = [TimeSpan.TicksPerHour, TimeSpan.TicksPerMinute, TimeSpan.TicksPerSecond];
            int[][] intervals =
            [
                [1, 1, 2, 3, 5, 7, 12, 25, 49],
                [1, 1, 2, 7, 15, 45, 90, 97, 1441],
                [1, 1, 3, 7, 30, 59, 61, 3600, 86401],
            ];

            for (int row = 0; row < 120; row++)
            {
                int frequency = random.Next(3);
                int interval = intervals[frequency][random.Next(intervals[frequency].Length)];
                var start = new DateTime(
                    random.Next(1990, 2036),
                    random.Next(1, 13),
                    random.Next(1, 29),
                    random.Next(24),
                    random.Next(60),
                    random.Next(60),
                    row % 3 == 0 ? DateTimeKind.Utc : DateTimeKind.Unspecified);

                var parts = new List<string> { $"FREQ={frequencies[frequency]}" };
                if (interval > 1)
                {
                    parts.Add(string.Create(CultureInfo.InvariantCulture, $"INTERVAL={interval}"));
                }

                AddPart(random, parts, 0.35, "BYHOUR", RandomValues(random, Enumerable.Range(0, 24), 5, start.Hour));
                AddPart(random, parts, 0.35, "BYMINUTE", RandomValues(random, Enumerable.Range(0, 60), 4, start.Minute));
                AddPart(random, parts, 0.30, "BYSECOND", RandomValues(random, Enumerable.Range(0, 61), 4, start.Second));
                AddPart(random, parts, 0.25, "BYDAY", RandomWeekDays(random));
                AddPart(random, parts, 0.20, "BYMONTH", RandomValues(random, Enumerable.Range(1, 12), 3, start.Month));
                AddPart(random, parts, 0.20, "BYMONTHDAY", RandomValues(random, Enumerable.Range(1, 28).Concat([-1, -2, -3]), 3, start.Day));
                AddPart(random, parts, 0.10, "BYYEARDAY", RandomValues(random, Enumerable.Range(1, 365).Append(-1), 2, start.DayOfYear));
                AddPart(random, parts, 0.15, "BYSETPOS", RandomValues(random, [1, 2, -1, -2], 2, 1));

                double bound = random.NextDouble();
                if (bound < 0.2)
                {
                    parts.Add(string.Create(CultureInfo.InvariantCulture, $"COUNT={random.Next(1, 60)}"));
                }
                else if (bound < 0.4)
                {
                    DateTime until = start.AddTicks(random.NextInt64(TimeSpan.TicksPerHour, 400 * TimeSpan.TicksPerDay));
                    parts.Add(string.Create(CultureInfo.InvariantCulture, $"UNTIL={until:yyyyMMdd'T'HHmmss}"));
                }

                long span = Math.Min(40_000L * interval * units[frequency], 30 * 365 * TimeSpan.TicksPerDay);
                var horizon = new DateTime(Math.Min(start.Ticks + span, DateTime.MaxValue.Ticks), start.Kind);
                string rule = string.Join(';', parts);

                yield return [new RecurrenceAnchorKat(string.Create(CultureInfo.InvariantCulture, $"#{row} {rule}"), rule, start, horizon)];
            }
        }
    }

    /// <summary>
    /// Verifies that each RFC 5545 §3.8.5.3 example at a sub-daily frequency produces exactly the occurrences the
    /// standard lists.
    /// </summary>
    /// <param name="kat">The example under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Rfc5545SubDailyExamples),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenRfc5545SubDailyExample_ShouldMatchTheStandard(RRuleExpansionKat kat)
    {
        DateTime[] actual = Occurrences(kat.Rule, kat.Start, kat.Take);

        CollectionAssert.AreEqual(kat.Expected, actual, kat.Rule);
    }

    /// <summary>
    /// Verifies that RFC 5545's two forms of "every 20 minutes from 9:00 AM to 4:40 PM every day", one minutely and one
    /// daily, produce the same occurrences: 24 a day, from 9:00 to 16:40.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenEveryTwentyMinutesDuringTheDay_ShouldMatchTheDailyForm()
    {
        var start = new DateTime(1997, 9, 2, 9, 0, 0);

        DateTime[] minutely = Occurrences("FREQ=MINUTELY;INTERVAL=20;BYHOUR=9,10,11,12,13,14,15,16", start, 24 * 7);
        DateTime[] daily = Occurrences("FREQ=DAILY;BYHOUR=9,10,11,12,13,14,15,16;BYMINUTE=0,20,40", start, 24 * 7);

        CollectionAssert.AreEqual(daily, minutely);
        Assert.AreEqual(new DateTime(1997, 9, 2, 16, 40, 0), minutely[23]);
        Assert.AreEqual(new DateTime(1997, 9, 3, 9, 0, 0), minutely[24]);
        Assert.AreEqual(new DateTime(1997, 9, 8, 16, 40, 0), minutely[^1]);
    }

    /// <summary>
    /// Verifies that each sub-daily expansion and limit produces the occurrences python-dateutil produces for it.
    /// </summary>
    /// <param name="kat">The rule under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(SubDailyExpansionsAndLimits),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenSubDailyRuleExpandsOrLimits_ShouldMatchReference(RRuleExpansionKat kat)
    {
        DateTime[] actual = Occurrences(kat.Rule, kat.Start, kat.Take);

        CollectionAssert.AreEqual(kat.Expected, actual, kat.Rule);
    }

    /// <summary>
    /// Verifies that a sub-daily rule whose interval never brings its periods to a time of day its limits allow ends
    /// at once with no occurrences, rather than searching to the end of the calendar.
    /// </summary>
    /// <param name="rule">The rule text.</param>
    [TestMethod]
    [DataRow("FREQ=MINUTELY;INTERVAL=15;BYMINUTE=10")]
    [DataRow("FREQ=HOURLY;INTERVAL=24;BYHOUR=10")]
    [DataRow("FREQ=SECONDLY;INTERVAL=86400;BYSECOND=30")]
    [DataRow("FREQ=HOURLY;INTERVAL=4;BYHOUR=10,14,22")]
    public void GetOccurrences_WhenSubDailyIntervalNeverReachesAnAllowedTime_ShouldTerminateEmpty(string rule)
    {
        var start = new DateTime(2020, 1, 1, 9, 0, 0);

        DateTime[] occurrences = Occurrences(rule, start, 1);
        DateTime? previous = RecurrenceRule.Parse(rule).GetPreviousOccurrence(start, DateTime.MaxValue, inclusive: true);

        Assert.AreEqual(0, occurrences.Length);
        Assert.IsNull(previous);
    }

    /// <summary>
    /// Verifies that a sub-daily rule whose <c>BYSETPOS</c> selects nothing from a period, and so nothing from any,
    /// ends at once with no occurrences.
    /// </summary>
    /// <param name="rule">The rule text.</param>
    [TestMethod]
    [DataRow("FREQ=SECONDLY;BYSETPOS=2")]
    [DataRow("FREQ=HOURLY;BYMINUTE=0,30;BYSETPOS=3,-3")]
    public void GetOccurrences_WhenSubDailySetPositionSelectsNothing_ShouldTerminateEmpty(string rule)
    {
        var start = new DateTime(2020, 1, 1, 9, 0, 0);

        DateTime[] occurrences = Occurrences(rule, start, 1);
        DateTime? previous = RecurrenceRule.Parse(rule).GetPreviousOccurrence(start, DateTime.MaxValue, inclusive: true);

        Assert.AreEqual(0, occurrences.Length);
        Assert.IsNull(previous);
    }

    /// <summary>
    /// Verifies that a sub-daily rule running into the end of the calendar produces the occurrences before it and then
    /// ends, forward and back.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenSubDailyRuleReachesTheEndOfTheCalendar_ShouldStopThere()
    {
        var start = new DateTime(9999, 12, 31, 22, 30, 0);
        RecurrenceRule rule = RecurrenceRule.Parse("FREQ=HOURLY;BYMINUTE=0,59;BYSECOND=59");

        DateTime[] occurrences = rule.GetOccurrences(start).ToArray();
        DateTime? last = rule.GetPreviousOccurrence(start, DateTime.MaxValue, inclusive: true);

        CollectionAssert.AreEqual(
            new[] { new DateTime(9999, 12, 31, 22, 59, 59), new DateTime(9999, 12, 31, 23, 0, 59), new DateTime(9999, 12, 31, 23, 59, 59) },
            occurrences);
        Assert.AreEqual(new DateTime(9999, 12, 31, 23, 59, 59), last);
    }

    /// <summary>
    /// Verifies that a sub-daily rule whose interval spans millennia stops at the end of the calendar, forward and back:
    /// an hourly interval of <see cref="int.MaxValue" /> passes it at the first step, and a minutely one, about 4,083
    /// years, at the second.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenSubDailyIntervalSpansMillennia_ShouldStopAtTheEndOfTheCalendar()
    {
        var start = new DateTime(2020, 1, 1, 9, 0, 0);
        RecurrenceRule hourly = RecurrenceRule.Parse($"FREQ=HOURLY;INTERVAL={int.MaxValue}");
        RecurrenceRule minutely = RecurrenceRule.Parse($"FREQ=MINUTELY;INTERVAL={int.MaxValue}");

        DateTime[] hourlyOccurrences = hourly.GetOccurrences(start).ToArray();
        DateTime[] minutelyOccurrences = minutely.GetOccurrences(start).ToArray();

        CollectionAssert.AreEqual(new[] { start }, hourlyOccurrences);
        CollectionAssert.AreEqual(new[] { start, start.AddMinutes(int.MaxValue) }, minutelyOccurrences);
        Assert.AreEqual(start, hourly.GetPreviousOccurrence(start, DateTime.MaxValue));
        Assert.AreEqual(start.AddMinutes(int.MaxValue), minutely.GetPreviousOccurrence(start, DateTime.MaxValue));
    }

    /// <summary>
    /// Verifies that a <c>BYSECOND</c> value of 60, the leap second RFC 5545 allows, stands for the last second of the
    /// minute, both where it expands a period and where it limits one.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenSubDailyBySecondIsSixty_ShouldUseTheLastSecondOfTheMinute()
    {
        var start = new DateTime(2020, 1, 1, 9, 0, 0);

        DateTime[] expanded = Occurrences("FREQ=MINUTELY;BYSECOND=60;COUNT=2", start, 10);
        DateTime[] limited = Occurrences("FREQ=SECONDLY;BYSECOND=60;COUNT=2", start, 10);

        CollectionAssert.AreEqual(new[] { new DateTime(2020, 1, 1, 9, 0, 59), new DateTime(2020, 1, 1, 9, 1, 59) }, expanded);
        CollectionAssert.AreEqual(expanded, limited);
    }

    /// <summary>
    /// Verifies that the occurrences of a sub-daily rule anchored at a UTC start are UTC instants.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenSubDailyStartIsUtc_ShouldReturnUtcInstants()
    {
        var start = new DateTime(2020, 1, 1, 23, 0, 0, DateTimeKind.Utc);

        DateTime[] occurrences = Occurrences("FREQ=MINUTELY;INTERVAL=45;COUNT=3", start, 10);

        CollectionAssert.AreEqual(
            new[] { start, new DateTime(2020, 1, 1, 23, 45, 0), new DateTime(2020, 1, 2, 0, 30, 0) },
            occurrences);
        Assert.IsTrue(occurrences.All(o => o.Kind == DateTimeKind.Utc));
    }

    /// <summary>
    /// Verifies that the <see cref="DateTimeOffset" /> overload of a sub-daily rule steps through the start's wall clock
    /// and carries the start's offset onto every occurrence.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenSubDailyForDateTimeOffset_ShouldCarryTheStartOffset()
    {
        var offset = new TimeSpan(10, 0, 0);
        var start = new DateTimeOffset(2020, 1, 1, 20, 0, 0, offset);

        DateTimeOffset[] occurrences = RecurrenceRule.Parse("FREQ=HOURLY;INTERVAL=8;COUNT=3").GetOccurrences(start).ToArray();

        CollectionAssert.AreEqual(
            new[] { start, new DateTimeOffset(2020, 1, 2, 4, 0, 0, offset), new DateTimeOffset(2020, 1, 2, 12, 0, 0, offset) },
            occurrences);
        Assert.IsTrue(occurrences.All(o => o.Offset == offset));
    }

    /// <summary>
    /// Verifies that each seeded random sub-daily rule produces, up to its horizon, exactly the occurrences of the
    /// reference model that visits every period, kind included.
    /// </summary>
    /// <param name="kat">The rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RandomSubDailyRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenRandomSubDailyRule_ShouldMatchTheReferenceModel(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> expected = SubDailyReference.Enumerate(rule, kat.Start, kat.Horizon, ReferenceModelMaximum, out DateTime complete);

        DateTime[] actual = rule.GetOccurrences(kat.Start).TakeWhile(o => o <= complete).Take(ReferenceModelMaximum + 1).ToArray();

        CollectionAssert.AreEqual(expected, actual, string.Create(CultureInfo.InvariantCulture, $"{kat.Rule} from {kat.Start:O}"));
        Assert.IsTrue(actual.All(o => o.Kind == kat.Start.Kind), kat.Rule);
    }

    /// <summary>
    /// Verifies that the next occurrence of each seeded random sub-daily rule, queried on, around, and between the
    /// reference model's occurrences and at random instants, is the model's, with and without the inclusive flag.
    /// </summary>
    /// <param name="kat">The rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RandomSubDailyRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenRandomSubDailyRule_ShouldMatchTheReferenceModel(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> model = SubDailyReference.Enumerate(rule, kat.Start, kat.Horizon, ReferenceModelMaximum, out DateTime complete);

        foreach (DateTime after in ReferenceModelProbes(kat, model, complete))
        {
            foreach (bool inclusive in new[] { false, true })
            {
                int index = inclusive ? LowerBound(model, after) : UpperBound(model, after);

                DateTime? actual = rule.GetNextOccurrence(kat.Start, after, inclusive);

                // Past the instant up to which the model is complete, it says only that nothing comes before it.
                string context = Describe(kat, after, inclusive);
                if (index < model.Count)
                {
                    Assert.AreEqual(model[index], actual, context);
                }
                else
                {
                    Assert.IsTrue(actual is null || actual > complete, context);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that the previous occurrence of each seeded random sub-daily rule, queried on, around, and between the
    /// reference model's occurrences and at random instants, is the model's, with and without the inclusive flag.
    /// </summary>
    /// <param name="kat">The rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RandomSubDailyRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenRandomSubDailyRule_ShouldMatchTheReferenceModel(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> model = SubDailyReference.Enumerate(rule, kat.Start, kat.Horizon, ReferenceModelMaximum, out DateTime complete);

        foreach (DateTime before in ReferenceModelProbes(kat, model, complete))
        {
            foreach (bool inclusive in new[] { false, true })
            {
                int index = (inclusive ? UpperBound(model, before) : LowerBound(model, before)) - 1;
                DateTime? expected = index >= 0 ? model[index] : null;

                DateTime? actual = rule.GetPreviousOccurrence(kat.Start, before, inclusive);

                string context = Describe(kat, before, inclusive);
                Assert.AreEqual(expected, actual, context);
                Assert.AreEqual(expected?.Kind, actual?.Kind, context);
            }
        }
    }

    /// <summary>
    /// Builds the instants the reference-model tests query a rule at: around its start, on, either side of, and
    /// between a spread of the model's occurrences, and at seeded random instants, none past the instant up to which
    /// the model is complete.
    /// </summary>
    /// <param name="kat">The rule under test.</param>
    /// <param name="model">The model's occurrences.</param>
    /// <param name="complete">The latest instant up to which <paramref name="model" /> is the whole stream.</param>
    /// <returns>The query instants, each in the kind of the series start.</returns>
    private static List<DateTime> ReferenceModelProbes(RecurrenceAnchorKat kat, List<DateTime> model, DateTime complete)
    {
        var probes = new List<DateTime>();
        DateTimeKind kind = kat.Start.Kind;
        long last = Math.Min(complete.Ticks, kat.Horizon.Ticks);

        void Add(long ticks)
        {
            if (ticks >= DateTime.MinValue.Ticks && ticks <= complete.Ticks)
            {
                probes.Add(new DateTime(ticks, kind));
            }
        }

        foreach (long delta in new[] { -TimeSpan.TicksPerDay, -1, 0, 1 })
        {
            Add(kat.Start.Ticks + delta);
        }

        int step = Math.Max(1, model.Count / 30);
        for (int i = 0; i < model.Count; i += step)
        {
            Add(model[i].Ticks - 1);
            Add(model[i].Ticks);
            Add(model[i].Ticks + 1);
            if (i + 1 < model.Count)
            {
                Add(model[i].Ticks + ((model[i + 1].Ticks - model[i].Ticks) / 2));
            }
        }

        var random = new Random(StableSeed(kat.Name));
        for (int i = 0; i < 30; i++)
        {
            Add(random.NextInt64(kat.Start.Ticks - TimeSpan.TicksPerDay, last + 1));
        }

        Add(DateTime.MaxValue.Ticks);
        return probes;
    }

    /// <summary>
    /// Adds a rule part to a rule under construction with a given probability.
    /// </summary>
    /// <param name="random">The seeded source of randomness.</param>
    /// <param name="parts">The rule parts so far.</param>
    /// <param name="probability">The probability of adding the part.</param>
    /// <param name="name">The rule part's name.</param>
    /// <param name="value">The rule part's value.</param>
    private static void AddPart(Random random, List<string> parts, double probability, string name, string value)
    {
        if (random.NextDouble() < probability)
        {
            parts.Add($"{name}={value}");
        }
    }

    /// <summary>
    /// Chooses up to a number of distinct values from a range, half the time including a preferred one.
    /// </summary>
    /// <param name="random">The seeded source of randomness.</param>
    /// <param name="range">The values to choose from.</param>
    /// <param name="most">The largest number of values chosen.</param>
    /// <param name="preferred">The value included half the time.</param>
    /// <returns>The chosen values, ascending and comma-separated.</returns>
    private static string RandomValues(Random random, IEnumerable<int> range, int most, int preferred)
    {
        int[] pool = range.ToArray();
        var chosen = new SortedSet<int>();
        int count = random.Next(1, most + 1);
        while (chosen.Count < Math.Min(count, pool.Length))
        {
            chosen.Add(pool[random.Next(pool.Length)]);
        }

        if (random.Next(2) == 0)
        {
            chosen.Add(preferred);
        }

        return string.Join(',', chosen.Select(v => v.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Chooses one to three distinct weekdays, a fifth of them with an ordinal.
    /// </summary>
    /// <param name="random">The seeded source of randomness.</param>
    /// <returns>The <c>BYDAY</c> value.</returns>
    private static string RandomWeekDays(Random random)
    {
        string[] weekdays = ["MO", "TU", "WE", "TH", "FR", "SA", "SU"];
        string[] ordinals = ["1", "2", "-1"];
        var chosen = new SortedSet<int>();
        int count = random.Next(1, 4);
        while (chosen.Count < count)
        {
            chosen.Add(random.Next(weekdays.Length));
        }

        return string.Join(',', chosen.Select(d => (random.Next(5) == 0 ? ordinals[random.Next(ordinals.Length)] : string.Empty) + weekdays[d]));
    }
}
