// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceCorpusTests.Dateutil.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceCorpusTests
{
    /// <summary>
    /// Gets the sub-daily rules of the python-dateutil comparison corpus.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> DateutilSubDailyRules =>
        LoadDateutil().Select(r => new object[] { r });

    /// <summary>
    /// Verifies that each sub-daily rule produces the occurrences python-dateutil records for it, and nothing after
    /// them where the corpus records the whole stream.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DateutilSubDailyRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenDateutilSubDailyRule_ShouldMatchTheRecordedOccurrences(DateutilSubDailyKat kat)
    {
        // Taking one more than a whole stream turns "produced too many" into a visible difference rather than a
        // silent truncation to the expected length.
        int take = kat.IsTruncated ? kat.Expected.Length : kat.Expected.Length + 1;

        DateTime[] actual = RecurrenceRule.Parse(kat.Rule).GetOccurrences(kat.Start).Take(take).ToArray();

        CollectionAssert.AreEqual(kat.Expected, actual, kat.Rule);
    }

    /// <summary>
    /// Verifies that the next occurrence, queried on, between, and a tick before the occurrences python-dateutil
    /// records for a sub-daily rule, is the recorded one, and that none follows the last of a whole stream.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DateutilSubDailyRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenDateutilSubDailyRule_ShouldStepThroughTheRecordedOccurrences(DateutilSubDailyKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        DateTime[] expected = kat.Expected;
        DateTime? first = expected.Length == 0 ? null : expected[0];

        Assert.AreEqual(first, rule.GetNextOccurrence(kat.Start, kat.Start, inclusive: true), Describe(kat, kat.Start));
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], rule.GetNextOccurrence(kat.Start, expected[i], inclusive: true), Describe(kat, expected[i]));
            Assert.AreEqual(expected[i], rule.GetNextOccurrence(kat.Start, expected[i].AddTicks(-1)), Describe(kat, expected[i].AddTicks(-1)));
            if (i > 0)
            {
                DateTime previous = expected[i - 1];
                DateTime between = previous.AddTicks((expected[i].Ticks - previous.Ticks) / 2);
                Assert.AreEqual(expected[i], rule.GetNextOccurrence(kat.Start, previous), Describe(kat, previous));
                Assert.AreEqual(expected[i], rule.GetNextOccurrence(kat.Start, between), Describe(kat, between));
            }
        }

        if (!kat.IsTruncated && expected.Length > 0)
        {
            Assert.IsNull(rule.GetNextOccurrence(kat.Start, expected[^1]), Describe(kat, expected[^1]));
        }
    }

    /// <summary>
    /// Verifies that the previous occurrence, queried on, between, and a tick after the occurrences python-dateutil
    /// records for a sub-daily rule, is the recorded one, that none precedes the first, and that the last of a whole
    /// stream is the latest there is.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DateutilSubDailyRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenDateutilSubDailyRule_ShouldStepBackThroughTheRecordedOccurrences(DateutilSubDailyKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);
        DateTime[] expected = kat.Expected;

        for (int i = 0; i < expected.Length; i++)
        {
            DateTime? previous = i == 0 ? null : expected[i - 1];

            Assert.AreEqual(expected[i], rule.GetPreviousOccurrence(kat.Start, expected[i], inclusive: true), Describe(kat, expected[i]));
            Assert.AreEqual(expected[i], rule.GetPreviousOccurrence(kat.Start, expected[i].AddTicks(1)), Describe(kat, expected[i].AddTicks(1)));
            Assert.AreEqual(previous, rule.GetPreviousOccurrence(kat.Start, expected[i]), Describe(kat, expected[i]));
            if (previous is DateTime earlier)
            {
                DateTime between = earlier.AddTicks((expected[i].Ticks - earlier.Ticks) / 2);
                Assert.AreEqual(earlier, rule.GetPreviousOccurrence(kat.Start, between), Describe(kat, between));
            }
        }

        if (!kat.IsTruncated)
        {
            DateTime? last = expected.Length == 0 ? null : expected[^1];
            Assert.AreEqual(last, rule.GetPreviousOccurrence(kat.Start, DateTime.MaxValue, inclusive: true), Describe(kat, DateTime.MaxValue));
        }
    }

    /// <summary>
    /// Reads the python-dateutil sub-daily table from the embedded corpus.
    /// </summary>
    /// <returns>The corpus rows in file order.</returns>
    private static IEnumerable<DateutilSubDailyKat> LoadDateutil()
    {
        foreach (string[] f in ReadCsv("dateutil-subdaily-vectors.csv"))
        {
            yield return new DateutilSubDailyKat(
                $"dateutil #{f[0]} {f[3]}",
                ParseStart(f[2]),
                f[3],
                f[4],
                f[5].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ParseStart).ToArray());
        }
    }

    /// <summary>
    /// Describes a point query on a corpus rule for a failure message.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    /// <param name="query">The instant queried.</param>
    /// <returns>The rule, its start, and the query instant.</returns>
    private static string Describe(DateutilSubDailyKat kat, DateTime query) =>
        string.Create(CultureInfo.InvariantCulture, $"{kat.Rule} from {kat.Start:s} at {query:O}");
}
