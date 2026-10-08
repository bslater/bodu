// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text;

using Bodu.Test.Corpus;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that every embedded catalogue loads without a problem: printable ASCII, the header lines, the column
    /// line, a reason on every row, known classes and kinds, valid escapes, and no repeated row name.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenLoaded_ShouldReportNoProblems()
    {
        string[] problems = s_catalogs.Value.SelectMany(catalog => catalog.Problems).ToArray();

        Assert.AreEqual(0, problems.Length, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that each library's catalogue holds the number of fixes, of rows in each class, and of runnable rows that
    /// <c>corpus/delimited/README.md</c> records.
    /// </summary>
    /// <remarks>
    /// A catalogue that silently lost rows, moved a fix from <c>applies</c> to <c>n/a</c>, or turned a runnable row into
    /// a <c>unit</c> row would quietly shrink the fixes this suite holds Bodu to.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenCounted_ShouldHoldTheRecordedRowsInEachClass()
    {
        (string Library, int Fixes, int Applies, int Dialect, int NotApplicable, int Unknown, int Runnable)[] recorded =
        [
            ("commons-csv", 116, 34, 5, 89, 0, 34),
            ("cpython-csv", 66, 16, 2, 58, 0, 17),
            ("csv-parse", 101, 28, 8, 72, 0, 29),
            ("csvhelper", 208, 44, 4, 166, 0, 26),
            ("go-csv", 12, 11, 6, 2, 0, 11),
            ("papaparse", 106, 32, 4, 71, 6, 31),
            ("rust-csv", 34, 16, 3, 21, 0, 14),
            ("sep", 16, 10, 1, 9, 0, 7),
            ("sylvan-csv", 57, 40, 6, 25, 0, 33),
        ];

        CollectionAssert.AreEqual(
            recorded.Select(r => r.Library).ToArray(),
            s_catalogs.Value.Select(catalog => catalog.Library).ToArray(),
            "The recorded counts do not cover the embedded catalogues.");

        foreach ((ReleaseNoteCatalog catalog, (string library, int fixes, int applies, int dialect, int notApplicable, int unknown, int runnable))
            in s_catalogs.Value.Zip(recorded))
        {
            IReadOnlyDictionary<string, int> counts = catalog.CountByClass();

            Assert.AreEqual(fixes, CountFixes(catalog.Rows), $"{library}: the fix count changed.");
            Assert.AreEqual(applies, counts["applies"], $"{library}: the applies row count changed.");
            Assert.AreEqual(dialect, counts["dialect"], $"{library}: the dialect row count changed.");
            Assert.AreEqual(notApplicable, counts["n/a"], $"{library}: the n/a row count changed.");
            Assert.AreEqual(unknown, counts["unknown"], $"{library}: the unknown row count changed.");
            Assert.AreEqual(runnable, catalog.Rows.Count(row => row.IsRunnable), $"{library}: the runnable row count changed.");
        }
    }

    /// <summary>
    /// Verifies that every row's options are well formed and name only the options this suite maps.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenOptionsAreRead_ShouldNameOnlyKnownOptions()
    {
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindUnknownOptions(AllRows, s_optionNames);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that every runnable row holds a scenario this suite can run: option values it maps, an input and an
    /// expected result in the notation its kind uses, and, for a rejecting kind, the exception this suite asserts.
    /// </summary>
    /// <remarks>
    /// The rows themselves run in the Regression tier; this check catches a malformed row in every build.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenRunnableRowsAreRead_ShouldHoldWellFormedScenarios()
    {
        var problems = new List<string>();
        foreach (ReleaseNoteFix fix in AllRows.Where(row => row.IsRunnable))
        {
            try
            {
                RowOptions options = RowOptions.Parse(fix.Options);
                switch (fix.Kind)
                {
                    case "parse":
                        _ = CorpusEscapes.Decode(fix.Input);
                        RequireExpected(fix);
                        break;

                    case "reject":
                        _ = CorpusEscapes.Decode(fix.Input);
                        RequireException(fix, nameof(DelimitedFormatException));
                        break;

                    case "write":
                        _ = ReadWriteNotation(CorpusEscapes.Decode(fix.Input), options.NoHeader);
                        RequireExpected(fix);
                        break;

                    case "write-reject":
                        _ = ReadWriteNotation(CorpusEscapes.Decode(fix.Input), options.NoHeader);
                        RequireException(fix, nameof(ArgumentNullException));
                        break;

                    default:
                        _ = CorpusEscapes.Decode(fix.Input);
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;
                }
            }
            catch (FormatException ex)
            {
                problems.Add($"{fix}: {ex.Message}");
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that every <c>unit</c> row names a test method that exists in this assembly.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenUnitRowsAreRead_ShouldNameExistingTests()
    {
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(DelimitedReleaseNoteCorpusTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogues are byte-for-byte copies of the files in <c>corpus/delimited/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(DelimitedReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Verifies that the rows of a fix that needs several, adjacent rows sharing its version, reference and summary,
    /// number their cases 1, 2 and so on in order, and that a fix with one row leaves its case empty.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenAFixHasSeveralRows_ShouldNumberItsCasesInOrder()
    {
        var problems = new List<string>();
        foreach (ReleaseNoteCatalog catalog in s_catalogs.Value)
        {
            IReadOnlyList<ReleaseNoteFix> rows = catalog.Rows;
            int first = 0;
            while (first < rows.Count)
            {
                int next = first + 1;
                while (next < rows.Count && IsSameFix(rows[first], rows[next]))
                    next++;

                for (int i = first; i < next; i++)
                {
                    string expectedCase = next - first == 1 ? string.Empty : (i - first + 1).ToString(CultureInfo.InvariantCulture);
                    if (!string.Equals(rows[i].Case, expectedCase, StringComparison.Ordinal))
                        problems.Add($"{rows[i].FileName}:{rows[i].LineNumber}: the case is '{rows[i].Case}', not '{expectedCase}'");
                }

                first = next;
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that every row is written in canonical RFC 4180 form, which <c>corpus/delimited/README.md</c> requires: a
    /// field is quoted only when it holds a comma or a double quote.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenRowsAreRead_ShouldQuoteOnlyTheFieldsThatNeedIt()
    {
        var problems = new List<string>();
        foreach (ReleaseNoteCatalog catalog in s_catalogs.Value)
        {
            string[] lines = ReadEmbeddedLines(catalog.FileName);
            foreach (ReleaseNoteFix fix in catalog.Rows)
            {
                string canonical = JoinCanonically(
                    fix.Library, fix.Version, fix.Reference, fix.Summary, fix.Class, fix.Case,
                    fix.Kind, fix.Options, fix.Input, fix.Expected, fix.Expectation, fix.Reason);

                if (!string.Equals(lines[fix.LineNumber - 1], canonical, StringComparison.Ordinal))
                    problems.Add($"{fix.FileName}:{fix.LineNumber}: the row is not in canonical form, which reads {canonical}");
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Counts the fixes a catalogue's rows describe.
    /// </summary>
    /// <param name="rows">The rows, in file order.</param>
    /// <returns>The number of runs of adjacent rows that belong to the same fix.</returns>
    private static int CountFixes(IReadOnlyList<ReleaseNoteFix> rows)
    {
        int fixes = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (i == 0 || !IsSameFix(rows[i - 1], rows[i]))
                fixes++;
        }

        return fixes;
    }

    /// <summary>
    /// Determines whether two adjacent rows belong to the same fix.
    /// </summary>
    /// <param name="first">The earlier row.</param>
    /// <param name="second">The row that follows it.</param>
    /// <returns><see langword="true" /> when the rows share their version, reference and summary.</returns>
    private static bool IsSameFix(ReleaseNoteFix first, ReleaseNoteFix second) =>
        string.Equals(first.Version, second.Version, StringComparison.Ordinal)
        && string.Equals(first.Reference, second.Reference, StringComparison.Ordinal)
        && string.Equals(first.Summary, second.Summary, StringComparison.Ordinal);

    /// <summary>
    /// Reads the lines of a catalogue embedded in this assembly.
    /// </summary>
    /// <param name="fileName">The catalogue's file name.</param>
    /// <returns>The lines, the first at index zero, so that a row's line is at its line number less one.</returns>
    private static string[] ReadEmbeddedLines(string fileName)
    {
        Assembly assembly = typeof(DelimitedReleaseNoteCorpusTests).Assembly;
        string resource = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(".Fixtures.ReleaseNotes." + fileName, StringComparison.Ordinal));

        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream, Encoding.ASCII);
        return reader.ReadToEnd().Split('\n');
    }

    /// <summary>
    /// Joins fields into a catalogue line in canonical RFC 4180 form.
    /// </summary>
    /// <param name="fields">The fields, in column order.</param>
    /// <returns>The line, each field that holds a comma or a double quote enclosed in quotes with its quotes doubled.</returns>
    private static string JoinCanonically(params string[] fields) =>
        string.Join(',', fields.Select(field => field.AsSpan().ContainsAny(',', '"')
            ? "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : field));

    /// <summary>
    /// Checks that a row gives the expected result its kind compares against.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <exception cref="FormatException">The expected result is empty, or is not in the catalogue's escape notation.</exception>
    private static void RequireExpected(ReleaseNoteFix fix)
    {
        if (CorpusEscapes.Decode(fix.Expected).Length == 0)
            throw new FormatException($"A {fix.Kind} row gives the expected result.");
    }

    /// <summary>
    /// Checks that a rejecting row names the exception this suite asserts for its kind.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <param name="exceptionName">The name of the exception this suite asserts.</param>
    /// <exception cref="FormatException">The row names another exception.</exception>
    private static void RequireException(ReleaseNoteFix fix, string exceptionName)
    {
        if (!string.Equals(fix.Expected, exceptionName, StringComparison.Ordinal))
            throw new FormatException($"The row expects {fix.Expected}; this suite asserts {exceptionName} for kind {fix.Kind}.");
    }
}
