// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRuleTests.Corpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceRuleTests
{
    /// <summary>
    /// Gets the RFC 5545 §3.8.5.3 worked-example corpus of rules paired with their expected leading occurrences.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> Rfc5545Corpus
    {
        get
        {
            var nineAm = new TimeSpan(9, 0, 0);
            DateTime At(int year, int month, int day) => new DateTime(year, month, day).Add(nineAm);

            var rows = new List<RRuleExpansionKat>
            {
                new("daily for ten occurrences", "FREQ=DAILY;COUNT=10", At(1997, 9, 2), 10,
                    [At(1997, 9, 2), At(1997, 9, 3), At(1997, 9, 4), At(1997, 9, 5), At(1997, 9, 6), At(1997, 9, 7), At(1997, 9, 8), At(1997, 9, 9), At(1997, 9, 10), At(1997, 9, 11)]),
                new("every other day", "FREQ=DAILY;INTERVAL=2;COUNT=5", At(1997, 9, 2), 5,
                    [At(1997, 9, 2), At(1997, 9, 4), At(1997, 9, 6), At(1997, 9, 8), At(1997, 9, 10)]),
                new("every ten days five times", "FREQ=DAILY;INTERVAL=10;COUNT=5", At(1997, 9, 2), 5,
                    [At(1997, 9, 2), At(1997, 9, 12), At(1997, 9, 22), At(1997, 10, 2), At(1997, 10, 12)]),
                new("weekly for six occurrences", "FREQ=WEEKLY;COUNT=6", At(1997, 9, 2), 6,
                    [At(1997, 9, 2), At(1997, 9, 9), At(1997, 9, 16), At(1997, 9, 23), At(1997, 9, 30), At(1997, 10, 7)]),
                new("every other week", "FREQ=WEEKLY;INTERVAL=2;COUNT=4;WKST=SU", At(1997, 9, 2), 4,
                    [At(1997, 9, 2), At(1997, 9, 16), At(1997, 9, 30), At(1997, 10, 14)]),
                new("weekly on Tuesday and Thursday", "FREQ=WEEKLY;COUNT=4;BYDAY=TU,TH;WKST=SU", At(1997, 9, 2), 4,
                    [At(1997, 9, 2), At(1997, 9, 4), At(1997, 9, 9), At(1997, 9, 11)]),
                new("monthly on the first Friday", "FREQ=MONTHLY;COUNT=3;BYDAY=1FR", At(1997, 9, 5), 3,
                    [At(1997, 9, 5), At(1997, 10, 3), At(1997, 11, 7)]),
                new("monthly on the last day", "FREQ=MONTHLY;COUNT=3;BYMONTHDAY=-1", At(1997, 9, 30), 3,
                    [At(1997, 9, 30), At(1997, 10, 31), At(1997, 11, 30)]),
                new("monthly on the 2nd and 15th", "FREQ=MONTHLY;COUNT=4;BYMONTHDAY=2,15", At(1997, 9, 2), 4,
                    [At(1997, 9, 2), At(1997, 9, 15), At(1997, 10, 2), At(1997, 10, 15)]),
                new("yearly on the first day of the year", "FREQ=YEARLY;COUNT=3;BYYEARDAY=1", At(1997, 1, 1), 3,
                    [At(1997, 1, 1), At(1998, 1, 1), At(1999, 1, 1)]),
                new("yearly Thanksgiving", "FREQ=YEARLY;BYMONTH=11;BYDAY=4TH;COUNT=3", At(1997, 11, 27), 3,
                    [At(1997, 11, 27), At(1998, 11, 26), At(1999, 11, 25)]),
                new("every four years US election day", "FREQ=YEARLY;INTERVAL=4;BYMONTH=11;BYDAY=TU;BYMONTHDAY=2,3,4,5,6,7,8;COUNT=3", At(1996, 11, 5), 3,
                    [At(1996, 11, 5), At(2000, 11, 7), At(2004, 11, 2)]),
            };

            foreach (RRuleExpansionKat row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Verifies that each RFC 5545 corpus rule expands to its documented leading occurrences.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Rfc5545Corpus),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenRfc5545CorpusRule_ShouldMatchExpectedOccurrences(RRuleExpansionKat kat)
    {
        DateTime[] actual = RecurrenceRule.Parse(kat.Rule).GetOccurrences(kat.Start).Take(kat.Take).ToArray();

        CollectionAssert.AreEqual(kat.Expected, actual);
    }
}
