// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceCorpusTests.CronLibraries.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceCorpusTests
{
    /// <summary>
    /// The cron library tables reconciled here, each with the directory under <c>corpus/recurrence</c> that holds it and
    /// the prefix of its file name, <c>&lt;table&gt;-cron-vectors.csv</c>.
    /// </summary>
    /// <remarks>
    /// Cronos's forward table predates this schema and keeps its own partial, <c>RecurrenceCorpusTests.Cronos.cs</c>;
    /// its reverse table, derived later, is read here.
    /// </remarks>
    private static readonly (string Directory, string Table)[] s_cronLibraryTables =
    [
        ("ccronexpr", "ccronexpr"),
        ("cron-parser", "cron-parser"),
        ("croner", "croner"),
        ("croniter", "croniter"),
        ("cronos", "cronos-reverse"),
        ("cronsim", "cronsim"),
        ("fugit", "fugit"),
        ("gorhill", "gorhill"),
        ("gronx", "gronx"),
        ("ncrontab", "ncrontab"),
        ("node-cron", "node-cron"),
        ("quartznet", "quartznet"),
        ("robfig", "robfig"),
        ("saffron", "saffron"),
        ("supertinycron", "supertinycron"),
        ("zslayton", "zslayton"),
    ];

    /// <summary>The assertion kinds the cron library tables and fix catalogues may record.</summary>
    private static readonly string[] s_cronLibraryKinds =
    [
        "next", "next-inclusive", "previous", "previous-inclusive", "next-sequence", "previous-sequence",
        "unreachable-next", "unreachable-previous", "invalid", "valid", "equal", "not-equal", "to-string",
    ];

    /// <summary>
    /// Gets the in-scope rows that assert a next occurrence through the <see cref="DateTime" /> overload.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryNextOccurrences =>
        CronLibraryRows("next", "next-inclusive").Where(r => !r.HasOffset).Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that assert a next occurrence through the <see cref="DateTimeOffset" /> overload.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryNextOffsetOccurrences =>
        CronLibraryRows("next", "next-inclusive").Where(r => r.HasOffset).Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that assert a previous occurrence through the <see cref="DateTime" /> overload.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryPreviousOccurrences =>
        CronLibraryRows("previous", "previous-inclusive").Where(r => !r.HasOffset).Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that assert a previous occurrence through the <see cref="DateTimeOffset" /> overload.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryPreviousOffsetOccurrences =>
        CronLibraryRows("previous", "previous-inclusive").Where(r => r.HasOffset).Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that assert a run of successive next occurrences.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryNextSequences =>
        CronLibraryRows("next-sequence").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that assert a run of successive previous occurrences.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryPreviousSequences =>
        CronLibraryRows("previous-sequence").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows whose next occurrence must not exist.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryUnreachableNext =>
        CronLibraryRows("unreachable-next").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows whose previous occurrence must not exist.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryUnreachablePrevious =>
        CronLibraryRows("unreachable-previous").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows whose expression must be rejected.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryMalformed =>
        CronLibraryRows("invalid").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows whose expression must be accepted.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryWellFormed =>
        CronLibraryRows("valid").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that pair two spellings of the same schedule.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryEquivalent =>
        CronLibraryRows("equal").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that pair two expressions denoting different schedules.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryDistinct =>
        CronLibraryRows("not-equal").Select(r => new object[] { r });

    /// <summary>
    /// Gets the in-scope rows that assert an expression's canonical text.
    /// </summary>
    /// <value>The rows, from every library table and fix catalogue.</value>
    public static IEnumerable<object[]> CronLibraryCanonicalText =>
        CronLibraryRows("to-string").Select(r => new object[] { r });

    /// <summary>
    /// Verifies that each library's next-occurrence assertion holds for Bodu, inclusively or exclusively as the row
    /// records.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryNextOccurrences),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenCronLibraryVector_ShouldMatchTheAssertedInstant(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);

        DateTime? actual = cron.GetNextOccurrence(CronLibraryVectorKat.ParseLocal(kat.From), kat.Kind == "next-inclusive");

        Assert.AreEqual(CronLibraryVectorKat.ParseLocal(kat.Expected), actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that each library's next-occurrence assertion stated with an offset holds for Bodu's
    /// <see cref="DateTimeOffset" /> overload, including the offset the answer carries.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryNextOffsetOccurrences),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenCronLibraryVectorHasOffset_ShouldMatchTheAssertedInstantAndOffset(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);
        DateTimeOffset expected = CronLibraryVectorKat.ParseOffset(kat.Expected);

        DateTimeOffset? actual = cron.GetNextOccurrence(CronLibraryVectorKat.ParseOffset(kat.From), kat.Kind == "next-inclusive");

        Assert.IsTrue(actual.HasValue && actual.Value.EqualsExact(expected), $"{kat.Expression}: expected {expected:O}, got {actual:O}");
    }

    /// <summary>
    /// Verifies that each library's previous-occurrence assertion holds for Bodu, inclusively or exclusively as the row
    /// records.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryPreviousOccurrences),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenCronLibraryVector_ShouldMatchTheAssertedInstant(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);

        DateTime? actual = cron.GetPreviousOccurrence(CronLibraryVectorKat.ParseLocal(kat.From), kat.Kind == "previous-inclusive");

        Assert.AreEqual(CronLibraryVectorKat.ParseLocal(kat.Expected), actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that each library's previous-occurrence assertion stated with an offset holds for Bodu's
    /// <see cref="DateTimeOffset" /> overload, including the offset the answer carries.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryPreviousOffsetOccurrences),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenCronLibraryVectorHasOffset_ShouldMatchTheAssertedInstantAndOffset(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);
        DateTimeOffset expected = CronLibraryVectorKat.ParseOffset(kat.Expected);

        DateTimeOffset? actual = cron.GetPreviousOccurrence(CronLibraryVectorKat.ParseOffset(kat.From), kat.Kind == "previous-inclusive");

        Assert.IsTrue(actual.HasValue && actual.Value.EqualsExact(expected), $"{kat.Expression}: expected {expected:O}, got {actual:O}");
    }

    /// <summary>
    /// Verifies that querying forward from each answer in turn reproduces the run of occurrences a library asserts, so
    /// a schedule's stride is checked as well as its first match.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryNextSequences),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenCronLibrarySequence_ShouldMatchEachAssertedInstant(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);
        DateTime[] expected = kat.ExpectedInstants.Select(CronLibraryVectorKat.ParseLocal).ToArray();

        var actual = new List<DateTime>();
        DateTime at = CronLibraryVectorKat.ParseLocal(kat.From);
        while (actual.Count < expected.Length && cron.GetNextOccurrence(at) is DateTime next)
        {
            actual.Add(next);
            at = next;
        }

        CollectionAssert.AreEqual(expected, actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that querying backward from each answer in turn reproduces the run of occurrences a library asserts.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryPreviousSequences),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenCronLibrarySequence_ShouldMatchEachAssertedInstant(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);
        DateTime[] expected = kat.ExpectedInstants.Select(CronLibraryVectorKat.ParseLocal).ToArray();

        var actual = new List<DateTime>();
        DateTime at = CronLibraryVectorKat.ParseLocal(kat.From);
        while (actual.Count < expected.Length && cron.GetPreviousOccurrence(at) is DateTime previous)
        {
            actual.Add(previous);
            at = previous;
        }

        CollectionAssert.AreEqual(expected, actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that an expression with no later match in the calendar reports no next occurrence rather than searching
    /// without end or throwing.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryUnreachableNext),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetNextOccurrence_WhenCronLibraryVectorIsUnreachable_ShouldReturnNull(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);

        DateTime? actual = cron.GetNextOccurrence(CronLibraryVectorKat.ParseLocal(kat.From));

        Assert.IsNull(actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that an expression with no earlier match in the calendar reports no previous occurrence rather than
    /// searching without end or throwing.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryUnreachablePrevious),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void GetPreviousOccurrence_WhenCronLibraryVectorIsUnreachable_ShouldReturnNull(CronLibraryVectorKat kat)
    {
        CronExpression cron = CronExpression.Parse(kat.Expression, kat.Format);

        DateTime? actual = cron.GetPreviousOccurrence(CronLibraryVectorKat.ParseLocal(kat.From));

        Assert.IsNull(actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that each expression a library rejects, and Bodu's dialect rejects too, fails to parse without
    /// throwing.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryMalformed),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenCronLibraryVectorIsMalformed_ShouldReturnFalse(CronLibraryVectorKat kat)
    {
        bool parsed = CronExpression.TryParse(kat.Expression, kat.Format, out CronExpression? result);

        Assert.IsFalse(parsed, kat.Expression);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that each expression a library accepts, and Bodu's dialect accepts too, parses.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryWellFormed),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenCronLibraryVectorIsWellFormed_ShouldReturnTrue(CronLibraryVectorKat kat)
    {
        bool parsed = CronExpression.TryParse(kat.Expression, kat.Format, out CronExpression? result, out string? failureMessage);

        Assert.IsTrue(parsed, $"{kat.Expression}: {failureMessage}");
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Verifies that two spellings a library treats as one schedule compare equal and hash alike.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryEquivalent),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Equals_WhenCronLibraryVectorsDenoteTheSameSchedule_ShouldReturnTrue(CronLibraryVectorKat kat)
    {
        CronExpression left = CronExpression.Parse(kat.Expression, kat.Format);
        CronExpression right = CronExpression.Parse(kat.Expected, kat.Format);

        Assert.IsTrue(left.Equals(right), $"{kat.Expression} vs {kat.Expected}");
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode(), $"{kat.Expression} vs {kat.Expected}");
    }

    /// <summary>
    /// Verifies that two expressions a library treats as different schedules do not compare equal.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryDistinct),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Equals_WhenCronLibraryVectorsDenoteDifferentSchedules_ShouldReturnFalse(CronLibraryVectorKat kat)
    {
        CronExpression left = CronExpression.Parse(kat.Expression, kat.Format);
        CronExpression right = CronExpression.Parse(kat.Expected, kat.Format);

        Assert.IsFalse(left.Equals(right), $"{kat.Expression} vs {kat.Expected}");
    }

    /// <summary>
    /// Verifies that an expression renders to the canonical text the row records.
    /// </summary>
    /// <param name="kat">The row under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(CronLibraryCanonicalText),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToString_WhenCronLibraryVector_ShouldMatchTheCanonicalText(CronLibraryVectorKat kat)
    {
        string actual = CronExpression.Parse(kat.Expression, kat.Format).ToString();

        Assert.AreEqual(kat.Expected, actual, kat.Expression);
    }

    /// <summary>
    /// Verifies that every library table loads the number of rows, and of in-scope rows, the corpus README records, and
    /// reports each table's exclusions by flag.
    /// </summary>
    /// <remarks>
    /// A table that silently loaded no rows would let every reconciliation above pass vacuously, and an exclusion that
    /// grew unnoticed would quietly shrink the coverage the README claims.
    /// </remarks>
    [TestMethod]
    public void CronLibraryTables_ShouldLoadTheRecordedRowsAndReportTheirExclusions()
    {
        (string Table, int Rows, int InScope)[] recorded =
        [
            ("ccronexpr", 88, 88),
            ("cron-parser", 285, 225),
            ("croner", 367, 297),
            ("croniter", 389, 264),
            ("cronos-reverse", 29, 26),
            ("cronsim", 438, 432),
            ("fugit", 434, 309),
            ("gorhill", 69, 63),
            ("gronx", 434, 385),
            ("ncrontab", 187, 184),
            ("node-cron", 97, 91),
            ("quartznet", 385, 269),
            ("robfig", 108, 104),
            ("saffron", 290, 244),
            ("supertinycron", 536, 478),
            ("zslayton", 91, 73),
        ];

        CollectionAssert.AreEqual(
            s_cronLibraryTables.Select(t => t.Table).ToArray(),
            recorded.Select(r => r.Table).ToArray(),
            "The recorded counts do not cover the reconciled tables.");

        var report = new StringBuilder();
        foreach ((string table, int rows, int inScope) in recorded)
        {
            CronLibraryVectorKat[] loaded = LoadCronLibrary(table).ToArray();

            Assert.AreEqual(rows, loaded.Length, $"{table}: row count changed.");
            Assert.AreEqual(inScope, loaded.Count(r => r.IsInScope), $"{table}: in-scope row count changed.");

            report.AppendLine(CultureInfo.InvariantCulture, $"{table}: {inScope}/{rows} in scope");
            foreach (IGrouping<string, CronLibraryVectorKat> group in loaded
                .Where(r => !r.IsInScope)
                .SelectMany(r => r.ExclusionFlags.Select(f => (Flag: f, Row: r)))
                .GroupBy(p => p.Flag, p => p.Row)
                .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                report.AppendLine(CultureInfo.InvariantCulture, $"  excluded [{group.Key}] {group.Count()} rows");
            }
        }

        Console.WriteLine(report.ToString());
    }

    /// <summary>
    /// Verifies that every row of every library table and fix catalogue records an assertion kind the reconciliation
    /// runs, so no row is dropped from every data source by a misspelt kind.
    /// </summary>
    [TestMethod]
    public void CronLibraryTables_ShouldRecordOnlyKindsTheReconciliationRuns()
    {
        string[] unknown = s_cronLibraryTables
            .SelectMany(t => LoadCronLibrary(t.Table))
            .Concat(LoadCronLibraryFixes())
            .Where(r => !s_cronLibraryKinds.Contains(r.Kind))
            .Select(r => $"{r.Name} ({r.Kind})")
            .ToArray();

        Assert.AreEqual(0, unknown.Length, string.Join("; ", unknown));
    }

    /// <summary>
    /// Returns the in-scope rows of the given kinds from every library table and fix catalogue.
    /// </summary>
    /// <param name="kinds">The assertion kinds to keep.</param>
    /// <returns>The rows, library tables first, each in file order.</returns>
    private static IEnumerable<CronLibraryVectorKat> CronLibraryRows(params string[] kinds) =>
        s_cronLibraryTables
            .SelectMany(t => LoadCronLibrary(t.Table))
            .Concat(LoadCronLibraryFixes())
            .Where(r => r.IsInScope && kinds.Contains(r.Kind));

    /// <summary>
    /// Reads one library's vector table from the embedded corpus.
    /// </summary>
    /// <param name="table">The table's file-name prefix.</param>
    /// <returns>Every row of the table, in file order.</returns>
    private static IEnumerable<CronLibraryVectorKat> LoadCronLibrary(string table)
    {
        foreach (string[] f in ReadCsv($"{table}-cron-vectors.csv"))
        {
            yield return new CronLibraryVectorKat(
                $"{table} {f[0]} '{f[3]}'",
                f[1],
                ParseCronFormat(f[2], $"{table} {f[0]}"),
                f[3],
                f[4],
                f[5],
                f[7]);
        }
    }

    /// <summary>
    /// Reads a table's <c>format</c> column, rejecting any value but the two field layouts.
    /// </summary>
    /// <param name="value">The column text.</param>
    /// <param name="row">The row's label, for the failure message.</param>
    /// <returns>The field layout the column names.</returns>
    /// <exception cref="FormatException">The column names no field layout.</exception>
    private static CronFormat ParseCronFormat(string value, string row) =>
        value switch
        {
            "standard" => CronFormat.Standard,
            "withSeconds" => CronFormat.WithSeconds,
            _ => throw new FormatException($"{row}: '{value}' is not a cron format."),
        };
}
