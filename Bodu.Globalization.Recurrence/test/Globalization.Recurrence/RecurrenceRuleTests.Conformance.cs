// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.Conformance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Gets the cross-library edge-case corpus distilled from logged issues in python-dateutil, rrule.js, and ical4j
    /// plus the RFC 5545 §3.8.5.3 examples.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> RecurrenceConformanceCorpus
    {
        get
        {
            var nine = new TimeSpan(9, 0, 0);
            DateTime At(int y, int mo, int d) => new DateTime(y, mo, d).Add(nine);

            var rows = new List<RRuleExpansionKat>
            {
                // WKST flips FREQ=WEEKLY;INTERVAL=2 results (RFC 5545 canonical; dateutil #1398).
                new("wkst=MO biweekly TU,SU", "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO", At(1997, 8, 5), 4,
                    [At(1997, 8, 5), At(1997, 8, 10), At(1997, 8, 19), At(1997, 8, 24)]),
                new("wkst=SU biweekly TU,SU", "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=SU", At(1997, 8, 5), 4,
                    [At(1997, 8, 5), At(1997, 8, 17), At(1997, 8, 19), At(1997, 8, 31)]),

                // BYMONTHDAY skips short months rather than clamping (rrule.js #114).
                new("monthly on the 31st skips short months", "FREQ=MONTHLY;BYMONTHDAY=31;COUNT=7", At(2015, 1, 31), 7,
                    [At(2015, 1, 31), At(2015, 3, 31), At(2015, 5, 31), At(2015, 7, 31), At(2015, 8, 31), At(2015, 10, 31), At(2015, 12, 31)]),
                new("monthly last day resolves per month", "FREQ=MONTHLY;BYMONTHDAY=-1;COUNT=5", At(1997, 9, 30), 5,
                    [At(1997, 9, 30), At(1997, 10, 31), At(1997, 11, 30), At(1997, 12, 31), At(1998, 1, 31)]),
                new("monthly first and last day", "FREQ=MONTHLY;BYMONTHDAY=1,-1;COUNT=10", At(1998, 9, 30), 10,
                    [At(1998, 9, 30), At(1998, 10, 1), At(1998, 10, 31), At(1998, 11, 1), At(1998, 11, 30), At(1998, 12, 1), At(1998, 12, 31), At(1999, 1, 1), At(1999, 1, 31), At(1999, 2, 1)]),

                // Leap-day and year-day handling (dateutil #823, ical4j #243).
                new("yearly on Feb 29 only in leap years", "FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=29;COUNT=3", At(2016, 2, 29), 3,
                    [At(2016, 2, 29), At(2020, 2, 29), At(2024, 2, 29)]),
                new("yearly on the last day of the year", "FREQ=YEARLY;BYYEARDAY=-1;COUNT=3", At(1997, 12, 31), 3,
                    [At(1997, 12, 31), At(1998, 12, 31), At(1999, 12, 31)]),
                new("byyearday 366 never spills into January", "FREQ=YEARLY;BYYEARDAY=2,365,366", At(2016, 1, 1), 9,
                    [At(2016, 1, 2), At(2016, 12, 30), At(2016, 12, 31), At(2017, 1, 2), At(2017, 12, 31), At(2018, 1, 2), At(2018, 12, 31), At(2019, 1, 2), At(2019, 12, 31)]),

                // BYSETPOS is per-period, over the whole candidate list, negative-indexed (RFC; ical4j #39; rrule.js #375).
                new("third Tu/We/Th of the month", "FREQ=MONTHLY;COUNT=3;BYDAY=TU,WE,TH;BYSETPOS=3", At(1997, 9, 4), 3,
                    [At(1997, 9, 4), At(1997, 10, 7), At(1997, 11, 6)]),
                new("second-to-last weekday of the month", "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-2;COUNT=4", At(1997, 9, 29), 4,
                    [At(1997, 9, 29), At(1997, 10, 30), At(1997, 11, 27), At(1997, 12, 30)]),
                new("last valid day across named months", "FREQ=MONTHLY;BYMONTH=2,3,9,10;BYMONTHDAY=28,29,30,31;BYSETPOS=-1", At(2015, 7, 1), 6,
                    [At(2015, 9, 30), At(2015, 10, 31), At(2016, 2, 29), At(2016, 3, 31), At(2016, 9, 30), At(2016, 10, 31)]),

                // Ordinal BYDAY vs BYSETPOS equivalence for the single-weekday case.
                new("last Friday via ordinal BYDAY", "FREQ=MONTHLY;BYDAY=-1FR;COUNT=3", At(1997, 9, 26), 3,
                    [At(1997, 9, 26), At(1997, 10, 31), At(1997, 11, 28)]),
                new("last Friday via BYSETPOS", "FREQ=MONTHLY;BYDAY=FR;BYSETPOS=-1;COUNT=3", At(1997, 9, 26), 3,
                    [At(1997, 9, 26), At(1997, 10, 31), At(1997, 11, 28)]),
                new("twentieth Monday of the year", "FREQ=YEARLY;BYDAY=20MO", At(1997, 5, 19), 3,
                    [At(1997, 5, 19), At(1998, 5, 18), At(1999, 5, 17)]),

                // BYDAY intersects BYMONTHDAY (AND), never unions (RFC "Friday the 13th").
                new("Friday the 13th", "FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13", At(1997, 9, 2), 5,
                    [At(1998, 2, 13), At(1998, 3, 13), At(1998, 11, 13), At(1999, 8, 13), At(2000, 10, 13)]),
                new("first Saturday after the first Sunday", "FREQ=MONTHLY;BYDAY=SA;BYMONTHDAY=7,8,9,10,11,12,13", At(1997, 9, 13), 5,
                    [At(1997, 9, 13), At(1997, 10, 11), At(1997, 11, 8), At(1997, 12, 13), At(1998, 1, 10)]),

                // BYWEEKNO with BYDAY (ISO week).
                new("Monday of ISO week 20", "FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO", At(1997, 5, 12), 3,
                    [At(1997, 5, 12), At(1998, 5, 11), At(1999, 5, 17)]),

                // Weekly expansion ignores month boundaries.
                new("weekly Tuesday and Thursday across a month boundary", "FREQ=WEEKLY;BYDAY=TU,TH;COUNT=6", At(1997, 9, 30), 6,
                    [At(1997, 9, 30), At(1997, 10, 2), At(1997, 10, 7), At(1997, 10, 9), At(1997, 10, 14), At(1997, 10, 16)]),
                new("every Thursday in March", "FREQ=YEARLY;BYMONTH=3;BYDAY=TH", At(1997, 3, 13), 8,
                    [At(1997, 3, 13), At(1997, 3, 20), At(1997, 3, 27), At(1998, 3, 5), At(1998, 3, 12), At(1998, 3, 19), At(1998, 3, 26), At(1999, 3, 4)]),
            };

            foreach (RRuleExpansionKat row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Verifies that each cross-library edge-case rule expands to the reference occurrences confirmed against
    /// python-dateutil, rrule.js, ical4j, and RFC 5545.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RecurrenceConformanceCorpus),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenCrossLibraryEdgeCase_ShouldMatchReference(RRuleExpansionKat kat)
    {
        DateTime[] actual = RecurrenceRule.Parse(kat.Rule).GetOccurrences(kat.Start).Take(kat.Take).ToArray();

        CollectionAssert.AreEqual(kat.Expected, actual);
    }

    /// <summary>
    /// Verifies that an <c>UNTIL</c> bound is inclusive, keeping an occurrence whose instant equals the bound.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenOccurrenceEqualsUntil_ShouldBeIncluded()
    {
        DateTime[] actual = RecurrenceRule.Parse("FREQ=DAILY;UNTIL=19971224T000000")
            .GetOccurrences(new DateTime(1997, 12, 22)).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateTime(1997, 12, 22), new DateTime(1997, 12, 23), new DateTime(1997, 12, 24) },
            actual);
    }

    /// <summary>
    /// Verifies that a start instant that does not satisfy the rule is not emitted (rruleset semantics).
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenStartDoesNotSatisfyRule_ShouldNotEmitStart()
    {
        DateTime[] actual = RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=TH;COUNT=2")
            .GetOccurrences(new DateTime(1997, 9, 2, 9, 0, 0)).ToArray();

        CollectionAssert.AreEqual(
            new[] { new DateTime(1997, 9, 4, 9, 0, 0), new DateTime(1997, 9, 11, 9, 0, 0) },
            actual);
        CollectionAssert.DoesNotContain(actual, new DateTime(1997, 9, 2, 9, 0, 0));
    }

    /// <summary>
    /// Verifies that <c>COUNT</c> is anchored at the start, so a window beginning after the counted occurrences yields
    /// nothing rather than re-seeding the count at the window.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenCountAnchoredAtStartAndWindowAfter_ShouldBeEmpty()
    {
        DateTime[] actual = RecurrenceRule.Parse("FREQ=DAILY;COUNT=1")
            .GetOccurrences(new DateTime(2019, 7, 15), new DateTime(2019, 7, 22), new DateTime(2019, 7, 28))
            .ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that a rule which can never match terminates with an empty sequence rather than looping forever.
    /// </summary>
    [TestMethod]
    public void GetOccurrences_WhenRuleNeverMatches_ShouldTerminateEmpty()
    {
        DateTime[] actual = RecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30")
            .GetOccurrences(new DateTime(2020, 1, 1, 9, 0, 0)).Take(1).ToArray();

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Gets the second-wave corpus covering time-of-day expansion, ordinal skipping, interval-with-ordinal, month
    /// filters, and multi-position set selection, cross-checked against python-dateutil.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> RecurrenceTimeAndSelectionCorpus
    {
        get
        {
            DateTime T(int y, int mo, int d, int h, int mi) => new DateTime(y, mo, d, h, mi, 0);

            var rows = new List<RRuleExpansionKat>
            {
                new("BYHOUR expands to multiple times per day", "FREQ=DAILY;BYHOUR=9,17;COUNT=4", T(2020, 1, 1, 0, 0), 4,
                    [T(2020, 1, 1, 9, 0), T(2020, 1, 1, 17, 0), T(2020, 1, 2, 9, 0), T(2020, 1, 2, 17, 0)]),
                new("BYHOUR and BYMINUTE expand together", "FREQ=DAILY;BYHOUR=9;BYMINUTE=0,30;COUNT=4", T(2020, 1, 1, 0, 0), 4,
                    [T(2020, 1, 1, 9, 0), T(2020, 1, 1, 9, 30), T(2020, 1, 2, 9, 0), T(2020, 1, 2, 9, 30)]),
                new("fifth Friday skips months with only four", "FREQ=MONTHLY;BYDAY=5FR;COUNT=3", T(2020, 1, 1, 9, 0), 3,
                    [T(2020, 1, 31, 9, 0), T(2020, 5, 29, 9, 0), T(2020, 7, 31, 9, 0)]),
                new("every other month, first Monday", "FREQ=MONTHLY;INTERVAL=2;BYDAY=1MO;COUNT=3", T(2020, 1, 6, 9, 0), 3,
                    [T(2020, 1, 6, 9, 0), T(2020, 3, 2, 9, 0), T(2020, 5, 4, 9, 0)]),
                new("BYMONTH filters a weekly rule", "FREQ=WEEKLY;BYMONTH=1;BYDAY=MO;COUNT=6", T(2020, 1, 1, 9, 0), 6,
                    [T(2020, 1, 6, 9, 0), T(2020, 1, 13, 9, 0), T(2020, 1, 20, 9, 0), T(2020, 1, 27, 9, 0), T(2021, 1, 4, 9, 0), T(2021, 1, 11, 9, 0)]),
                new("yearly first Monday of two months", "FREQ=YEARLY;BYMONTH=6,7;BYDAY=1MO;COUNT=4", T(2020, 6, 1, 9, 0), 4,
                    [T(2020, 6, 1, 9, 0), T(2020, 7, 6, 9, 0), T(2021, 6, 7, 9, 0), T(2021, 7, 5, 9, 0)]),
                new("first and last weekday via multi BYSETPOS", "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1,-1;COUNT=6", T(2020, 1, 1, 9, 0), 6,
                    [T(2020, 1, 1, 9, 0), T(2020, 1, 31, 9, 0), T(2020, 2, 3, 9, 0), T(2020, 2, 28, 9, 0), T(2020, 3, 2, 9, 0), T(2020, 3, 31, 9, 0)]),
                new("BYSETPOS selects the latest time-of-day", "FREQ=DAILY;BYHOUR=8,12,18;BYSETPOS=-1;COUNT=3", T(2020, 1, 1, 0, 0), 3,
                    [T(2020, 1, 1, 18, 0), T(2020, 1, 2, 18, 0), T(2020, 1, 3, 18, 0)]),
                new("monthly on the 29th resolves February in a leap year", "FREQ=MONTHLY;BYMONTHDAY=29;COUNT=4", T(2020, 1, 1, 9, 0), 4,
                    [T(2020, 1, 29, 9, 0), T(2020, 2, 29, 9, 0), T(2020, 3, 29, 9, 0), T(2020, 4, 29, 9, 0)]),

                new("daily interval filters weekdays, not every third weekday", "FREQ=DAILY;INTERVAL=3;BYDAY=MO,TU,WE,TH,FR;COUNT=6", T(2017, 3, 6, 10, 0), 6,
                    [T(2017, 3, 6, 10, 0), T(2017, 3, 9, 10, 0), T(2017, 3, 15, 10, 0), T(2017, 3, 21, 10, 0), T(2017, 3, 24, 10, 0), T(2017, 3, 27, 10, 0)]),
                new("yearly BYSETPOS spans multiple months (last)", "FREQ=YEARLY;BYMONTH=1,2,3;BYDAY=MO;BYSETPOS=-1;COUNT=2", T(2023, 1, 1, 9, 0), 2,
                    [T(2023, 3, 27, 9, 0), T(2024, 3, 25, 9, 0)]),
                new("yearly BYSETPOS spans multiple months (first)", "FREQ=YEARLY;BYMONTH=1,2,3;BYDAY=MO;BYSETPOS=1;COUNT=2", T(2023, 1, 1, 9, 0), 2,
                    [T(2023, 1, 2, 9, 0), T(2024, 1, 1, 9, 0)]),
                new("UNTIL cuts between intra-day expanded instants", "FREQ=DAILY;BYHOUR=8,12,18;UNTIL=20230102T120000", T(2023, 1, 1, 0, 0), 20,
                    [T(2023, 1, 1, 8, 0), T(2023, 1, 1, 12, 0), T(2023, 1, 1, 18, 0), T(2023, 1, 2, 8, 0), T(2023, 1, 2, 12, 0)]),
                new("first weekday emits a matching start", "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1;COUNT=7", T(2024, 1, 1, 9, 0), 7,
                    [T(2024, 1, 1, 9, 0), T(2024, 2, 1, 9, 0), T(2024, 3, 1, 9, 0), T(2024, 4, 1, 9, 0), T(2024, 5, 1, 9, 0), T(2024, 6, 3, 9, 0), T(2024, 7, 1, 9, 0)]),
            };

            foreach (RRuleExpansionKat row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Verifies the time-expansion and selection edge cases against their python-dateutil reference occurrences.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RecurrenceTimeAndSelectionCorpus),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenTimeOrSelectionEdgeCase_ShouldMatchReference(RRuleExpansionKat kat)
    {
        DateTime[] actual = RecurrenceRule.Parse(kat.Rule).GetOccurrences(kat.Start).Take(kat.Take).ToArray();

        CollectionAssert.AreEqual(kat.Expected, actual);
    }
}
