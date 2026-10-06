// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Reconciles the recurrence engine against the committed validation corpora: the normative RFC 5545 §3.8.5.3
/// worked examples, the occurrence counts libical's reference implementation asserts in its own test data, the
/// occurrences python-dateutil produces for generated sub-daily rules, the cron vectors derived from the test suites
/// of Cronos and twenty other cron libraries, and the scenarios of the fixes in those libraries' release notes.
/// </summary>
/// <remarks>
/// <para>
/// Provenance, licensing, and the reasoning behind each scope exclusion are recorded in
/// <c>corpus/recurrence/README.md</c>; the tables here are copies of that tree, embedded so the tests are
/// reproducible from committed artifacts alone, and held byte-identical to it.
/// </para>
/// <para>
/// Rows a table cannot check - intra-day time expansion in a table that records dates only, occurrence lists the
/// RFC abbreviates, and <c>EXRULE</c>, which the library does not model - are excluded by flag and reported by name,
/// never silently passed over.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class RecurrenceCorpusTests
{
    /// <summary>The flags that place a corpus row outside the modelled surface.</summary>
    /// <remarks>
    /// A UTC-valued <c>UNTIL</c> against a zoned start is deliberately <b>not</b> excluded. This library compares
    /// the bound as a wall-clock instant rather than resolving the start's zone, and for every such row in both
    /// corpora no occurrence falls inside the resulting offset window, so the two readings bracket the same
    /// occurrence set. That is a property of these vectors rather than a general theorem; a future row where an
    /// occurrence does fall inside the window would surface here as a difference, which is the intended signal.
    /// </remarks>
    private static readonly string[] s_excludedFlags =
        ["time-expansion", "elided", "exrule"];

    /// <summary>
    /// The embedded corpus tables other than the cron library tables and fix catalogues, each with the directory under
    /// <c>corpus/recurrence</c> it copies.
    /// </summary>
    private static readonly (string Directory, string FileName)[] s_corpusSources =
    [
        ("rfc5545", "rfc5545-recurrence-examples.csv"),
        ("libical", "libical-recur-expectations.csv"),
        ("dateutil", "dateutil-subdaily-vectors.csv"),
        ("cronos", "cronos-cron-vectors.csv"),
    ];

    /// <summary>
    /// Gets the RFC 5545 §3.8.5.3 examples whose occurrence lists are fully enumerable and in scope.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> Rfc5545Examples =>
        LoadRfc5545().Where(r => r.IsInScope).Select(r => new object[] { r });

    /// <summary>
    /// Gets the libical rules whose asserted occurrence counts are in scope.
    /// </summary>
    /// <value>The corpus rows.</value>
    public static IEnumerable<object[]> LibicalRules =>
        LoadLibical().Where(r => r.IsInScope).Select(r => new object[] { r });

    /// <summary>
    /// Verifies that each normative RFC 5545 example expands to exactly the occurrences the standard lists.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Rfc5545Examples),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenRfc5545Example_ShouldMatchTheStandard(Rfc5545ExampleKat kat)
    {
        string[] actual = RecurrenceRule.Parse(kat.Rule)
            .GetOccurrences(kat.Start)
            .Take(kat.Expected.Length)
            .Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToArray();

        CollectionAssert.AreEqual(kat.Expected, actual, kat.Rule);
    }

    /// <summary>
    /// Verifies that each RFC 5545 example emits occurrences at the time of day carried by its start instant.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Rfc5545Examples),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenRfc5545Example_ShouldPreserveTheStartTimeOfDay(Rfc5545ExampleKat kat)
    {
        TimeSpan[] times = RecurrenceRule.Parse(kat.Rule)
            .GetOccurrences(kat.Start)
            .Take(kat.Expected.Length)
            .Select(d => d.TimeOfDay)
            .Distinct()
            .ToArray();

        CollectionAssert.AreEqual(new[] { kat.Start.TimeOfDay }, times, kat.Rule);
    }

    /// <summary>
    /// Verifies that each libical rule produces exactly the number of occurrences its reference implementation
    /// asserts, which is the assertion that catches both duplicated and dropped occurrences.
    /// </summary>
    /// <param name="kat">The corpus row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(LibicalRules),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetOccurrences_WhenLibicalRule_ShouldProduceTheAssertedCount(LibicalRuleKat kat)
    {
        RecurrenceRule rule = RecurrenceRule.Parse(kat.Rule);

        // Taking more than the asserted count turns "produced too many" into a visible difference rather than a
        // silent truncation to the expected value.
        int actual = kat.ExceptionDate is DateTime exdate
            ? new RecurrenceSet(kat.Start, [rule], exceptionDates: [exdate]).GetOccurrences().Take(kat.ExpectedCount + 50).Count()
            : rule.GetOccurrences(kat.Start).Take(kat.ExpectedCount + 50).Count();

        Assert.AreEqual(kat.ExpectedCount, actual, kat.Rule);
    }

    /// <summary>
    /// Verifies that every occurrence stream drawn from either corpus is strictly ascending and free of duplicates,
    /// which is the structural property the RFC requires of a recurrence set.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetOccurrences_WhenDrawnFromEitherCorpus_ShouldBeStrictlyAscending()
    {
        var offenders = new List<string>();

        foreach (string rule in LoadRfc5545().Where(r => r.IsInScope).Select(r => r.Rule)
            .Concat(LoadLibical().Where(r => r.IsInScope).Select(r => r.Rule)))
        {
            DateTime[] occurrences = RecurrenceRule.Parse(rule)
                .GetOccurrences(new DateTime(1997, 9, 2, 9, 0, 0))
                .Take(60)
                .ToArray();

            for (int i = 1; i < occurrences.Length; i++)
            {
                if (occurrences[i] <= occurrences[i - 1])
                {
                    offenders.Add($"{rule} ({occurrences[i - 1]:yyyy-MM-dd} then {occurrences[i]:yyyy-MM-dd})");
                    break;
                }
            }
        }

        Assert.AreEqual(0, offenders.Count, string.Join("; ", offenders));
    }

    /// <summary>
    /// Verifies that the corpora loaded, that the in-scope share is what the recorded reconciliation claims, and
    /// that every excluded row is named with the reason it was excluded.
    /// </summary>
    /// <remarks>
    /// A corpus that silently loaded zero rows would let every reconciliation above pass vacuously, and an
    /// exclusion that grew unnoticed would quietly shrink the coverage this suite claims.
    /// </remarks>
    [TestMethod]
    public void Corpora_ShouldLoadAndReportTheirExclusions()
    {
        Rfc5545ExampleKat[] rfc = LoadRfc5545().ToArray();
        LibicalRuleKat[] libical = LoadLibical().ToArray();
        CronosVectorKat[] cronos = LoadCronos().ToArray();
        DateutilSubDailyKat[] dateutil = LoadDateutil().ToArray();

        Assert.AreEqual(39, rfc.Length, "RFC 5545 corpus row count changed.");
        Assert.AreEqual(57, libical.Length, "libical corpus row count changed.");
        Assert.AreEqual(1354, cronos.Length, "Cronos corpus row count changed.");
        Assert.AreEqual(200, dateutil.Length, "dateutil corpus row count changed.");
        Assert.AreEqual(23, rfc.Count(r => r.IsInScope), "RFC 5545 in-scope row count changed.");
        Assert.AreEqual(56, libical.Count(r => r.IsInScope), "libical in-scope row count changed.");
        Assert.AreEqual(1074, cronos.Count(r => r.IsInScope), "Cronos in-scope row count changed.");

        var report = new StringBuilder();
        report.AppendLine(CultureInfo.InvariantCulture, $"RFC 5545: {rfc.Count(r => r.IsInScope)}/{rfc.Length} in scope");
        foreach (Rfc5545ExampleKat row in rfc.Where(r => !r.IsInScope))
            report.AppendLine(CultureInfo.InvariantCulture, $"  excluded [{ExclusionReason(row.Flags, row.Expected.Length)}] {row.Name}");
        report.AppendLine(CultureInfo.InvariantCulture, $"libical: {libical.Count(r => r.IsInScope)}/{libical.Length} in scope");
        foreach (LibicalRuleKat row in libical.Where(r => !r.IsInScope))
            report.AppendLine(CultureInfo.InvariantCulture, $"  excluded [{ExclusionReason(row.Flags, row.ExpectedCount)}] {row.Name}");

        // Every dateutil row is in scope; the tally says how many record a whole stream, and how many of those are
        // empty because dateutil rejects the rule.
        report.AppendLine(
            CultureInfo.InvariantCulture,
            $"dateutil: {dateutil.Length}/{dateutil.Length} in scope, {dateutil.Count(r => r.IsTruncated)} truncated, {dateutil.Count(r => !r.IsTruncated && r.Expected.Length == 0)} with an empty stream");

        // The Cronos table is large enough that naming every excluded row would bury the summary, so its
        // exclusions are reported as a per-flag tally instead.
        report.AppendLine(CultureInfo.InvariantCulture, $"Cronos: {cronos.Count(r => r.IsInScope)}/{cronos.Length} in scope");
        foreach (IGrouping<string, CronosVectorKat> group in cronos
            .Where(r => !r.IsInScope)
            .SelectMany(r => r.ExclusionFlags.Select(f => (Flag: f, Row: r)))
            .GroupBy(p => p.Flag, p => p.Row)
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"  excluded [{group.Key}] {group.Count()} rows");
        }

        Console.WriteLine(report.ToString());
    }

    /// <summary>
    /// Verifies that each embedded corpus table is a byte-for-byte copy of its source under <c>corpus/recurrence</c>, so
    /// the tables these tests read cannot drift from the ones the corpus README documents.
    /// </summary>
    [TestMethod]
    public void Corpora_ShouldMatchTheirSourcesInTheCorpusTree()
    {
        string? root = FindRepositoryRoot();
        if (root is null)
        {
            Assert.Inconclusive("The repository root was not found from the test base directory.");
            return;
        }

        Assembly assembly = typeof(RecurrenceCorpusTests).Assembly;
        foreach ((string directory, string fileName) in CorpusSources())
        {
            byte[] source = File.ReadAllBytes(Path.Combine(root, "corpus", "recurrence", directory, fileName));
            string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName, StringComparison.Ordinal));
            using Stream stream = assembly.GetManifestResourceStream(name)!;
            using var copy = new MemoryStream();
            stream.CopyTo(copy);

            Assert.IsTrue(
                source.AsSpan().SequenceEqual(copy.ToArray()),
                $"Fixtures/Vectors/{fileName} differs from corpus/recurrence/{directory}/{fileName}.");
        }
    }

    /// <summary>
    /// Returns every embedded corpus table, each with the directory under <c>corpus/recurrence</c> it copies.
    /// </summary>
    /// <returns>The recurrence and Cronos tables, then the cron library tables, then the fix catalogues.</returns>
    /// <remarks>
    /// The lists live in three partial files, and C# leaves the order in which static fields declared in different
    /// partial files are initialized unspecified, so they are combined here rather than in a field initializer.
    /// </remarks>
    private static IEnumerable<(string Directory, string FileName)> CorpusSources() =>
        s_corpusSources
            .Concat(s_cronLibraryTables.Select(t => (t.Directory, $"{t.Table}-cron-vectors.csv")))
            .Concat(s_cronFixLibraries.Select(library => ("cron-fixes", $"{library}-fixes.csv")));

    /// <summary>
    /// Finds the repository root by walking up from the test's base directory to the solution file.
    /// </summary>
    /// <returns>The repository root, or <see langword="null" /> when the tests run outside a checkout.</returns>
    private static string? FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "bodu.slnx")))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the RFC 5545 example table from the embedded corpus.
    /// </summary>
    /// <returns>The corpus rows in file order.</returns>
    private static IEnumerable<Rfc5545ExampleKat> LoadRfc5545()
    {
        foreach (string[] f in ReadCsv("rfc5545-recurrence-examples.csv"))
        {
            yield return new Rfc5545ExampleKat(
                $"RFC5545 #{f[0]} {f[1]}",
                ParseStart(f[2]),
                f[3],
                f[4],
                f[5].Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    /// <summary>
    /// Reads the libical expectation table from the embedded corpus.
    /// </summary>
    /// <returns>The corpus rows in file order.</returns>
    private static IEnumerable<LibicalRuleKat> LoadLibical()
    {
        foreach (string[] f in ReadCsv("libical-recur-expectations.csv"))
        {
            yield return new LibicalRuleKat(
                $"libical #{f[0]} {f[1]}",
                ParseStart(f[2]),
                f[4],
                int.Parse(f[5], CultureInfo.InvariantCulture),
                string.IsNullOrEmpty(f[6]) ? null : ParseStart(f[6]),
                f[7]);
        }
    }

    /// <summary>
    /// Parses an iCalendar <c>DATE-TIME</c> as a wall-clock instant, discarding any UTC designator.
    /// </summary>
    /// <param name="value">The iCalendar date-time text.</param>
    /// <returns>The wall-clock instant.</returns>
    /// <remarks>
    /// Both corpora state their occurrences in the start's own zone, and the library is offset-based, so the
    /// comparison is performed on wall-clock readings throughout.
    /// </remarks>
    private static DateTime ParseStart(string value) =>
        DateTime.ParseExact(value.TrimEnd('Z'), "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads an embedded corpus table, skipping the provenance header and the column header.
    /// </summary>
    /// <param name="fileName">The corpus file name.</param>
    /// <returns>The data rows, each split into fields.</returns>
    private static IEnumerable<string[]> ReadCsv(string fileName)
    {
        Assembly assembly = typeof(RecurrenceCorpusTests).Assembly;
        string name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fileName, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);

        bool headerSeen = false;
        while (reader.ReadLine() is string line)
        {
            if (line.Length == 0 || line[0] == '#')
                continue;

            if (!headerSeen)
            {
                headerSeen = true;
                continue;
            }

            yield return SplitCsv(line);
        }
    }

    /// <summary>
    /// Splits one CSV record, honoring double-quoted fields and doubled quote escapes.
    /// </summary>
    /// <param name="line">The record text.</param>
    /// <returns>The field values.</returns>
    private static string[] SplitCsv(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());
        return [.. fields];
    }

    /// <summary>
    /// Determines whether a corpus row's flags place it inside the modelled surface.
    /// </summary>
    /// <param name="flags">The space-separated flag list.</param>
    /// <returns><see langword="true" /> when no excluding flag is present; otherwise <see langword="false" />.</returns>
    internal static bool InScope(string flags) =>
        ExcludingFlags(flags).Length == 0;

    /// <summary>
    /// Returns the flags of a corpus row that place it outside the modelled surface.
    /// </summary>
    /// <param name="flags">The space-separated flag list.</param>
    /// <returns>The excluding flags, space-separated, or an empty string when there are none.</returns>
    private static string ExcludingFlags(string flags) =>
        string.Join(' ', flags.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(f => s_excludedFlags.Contains(f)));

    /// <summary>
    /// Describes why a corpus row is out of scope, for the exclusion report.
    /// </summary>
    /// <param name="flags">The row's space-separated flag list.</param>
    /// <param name="expectedCount">The number of occurrences the corpus records for the row.</param>
    /// <returns>The excluding flags, or a note that the corpus records no occurrences for the row.</returns>
    private static string ExclusionReason(string flags, int expectedCount)
    {
        string excluding = ExcludingFlags(flags);
        return excluding.Length > 0 || expectedCount > 0 ? excluding : "no occurrences transcribed";
    }
}
