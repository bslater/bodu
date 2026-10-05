// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.StreamAgreement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Gets recurrence sets that start years or decades before the instants the agreement tests query them at. The
    /// point queries and the windowed enumeration start each rule at the frequency period near the instant they are
    /// given, and these tests hold them to the union of each rule's occurrence stream, enumerated from the set's start,
    /// and the explicit dates, less the exception dates, which remains the definition of the set.
    /// </summary>
    /// <value>
    /// One row per set, covering overlapping rules, <c>COUNT</c> and <c>UNTIL</c> rules beside unbounded ones, explicit
    /// dates before the start, after the rules end, duplicated, and equal to rule occurrences, exception dates that
    /// remove the start, a run of occurrences, a whole year of them, or nothing at all, a set of explicit dates alone,
    /// a UTC set, and the end of the calendar.
    /// </value>
    public static IEnumerable<object[]> AnchoredSets
    {
        get
        {
            var horizon = new DateTime(2040, 12, 31, 23, 59, 59);
            var calendarEnd = new DateTime(9999, 12, 31, 23, 59, 59);

            RecurrenceSetAnchorKat[] rows =
            [
                new(
                    "daily since 1990, a holiday run excluded",
                    "DTSTART:19900315T093000\nRRULE:FREQ=DAILY\n" +
                    $"EXDATE:{Instants(new DateTime(2025, 12, 20, 9, 30, 0), 17, TimeSpan.FromDays(1))},20260110T120000",
                    horizon),
                new(
                    "overlapping weekly rules since 1993",
                    "DTSTART:19930210T140000\nRRULE:FREQ=WEEKLY;BYDAY=MO,WE,FR\nRRULE:FREQ=WEEKLY;INTERVAL=2;BYDAY=WE,SA",
                    horizon),
                new(
                    "monthly last weekday, dates before the start and duplicated",
                    "DTSTART:19910531T090000\nRRULE:FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1\n" +
                    "RDATE:19700101T000000,19850704T120000,19850704T120000,20130830T090000,20260315T100000,20260315T100000\n" +
                    "EXDATE:20151130T090000,19850704T120000",
                    horizon),
                new(
                    "count rule beside an unbounded yearly rule",
                    "DTSTART:20000101T080000\nRRULE:FREQ=DAILY;COUNT=500\nRRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=4TH\n" +
                    "EXDATE:20000105T080000,20001123T080000",
                    horizon),
                new(
                    "exhausted count rule and later dates",
                    "DTSTART:19700101T000000\nRRULE:FREQ=MONTHLY;COUNT=12\nRDATE:20050505T050505,20300101T000000",
                    horizon),
                new(
                    "rule until 2010 and dates after it",
                    "DTSTART:20000101T090000\nRRULE:FREQ=WEEKLY;BYDAY=TU;UNTIL=20100630T000000\n" +
                    "RDATE:20120101T090000,20200229T090000,20350101T000000\nEXDATE:20100629T090000",
                    horizon),
                new(
                    "explicit dates alone",
                    "DTSTART:20050101T000000\n" +
                    "RDATE:20050101T000000,20070304T050607,20070304T050607,20191231T235959,20200101T000000,20310704T120000\n" +
                    "EXDATE:20191231T235959",
                    horizon),
                new(
                    "every day of 2024 excluded",
                    "DTSTART:20100101T070000\nRRULE:FREQ=DAILY\n" +
                    $"EXDATE:{Instants(new DateTime(2024, 1, 1, 7, 0, 0), 366, TimeSpan.FromDays(1))}",
                    horizon),
                new(
                    "every other Wednesday excluded",
                    "DTSTART:20150107T180000\nRRULE:FREQ=WEEKLY;BYDAY=WE\n" +
                    $"EXDATE:{Instants(new DateTime(2015, 1, 7, 18, 0, 0), 600, TimeSpan.FromDays(14))}",
                    horizon),
                new(
                    "Mondays of ISO week one and the first of the month",
                    "DTSTART:19900101T100000\nRRULE:FREQ=YEARLY;BYWEEKNO=1;BYDAY=MO\nRRULE:FREQ=MONTHLY;BYMONTHDAY=1\n" +
                    "EXDATE:20251229T100000",
                    horizon),
                new(
                    "two times a day and Friday the thirteenth",
                    "DTSTART:20100101T080000\nRRULE:FREQ=DAILY;BYHOUR=8,17;BYMINUTE=0;BYSECOND=0\n" +
                    "RRULE:FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13\nEXDATE:20260213T080000,20260213T170000",
                    horizon),
                new(
                    "leap days and a rule that never matches",
                    "DTSTART:19600229T000000\nRRULE:FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=29\n" +
                    "RRULE:FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30\nRDATE:20000301T000000\nEXDATE:20000229T000000",
                    horizon),
                new(
                    "start added as a date and excluded",
                    "DTSTART:20030420T090000\nRRULE:FREQ=MONTHLY;BYMONTHDAY=15\nRDATE:20030420T090000\n" +
                    "EXDATE:20030420T090000,20030515T090000",
                    horizon),
                new(
                    "every other day in UTC",
                    "DTSTART:19991231T230000Z\nRRULE:FREQ=DAILY;INTERVAL=2\nRDATE:20200101T000000Z\n" +
                    "EXDATE:20200102T230000Z,20200103T230000Z",
                    horizon),
                new(
                    "daily at the end of the calendar",
                    "DTSTART:99991220T233000\nRRULE:FREQ=DAILY\nRDATE:99991231T235959\nEXDATE:99991225T233000",
                    calendarEnd),
            ];

            foreach (RecurrenceSetAnchorKat row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Verifies that a set enumerated from its start holds exactly the union of its rules' occurrence streams and its
    /// explicit dates, less its exception dates.
    /// </summary>
    /// <param name="kat">The set under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AnchoredSets),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenEnumeratedFromTheStart_ShouldMatchTheUnionOfItsSources(RecurrenceSetAnchorKat kat)
    {
        var set = RecurrenceSet.Parse(kat.Block);
        List<DateTime> union = UnionUpTo(set, kat.Horizon);

        DateTime[] actual = set.GetOccurrences().TakeWhile(o => o <= kat.Horizon).ToArray();

        CollectionAssert.AreEqual(union, actual, kat.Name);
    }

    /// <summary>
    /// Verifies that the next occurrence of a set, at instants anywhere in it, is the one the union of its sources
    /// gives, kind included, with and without the inclusive flag.
    /// </summary>
    /// <param name="kat">The set under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AnchoredSets),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenQueriedAnywhereInTheSet_ShouldMatchTheUnionOfItsSources(RecurrenceSetAnchorKat kat)
    {
        var set = RecurrenceSet.Parse(kat.Block);
        List<DateTime> union = UnionUpTo(set, kat.Horizon);
        DateTime? beyond = FirstBeyond(set, kat.Horizon);

        foreach (DateTime after in SetProbes(set, kat, union))
        {
            foreach (bool inclusive in new[] { false, true })
            {
                int index = inclusive ? FirstAtOrAfter(union, after) : FirstAfter(union, after);
                DateTime? expected = index < union.Count ? union[index] : beyond;

                DateTime? actual = set.GetNextOccurrence(after, inclusive);

                string context = Describe(kat, after, inclusive);
                Assert.AreEqual(expected, actual, context);
                Assert.AreEqual(expected?.Kind, actual?.Kind, context);
            }
        }
    }

    /// <summary>
    /// Verifies that the previous occurrence of a set, at instants anywhere in it, is the one the union of its sources
    /// gives, kind included, with and without the inclusive flag.
    /// </summary>
    /// <param name="kat">The set under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AnchoredSets),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenQueriedAnywhereInTheSet_ShouldMatchTheUnionOfItsSources(RecurrenceSetAnchorKat kat)
    {
        var set = RecurrenceSet.Parse(kat.Block);
        List<DateTime> union = UnionUpTo(set, kat.Horizon);

        foreach (DateTime before in SetProbes(set, kat, union))
        {
            foreach (bool inclusive in new[] { false, true })
            {
                int index = (inclusive ? FirstAfter(union, before) : FirstAtOrAfter(union, before)) - 1;
                DateTime? expected = index >= 0 ? union[index] : null;

                DateTime? actual = set.GetPreviousOccurrence(before, inclusive);

                string context = Describe(kat, before, inclusive);
                Assert.AreEqual(expected, actual, context);
                Assert.AreEqual(expected?.Kind, actual?.Kind, context);
            }
        }
    }

    /// <summary>
    /// Verifies that a window placed anywhere in a set holds exactly the occurrences of the union of its sources that
    /// fall within it.
    /// </summary>
    /// <param name="kat">The set under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(AnchoredSets),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenWindowIsAnywhereInTheSet_ShouldMatchTheUnionOfItsSources(RecurrenceSetAnchorKat kat)
    {
        var set = RecurrenceSet.Parse(kat.Block);
        List<DateTime> union = UnionUpTo(set, kat.Horizon);
        List<DateTime> probes = SetProbes(set, kat, union);
        var random = new Random(StableSeed(kat.Name));

        for (int i = 0; i < 100; i++)
        {
            DateTime from = probes[random.Next(probes.Count)];
            DateTime to = probes[random.Next(probes.Count)];

            DateTime[] expected = union.Where(o => o >= from && o <= to).ToArray();
            DateTime[] actual = set.GetOccurrences(from, to).ToArray();

            CollectionAssert.AreEqual(expected, actual, string.Create(CultureInfo.InvariantCulture, $"{kat.Name} in [{from:O}, {to:O}]"));
        }
    }

    /// <summary>
    /// Materializes the union of a set's sources up to an instant: each rule's occurrence stream enumerated from the
    /// set's start and the explicit dates, less the exception dates.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="horizon">The latest occurrence kept.</param>
    /// <returns>The ascending, distinct occurrences no later than <paramref name="horizon" />.</returns>
    private static List<DateTime> UnionUpTo(RecurrenceSet set, DateTime horizon)
    {
        var union = new SortedSet<DateTime>(set.Dates.Where(d => d <= horizon));
        foreach (RecurrenceRule rule in set.Rules)
        {
            union.UnionWith(rule.GetOccurrences(set.Start).TakeWhile(o => o <= horizon));
        }

        union.ExceptWith(set.ExceptionDates);
        return [.. union];
    }

    /// <summary>
    /// Returns the first occurrence of the union of a set's sources after an instant.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="horizon">The instant.</param>
    /// <returns>The earliest occurrence after <paramref name="horizon" />, or <see langword="null" /> when none is.</returns>
    private static DateTime? FirstBeyond(RecurrenceSet set, DateTime horizon)
    {
        var exceptions = new HashSet<DateTime>(set.ExceptionDates);
        IEnumerable<DateTime> candidates = set.Dates.Where(d => d > horizon && !exceptions.Contains(d));
        foreach (RecurrenceRule rule in set.Rules)
        {
            candidates = candidates.Concat(rule.GetOccurrences(set.Start).Where(o => o > horizon && !exceptions.Contains(o)).Take(1));
        }

        return candidates.Select(o => (DateTime?)o).Min();
    }

    /// <summary>
    /// Builds the instants an agreement test queries a set at: around its start, on and either side of its explicit
    /// and exception dates and of a spread of its occurrences, across the turn of several years, and at seeded random
    /// instants up to its horizon.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="kat">The row the set was parsed from.</param>
    /// <param name="union">The occurrences up to the horizon.</param>
    /// <returns>The query instants, each in the kind of the set's start and no later than the horizon.</returns>
    private static List<DateTime> SetProbes(RecurrenceSet set, RecurrenceSetAnchorKat kat, List<DateTime> union)
    {
        var probes = new List<DateTime>();
        DateTimeKind kind = set.Start.Kind;

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
            Add(set.Start.Ticks + delta);
        }

        // The explicit and exception dates, or a spread of a long list of them, the last one included.
        foreach (IReadOnlyList<DateTime> dates in new[] { set.Dates, set.ExceptionDates })
        {
            int dateStep = Math.Max(1, dates.Count / 20);
            for (int i = 0; i < dates.Count; i++)
            {
                if (i % dateStep == 0 || i == dates.Count - 1)
                {
                    Add(dates[i].Ticks - 1);
                    Add(dates[i].Ticks);
                    Add(dates[i].Ticks + 1);
                }
            }
        }

        // A spread of the occurrences, the last one included, each probed on itself, a tick either side, and the
        // edges of its day.
        int step = Math.Max(1, union.Count / 40);
        IEnumerable<int> spread = Enumerable.Range(0, union.Count).Where(i => i % step == 0 || i == union.Count - 1);
        foreach (int i in spread)
        {
            long ticks = union[i].Ticks;
            long midnight = union[i].Date.Ticks;
            foreach (long candidate in new[] { ticks - 1, ticks, ticks + 1, midnight, midnight + TimeSpan.TicksPerDay - 1, midnight - (3 * TimeSpan.TicksPerDay) })
            {
                Add(candidate);
            }
        }

        int yearStep = Math.Max(1, (kat.Horizon.Year - set.Start.Year) / 8);
        for (int year = set.Start.Year + 1; year <= kat.Horizon.Year; year += yearStep)
        {
            long newYear = new DateTime(year, 1, 1).Ticks;
            for (int day = -4; day <= 4; day++)
            {
                Add(newYear + (day * TimeSpan.TicksPerDay) + (9 * TimeSpan.TicksPerHour));
            }
        }

        var random = new Random(StableSeed(kat.Name));
        long low = Math.Max(DateTime.MinValue.Ticks, set.Start.Ticks - (730 * TimeSpan.TicksPerDay));
        for (int i = 0; i < 100; i++)
        {
            Add(random.NextInt64(low, kat.Horizon.Ticks + 1));
        }

        return probes;
    }

    /// <summary>
    /// Formats a run of evenly spaced instants as an iCalendar date-time list.
    /// </summary>
    /// <param name="first">The first instant.</param>
    /// <param name="count">The number of instants.</param>
    /// <param name="step">The spacing between consecutive instants.</param>
    /// <returns>The comma-separated list.</returns>
    private static string Instants(DateTime first, int count, TimeSpan step) =>
        string.Join(',', Enumerable.Range(0, count).Select(i => (first + (step * i)).ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture)));

    /// <summary>
    /// Returns the index of the first element of an ascending, distinct list at or after an instant.
    /// </summary>
    /// <param name="union">The ascending, distinct list.</param>
    /// <param name="value">The instant.</param>
    /// <returns>The index, or the list's length when every element precedes <paramref name="value" />.</returns>
    private static int FirstAtOrAfter(List<DateTime> union, DateTime value)
    {
        int index = union.BinarySearch(value);
        return index < 0 ? ~index : index;
    }

    /// <summary>
    /// Returns the index of the first element of an ascending, distinct list after an instant.
    /// </summary>
    /// <param name="union">The ascending, distinct list.</param>
    /// <param name="value">The instant.</param>
    /// <returns>The index, or the list's length when no element follows <paramref name="value" />.</returns>
    private static int FirstAfter(List<DateTime> union, DateTime value)
    {
        int index = union.BinarySearch(value);
        return index < 0 ? ~index : index + 1;
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
    /// <param name="kat">The set's row.</param>
    /// <param name="instant">The query instant.</param>
    /// <param name="inclusive">The inclusive flag.</param>
    /// <returns>The description.</returns>
    private static string Describe(RecurrenceSetAnchorKat kat, DateTime instant, bool inclusive) =>
        string.Create(CultureInfo.InvariantCulture, $"{kat.Name} at {instant:O}, inclusive {inclusive}");
}
