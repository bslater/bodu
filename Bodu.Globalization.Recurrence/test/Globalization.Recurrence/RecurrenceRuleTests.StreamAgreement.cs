// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.StreamAgreement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>The UTC offsets the <see cref="DateTimeOffset" /> agreement tests anchor their series at.</summary>
    private static readonly TimeSpan[] s_startOffsets = [new(10, 0, 0), new(-5, -30, 0), TimeSpan.Zero];

    /// <summary>The UTC offsets the <see cref="DateTimeOffset" /> agreement tests express their queries in.</summary>
    private static readonly TimeSpan[] s_queryOffsets = [TimeSpan.Zero, new(-11, 0, 0), new(14, 0, 0)];

    /// <summary>
    /// Gets rules anchored years or decades before the instants the agreement tests query them at. The point queries
    /// and the windowed enumeration start at the frequency period near the instant they are given, and these tests hold
    /// them to the occurrence stream enumerated from the series start, which remains the definition of the rule.
    /// </summary>
    /// <value>
    /// One row per rule, covering every frequency with intervals, limits, <c>BYSETPOS</c>, <c>BYWEEKNO</c> weeks that
    /// straddle the turn of a year, sparse and never-matching rules, <c>UNTIL</c> and <c>COUNT</c> bounds, a start that
    /// is not itself an occurrence, every <see cref="DateTimeKind" />, and the end of the calendar.
    /// </value>
    public static IEnumerable<object[]> AnchoredRules
    {
        get
        {
            var horizon = new DateTime(2040, 12, 31, 23, 59, 59);
            var calendarEnd = new DateTime(9999, 12, 31, 23, 59, 59);
            DateTime At(int year, int month, int day, int hour = 9, int minute = 0) => new(year, month, day, hour, minute, 0);

            RecurrenceAnchorKat[] rows =
            [
                new("daily since 1990", "FREQ=DAILY", At(1990, 3, 15, 9, 30), horizon),
                new("every third day since 1985", "FREQ=DAILY;INTERVAL=3", At(1985, 7, 1, 6), horizon),
                new("weekdays", "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR", At(2001, 9, 3), horizon),
                new("daily leap days", "FREQ=DAILY;BYMONTH=2;BYMONTHDAY=29", At(1996, 1, 1), horizon),
                new("daily at two times", "FREQ=DAILY;BYHOUR=8,17;BYMINUTE=0;BYSECOND=0", At(2010, 1, 1), horizon),
                new("daily until 2010", "FREQ=DAILY;UNTIL=20100630T000000", At(2000, 1, 1), horizon),
                new("daily in UTC", "FREQ=DAILY;INTERVAL=2", new DateTime(1999, 12, 31, 23, 0, 0, DateTimeKind.Utc), horizon),
                new("daily count 500", "FREQ=DAILY;COUNT=500", At(2020, 1, 1), horizon),
                new("weekly since 1993", "FREQ=WEEKLY", At(1993, 2, 10, 14), horizon),
                new("fortnightly MO,FR from Sunday weeks", "FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,FR;WKST=SU", At(1994, 5, 6), horizon),
                new("every third week on Sunday", "FREQ=WEEKLY;INTERVAL=3;BYDAY=SU", At(1988, 8, 14, 18), horizon),
                new("summer Saturdays", "FREQ=WEEKLY;BYMONTH=6,7,8;BYDAY=SA", At(1979, 6, 2), horizon),
                new("last weekday of the week", "FREQ=WEEKLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1", At(2003, 3, 7), horizon),
                new("monthly on the 31st", "FREQ=MONTHLY", At(1975, 1, 31, 8), horizon),
                new("monthly last day", "FREQ=MONTHLY;BYMONTHDAY=-1", At(1975, 1, 31, 8), horizon),
                new("quarterly second Tuesday", "FREQ=MONTHLY;INTERVAL=3;BYDAY=2TU", At(1982, 2, 9), horizon),
                new("monthly last weekday", "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1", At(1991, 5, 31), horizon),
                new("Friday the thirteenth", "FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13", At(1970, 2, 13), horizon),
                new("monthly 15th from a start that is not an occurrence", "FREQ=MONTHLY;BYMONTHDAY=15", At(2003, 4, 20), horizon),
                new("monthly last Friday, count 40", "FREQ=MONTHLY;BYDAY=-1FR;COUNT=40", At(2015, 1, 30), horizon),
                new("yearly since 1950", "FREQ=YEARLY", At(1950, 6, 15), horizon),
                new("yearly leap day", "FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=29", At(1960, 2, 29, 0), horizon),
                new("US election day", "FREQ=YEARLY;INTERVAL=4;BYMONTH=11;BYDAY=TU;BYMONTHDAY=2,3,4,5,6,7,8", At(1996, 11, 5), horizon),
                new("Mondays of ISO week one", "FREQ=YEARLY;BYWEEKNO=1;BYDAY=MO", At(1990, 1, 1, 10), horizon),
                new("every day of week 53", "FREQ=YEARLY;BYWEEKNO=53", At(1970, 1, 1), horizon),
                new("last-week Sundays from Sunday weeks", "FREQ=YEARLY;BYWEEKNO=-1;BYDAY=SU;WKST=SU", At(1980, 1, 1), horizon),
                new("Thursdays of week two every seven years", "FREQ=YEARLY;INTERVAL=7;BYWEEKNO=2;BYDAY=TH", At(1950, 1, 1), horizon),
                new("year days 100 and -1", "FREQ=YEARLY;BYYEARDAY=100,-1", At(1966, 4, 10), horizon),
                new("twentieth Monday of the year", "FREQ=YEARLY;BYDAY=20MO", At(1977, 5, 16), horizon),
                new("Thanksgiving", "FREQ=YEARLY;BYMONTH=11;BYDAY=4TH", At(1950, 11, 23), horizon),
                new("last December weekday", "FREQ=YEARLY;BYMONTH=12;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1", At(1960, 12, 30), horizon),
                new("yearly until 2005", "FREQ=YEARLY;UNTIL=20050101T000000", At(1955, 3, 1), horizon),
                new("never: 30 February", "FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30", At(1980, 1, 1), horizon),
                new("daily at the end of the calendar", "FREQ=DAILY", At(9999, 12, 20, 23, 30), calendarEnd),
                new("weekends at the end of the calendar", "FREQ=WEEKLY;BYDAY=SA,SU", At(9999, 11, 6), calendarEnd),
                new("monthly at the end of the calendar", "FREQ=MONTHLY;BYMONTHDAY=-1", At(9990, 1, 31), calendarEnd),
                new("week one at the end of the calendar", "FREQ=YEARLY;BYWEEKNO=1", At(9990, 1, 1), calendarEnd),
            ];

            foreach (RecurrenceAnchorKat row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Gets the anchored rules whose occurrences up to the horizon have an instant in every UTC offset.
    /// </summary>
    /// <value>The rows of <see cref="AnchoredRules" /> whose horizon is before the last year of the calendar.</value>
    public static IEnumerable<object[]> OffsetAnchoredRules =>
        AnchoredRules.Where(row => ((RecurrenceAnchorKat)row[0]).Horizon.Year < DateTime.MaxValue.Year);

    /// <summary>
    /// Verifies that the next occurrence, at instants anywhere in a series, is the one the occurrence stream enumerated
    /// from the series start gives, kind included, with and without the inclusive flag.
    /// </summary>
    /// <param name="kat">The anchored rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AnchoredRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenQueriedAnywhereInTheSeries_ShouldMatchTheOccurrenceStream(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> stream = StreamUpTo(rule, kat.Start, kat.Horizon);
        DateTime? beyond = rule.GetOccurrences(kat.Start).Where(o => o > kat.Horizon).Select(o => (DateTime?)o).FirstOrDefault();

        foreach (DateTime after in Probes(kat, stream))
        {
            foreach (bool inclusive in new[] { false, true })
            {
                int index = inclusive ? LowerBound(stream, after) : UpperBound(stream, after);
                DateTime? expected = index < stream.Count ? stream[index] : beyond;

                DateTime? actual = rule.GetNextOccurrence(kat.Start, after, inclusive);

                string context = Describe(kat, after, inclusive);
                Assert.AreEqual(expected, actual, context);
                Assert.AreEqual(expected?.Kind, actual?.Kind, context);
            }
        }
    }

    /// <summary>
    /// Verifies that the previous occurrence, at instants anywhere in a series, is the one the occurrence stream
    /// enumerated from the series start gives, kind included, with and without the inclusive flag.
    /// </summary>
    /// <param name="kat">The anchored rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AnchoredRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenQueriedAnywhereInTheSeries_ShouldMatchTheOccurrenceStream(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> stream = StreamUpTo(rule, kat.Start, kat.Horizon);

        foreach (DateTime before in Probes(kat, stream))
        {
            foreach (bool inclusive in new[] { false, true })
            {
                int index = (inclusive ? UpperBound(stream, before) : LowerBound(stream, before)) - 1;
                DateTime? expected = index >= 0 ? stream[index] : null;

                DateTime? actual = rule.GetPreviousOccurrence(kat.Start, before, inclusive);

                string context = Describe(kat, before, inclusive);
                Assert.AreEqual(expected, actual, context);
                Assert.AreEqual(expected?.Kind, actual?.Kind, context);
            }
        }
    }

    /// <summary>
    /// Verifies that a window placed anywhere in a series holds exactly the occurrences of the occurrence stream
    /// enumerated from the series start that fall within it.
    /// </summary>
    /// <param name="kat">The anchored rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AnchoredRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenWindowIsAnywhereInTheSeries_ShouldMatchTheOccurrenceStream(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> stream = StreamUpTo(rule, kat.Start, kat.Horizon);
        List<DateTime> probes = Probes(kat, stream);
        var random = new Random(StableSeed(kat.Name));

        for (int i = 0; i < 200; i++)
        {
            DateTime from = probes[random.Next(probes.Count)];
            DateTime to = probes[random.Next(probes.Count)];

            DateTime[] expected = stream.Where(o => o >= from && o <= to).ToArray();
            DateTime[] actual = rule.GetOccurrences(kat.Start, from, to).ToArray();

            CollectionAssert.AreEqual(expected, actual, string.Create(CultureInfo.InvariantCulture, $"{kat.Rule} from {kat.Start:O} in [{from:O}, {to:O}]"));
        }
    }

    /// <summary>
    /// Verifies that the offset-preserving next occurrence, queried anywhere in a series in another offset, is the one
    /// the occurrence stream enumerated from the series start gives.
    /// </summary>
    /// <param name="kat">The anchored rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(OffsetAnchoredRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenQueriedAnywhereInTheSeries_ForDateTimeOffset_ShouldMatchTheOccurrenceStream(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> wallClockStream = StreamUpTo(rule, kat.Start, kat.Horizon);
        List<DateTime> probes = Probes(kat, wallClockStream);
        int row = StableSeed(kat.Name);

        foreach (TimeSpan startOffset in s_startOffsets)
        {
            var start = new DateTimeOffset(DateTime.SpecifyKind(kat.Start, DateTimeKind.Unspecified), startOffset);
            DateTimeOffset[] stream = rule.GetOccurrences(start).TakeWhile(o => o.DateTime <= kat.Horizon).ToArray();
            DateTimeOffset? beyond = rule.GetOccurrences(start).Where(o => o.DateTime > kat.Horizon).Select(o => (DateTimeOffset?)o).FirstOrDefault();

            for (int i = 0; i < probes.Count; i++)
            {
                DateTimeOffset after = OffsetProbe(probes[i], startOffset, s_queryOffsets[(row + i) % s_queryOffsets.Length]);
                foreach (bool inclusive in new[] { false, true })
                {
                    DateTimeOffset? expected = stream.Where(o => o > after || (inclusive && o == after)).Select(o => (DateTimeOffset?)o).FirstOrDefault() ?? beyond;

                    DateTimeOffset? actual = rule.GetNextOccurrence(start, after, inclusive);

                    string context = string.Create(CultureInfo.InvariantCulture, $"{kat.Rule} from {start:O} after {after:O}, inclusive {inclusive}");
                    Assert.AreEqual(expected, actual, context);
                    Assert.AreEqual(expected?.Offset, actual?.Offset, context);
                }
            }
        }
    }

    /// <summary>
    /// Verifies that the offset-preserving previous occurrence, queried anywhere in a series in another offset, is the
    /// one the occurrence stream enumerated from the series start gives.
    /// </summary>
    /// <param name="kat">The anchored rule under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(OffsetAnchoredRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenQueriedAnywhereInTheSeries_ForDateTimeOffset_ShouldMatchTheOccurrenceStream(RecurrenceAnchorKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        List<DateTime> wallClockStream = StreamUpTo(rule, kat.Start, kat.Horizon);
        List<DateTime> probes = Probes(kat, wallClockStream);
        int row = StableSeed(kat.Name);

        foreach (TimeSpan startOffset in s_startOffsets)
        {
            var start = new DateTimeOffset(DateTime.SpecifyKind(kat.Start, DateTimeKind.Unspecified), startOffset);
            DateTimeOffset[] stream = rule.GetOccurrences(start).TakeWhile(o => o.DateTime <= kat.Horizon).ToArray();

            for (int i = 0; i < probes.Count; i++)
            {
                DateTimeOffset before = OffsetProbe(probes[i], startOffset, s_queryOffsets[(row + i) % s_queryOffsets.Length]);
                foreach (bool inclusive in new[] { false, true })
                {
                    DateTimeOffset? expected = stream.Where(o => o < before || (inclusive && o == before)).Select(o => (DateTimeOffset?)o).LastOrDefault();

                    DateTimeOffset? actual = rule.GetPreviousOccurrence(start, before, inclusive);

                    string context = string.Create(CultureInfo.InvariantCulture, $"{kat.Rule} from {start:O} before {before:O}, inclusive {inclusive}");
                    Assert.AreEqual(expected, actual, context);
                    Assert.AreEqual(expected?.Offset, actual?.Offset, context);
                }
            }
        }
    }

    /// <summary>
    /// Materializes the occurrence stream of a rule from its series start up to an instant.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="start">The series start.</param>
    /// <param name="horizon">The latest occurrence kept.</param>
    /// <returns>The ascending occurrences no later than <paramref name="horizon" />.</returns>
    private static List<DateTime> StreamUpTo(RecurrenceRule rule, DateTime start, DateTime horizon) =>
        rule.GetOccurrences(start).TakeWhile(o => o <= horizon).ToList();

    /// <summary>
    /// Builds the instants an agreement test queries a series at: around its start, on and either side of a spread of
    /// its occurrences, across the turn of several years, and at seeded random instants up to its horizon.
    /// </summary>
    /// <param name="kat">The anchored rule.</param>
    /// <param name="stream">The occurrence stream up to the horizon.</param>
    /// <returns>The query instants, each in the kind of the series start and no later than the horizon.</returns>
    private static List<DateTime> Probes(RecurrenceAnchorKat kat, List<DateTime> stream)
    {
        var probes = new List<DateTime>();
        DateTimeKind kind = kat.Start.Kind;

        void Add(long ticks)
        {
            if (ticks >= DateTime.MinValue.Ticks && ticks <= kat.Horizon.Ticks)
            {
                probes.Add(new DateTime(ticks, kind));
            }
        }

        Add(DateTime.MinValue.Ticks);
        foreach (long delta in new[] { -1, 0, 1, -TimeSpan.TicksPerDay, -400 * TimeSpan.TicksPerDay })
        {
            Add(kat.Start.Ticks + delta);
        }

        // A spread of the occurrences, the last one included, each probed on itself, a tick either side, and the
        // edges of its day.
        int step = Math.Max(1, stream.Count / 40);
        IEnumerable<int> spread = Enumerable.Range(0, stream.Count).Where(i => i % step == 0 || i == stream.Count - 1);
        foreach (int i in spread)
        {
            long ticks = stream[i].Ticks;
            long midnight = stream[i].Date.Ticks;
            foreach (long candidate in new[] { ticks - 1, ticks, ticks + 1, midnight, midnight + TimeSpan.TicksPerDay - 1, midnight - (3 * TimeSpan.TicksPerDay) })
            {
                Add(candidate);
            }
        }

        int yearStep = Math.Max(1, (kat.Horizon.Year - kat.Start.Year) / 8);
        for (int year = kat.Start.Year + 1; year <= kat.Horizon.Year; year += yearStep)
        {
            long newYear = new DateTime(year, 1, 1).Ticks;
            for (int day = -4; day <= 4; day++)
            {
                Add(newYear + (day * TimeSpan.TicksPerDay) + (9 * TimeSpan.TicksPerHour));
            }
        }

        var random = new Random(StableSeed(kat.Name));
        long low = Math.Max(DateTime.MinValue.Ticks, kat.Start.Ticks - (730 * TimeSpan.TicksPerDay));
        for (int i = 0; i < 100; i++)
        {
            Add(random.NextInt64(low, kat.Horizon.Ticks + 1));
        }

        return probes;
    }

    /// <summary>
    /// Expresses a wall-clock probe, read in the series' offset, as the same instant in another offset.
    /// </summary>
    /// <param name="wallClock">The probe's wall-clock time in the series' offset.</param>
    /// <param name="startOffset">The offset of the series start.</param>
    /// <param name="queryOffset">The offset the query is expressed in.</param>
    /// <returns>The probe instant, at <paramref name="queryOffset" /> where it can be expressed there.</returns>
    private static DateTimeOffset OffsetProbe(DateTime wallClock, TimeSpan startOffset, TimeSpan queryOffset)
    {
        long utcTicks = Math.Clamp(wallClock.Ticks - startOffset.Ticks, DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks);
        long queryTicks = utcTicks + queryOffset.Ticks;
        return queryTicks >= DateTime.MinValue.Ticks && queryTicks <= DateTime.MaxValue.Ticks
            ? new DateTimeOffset(new DateTime(queryTicks, DateTimeKind.Unspecified), queryOffset)
            : new DateTimeOffset(new DateTime(utcTicks, DateTimeKind.Unspecified), TimeSpan.Zero);
    }

    /// <summary>
    /// Returns the index of the first stream element at or after an instant.
    /// </summary>
    /// <param name="stream">The ascending stream.</param>
    /// <param name="value">The instant.</param>
    /// <returns>The index, or the stream's length when every element precedes <paramref name="value" />.</returns>
    private static int LowerBound(List<DateTime> stream, DateTime value)
    {
        int index = stream.BinarySearch(value);
        if (index < 0)
        {
            return ~index;
        }

        while (index > 0 && stream[index - 1] == value)
        {
            index--;
        }

        return index;
    }

    /// <summary>
    /// Returns the index of the first stream element after an instant.
    /// </summary>
    /// <param name="stream">The ascending stream.</param>
    /// <param name="value">The instant.</param>
    /// <returns>The index, or the stream's length when no element follows <paramref name="value" />.</returns>
    private static int UpperBound(List<DateTime> stream, DateTime value)
    {
        int index = LowerBound(stream, value);
        while (index < stream.Count && stream[index] == value)
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Derives a seed from a row name that is the same in every process, unlike <see cref="string.GetHashCode()" />.
    /// </summary>
    /// <param name="name">The row name.</param>
    /// <returns>The seed, which is non-negative.</returns>
    private static int StableSeed(string name)
    {
        int seed = 17;
        foreach (char c in name)
        {
            seed = unchecked((seed * 31) + c);
        }

        return seed & 0xFFFF;
    }

    /// <summary>
    /// Describes a query for an assertion message.
    /// </summary>
    /// <param name="kat">The anchored rule.</param>
    /// <param name="instant">The query instant.</param>
    /// <param name="inclusive">The inclusive flag.</param>
    /// <returns>The description.</returns>
    private static string Describe(RecurrenceAnchorKat kat, DateTime instant, bool inclusive) =>
        string.Create(CultureInfo.InvariantCulture, $"{kat.Rule} from {kat.Start:O} at {instant:O}, inclusive {inclusive}");
}
