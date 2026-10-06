// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceCorpusTests.CronFixes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceCorpusTests
{
    /// <summary>
    /// The cron libraries whose release-note fixes are catalogued, each in
    /// <c>corpus/recurrence/cron-fixes/&lt;library&gt;-fixes.csv</c>.
    /// </summary>
    /// <remarks>
    /// Each catalogue records every fix its library's release notes list, one row per fix, or one per case where a fix
    /// needs several. The <c>applies</c> and <c>dialect</c> rows carry a scenario and run through the reconciliation
    /// tests in <c>RecurrenceCorpusTests.CronLibraries.cs</c> beside the library tables; the <c>n/a</c> and
    /// <c>unknown</c> rows carry only the reason they do not run.
    /// </remarks>
    private static readonly string[] s_cronFixLibraries =
    [
        "ccronexpr", "cron-parser", "cron-utils", "croner", "croniter", "cronos", "cronsim", "fugit", "gorhill",
        "gronx", "ncrontab", "node-cron", "quartznet", "robfig", "saffron", "supertinycron", "zslayton",
    ];

    /// <summary>
    /// Verifies that each library's fix catalogue loads and holds the number of rows in each class the corpus README
    /// records, and reports the totals.
    /// </summary>
    /// <remarks>
    /// A catalogue that silently lost rows, or moved a fix from <c>applies</c> to <c>n/a</c>, would quietly shrink the
    /// fixes this suite claims to hold Bodu to.
    /// </remarks>
    [TestMethod]
    public void CronFixCatalogues_ShouldHoldTheRecordedRowsInEachClass()
    {
        (string Library, int Applies, int Dialect, int NotApplicable, int Unknown)[] recorded =
        [
            ("ccronexpr", 9, 0, 15, 2),
            ("cron-parser", 81, 17, 101, 0),
            ("cron-utils", 214, 44, 163, 1),
            ("croner", 62, 20, 118, 2),
            ("croniter", 85, 24, 117, 2),
            ("cronos", 66, 5, 58, 0),
            ("cronsim", 13, 0, 13, 0),
            ("fugit", 38, 16, 38, 0),
            ("gorhill", 17, 4, 2, 0),
            ("gronx", 40, 6, 16, 1),
            ("ncrontab", 6, 0, 4, 0),
            ("node-cron", 14, 4, 26, 0),
            ("quartznet", 47, 10, 40, 3),
            ("robfig", 7, 2, 24, 0),
            ("saffron", 0, 2, 3, 0),
            ("supertinycron", 20, 4, 23, 4),
            ("zslayton", 6, 12, 8, 0),
        ];

        CollectionAssert.AreEqual(
            s_cronFixLibraries,
            recorded.Select(r => r.Library).ToArray(),
            "The recorded counts do not cover the catalogued libraries.");

        var report = new StringBuilder();
        foreach ((string library, int applies, int dialect, int notApplicable, int unknown) in recorded)
        {
            string[][] rows = ReadCsv($"{library}-fixes.csv").ToArray();
            Dictionary<string, int> classes = rows
                .GroupBy(f => f[4], StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

            Assert.AreEqual(applies, classes.GetValueOrDefault("applies"), $"{library}: applies row count changed.");
            Assert.AreEqual(dialect, classes.GetValueOrDefault("dialect"), $"{library}: dialect row count changed.");
            Assert.AreEqual(notApplicable, classes.GetValueOrDefault("n/a"), $"{library}: n/a row count changed.");
            Assert.AreEqual(unknown, classes.GetValueOrDefault("unknown"), $"{library}: unknown row count changed.");
            Assert.AreEqual(applies + dialect + notApplicable + unknown, rows.Length, $"{library}: a row has no known class.");

            report.AppendLine(
                CultureInfo.InvariantCulture,
                $"{library}: {applies} applies, {dialect} dialect, {notApplicable} n/a, {unknown} unknown");
        }

        Console.WriteLine(report.ToString());
    }

    /// <summary>
    /// Verifies that every fix the catalogues do not run as written says why: each <c>dialect</c> row names the Bodu
    /// behaviour it asserts instead, and each <c>n/a</c> or <c>unknown</c> row the reason it has no scenario.
    /// </summary>
    [TestMethod]
    public void CronFixCatalogues_ShouldGiveAReasonForEveryFixNotRunAsWritten()
    {
        string[] unexplained = s_cronFixLibraries
            .SelectMany(library => ReadCsv($"{library}-fixes.csv"))
            .Where(f => f[4] is "dialect" or "n/a" or "unknown" && string.IsNullOrWhiteSpace(f[12]))
            .Select(f => $"{f[0]} {f[1]} {f[2]}: {f[3]}")
            .ToArray();

        Assert.AreEqual(0, unexplained.Length, $"Fixes recorded without a reason: {string.Join("; ", unexplained)}");
    }

    /// <summary>
    /// Reads the runnable scenarios of every library's fix catalogue from the embedded corpus.
    /// </summary>
    /// <returns>
    /// The <c>applies</c> and <c>dialect</c> rows, library by library in file order, each named after the library,
    /// release, reference, and fix it tests.
    /// </returns>
    private static IEnumerable<CronLibraryVectorKat> LoadCronLibraryFixes()
    {
        foreach (string library in s_cronFixLibraries)
        {
            foreach (string[] f in ReadCsv($"{library}-fixes.csv"))
            {
                if (f[4] is not ("applies" or "dialect"))
                    continue;

                string reference = f[2].Length > 0 ? $" {f[2]}" : string.Empty;
                string @case = f[5].Length > 0 ? $" (case {f[5]})" : string.Empty;
                string name = $"{f[0]} fix {f[1]}{reference}: {f[3]}{@case}";
                yield return new CronLibraryVectorKat(
                    name,
                    f[6],
                    ParseCronFormat(f[7], name),
                    f[8],
                    f[9],
                    f[10],
                    string.Empty);
            }
        }
    }
}
