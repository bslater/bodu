// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Test.Corpus;

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvReleaseNoteCorpusTests
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
    /// Verifies that each library's catalogue holds the number of fixes, and of rows in each class, that
    /// <c>corpus/dotenv/README.md</c> records.
    /// </summary>
    /// <remarks>
    /// A catalogue that silently lost rows, or moved a fix from <c>applies</c> to <c>n/a</c>, would quietly shrink the
    /// fixes this suite holds Bodu to. A fix is the rows that share a version, a reference and a summary.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenCounted_ShouldHoldTheRecordedFixesAndRowsInEachClass()
    {
        (string Library, int Fixes, int Applies, int Dialect, int NotApplicable, int Unknown)[] recorded =
        [
            ("compose-go", 24, 27, 12, 9, 0),
            ("dotenv-node", 45, 23, 13, 31, 0),
            ("dotenv-ruby", 59, 20, 1, 47, 0),
            ("dotenvy", 19, 13, 0, 14, 0),
            ("dotnetenv", 13, 28, 2, 7, 0),
            ("godotenv", 27, 48, 12, 8, 0),
            ("phpdotenv", 56, 42, 4, 36, 0),
            ("python-dotenv", 71, 53, 3, 53, 0),
        ];

        CollectionAssert.AreEqual(
            recorded.Select(r => r.Library).ToArray(),
            s_catalogs.Value.Select(catalog => catalog.Library).ToArray(),
            "The recorded counts do not cover the embedded catalogues.");

        for (int i = 0; i < recorded.Length; i++)
        {
            (string library, int fixes, int applies, int dialect, int notApplicable, int unknown) = recorded[i];
            ReleaseNoteCatalog catalog = s_catalogs.Value[i];
            IReadOnlyDictionary<string, int> counts = catalog.CountByClass();

            Assert.AreEqual(fixes, catalog.Rows.Select(FixKey).Distinct().Count(), $"{library}: the fix count changed.");
            Assert.AreEqual(applies, counts["applies"], $"{library}: the applies row count changed.");
            Assert.AreEqual(dialect, counts["dialect"], $"{library}: the dialect row count changed.");
            Assert.AreEqual(notApplicable, counts["n/a"], $"{library}: the n/a row count changed.");
            Assert.AreEqual(unknown, counts["unknown"], $"{library}: the unknown row count changed.");
        }
    }

    /// <summary>
    /// Verifies that the catalogues hold the number of <c>applies</c> and <c>dialect</c> rows of each kind that
    /// <c>corpus/dotenv/README.md</c> records.
    /// </summary>
    /// <remarks>
    /// A row that moved from <c>parse</c> to another kind would change what this suite asserts about it without
    /// changing any class count.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenCounted_ShouldHoldTheRecordedRowsOfEachKind()
    {
        (string Kind, int Rows)[] recorded =
        [
            ("parse", 206),
            ("reject", 66),
            ("write", 3),
            ("write-reject", 0),
            ("roundtrip", 19),
            ("unit", 7),
        ];

        CollectionAssert.AreEqual(
            ReleaseNoteCatalog.Kinds.ToArray(),
            recorded.Select(r => r.Kind).ToArray(),
            "The recorded counts do not cover every kind.");

        foreach ((string kind, int rows) in recorded)
        {
            int actual = AllRows.Count(row => row.Class is "applies" or "dialect" && row.Kind == kind);

            Assert.AreEqual(rows, actual, $"The {kind} row count changed.");
        }
    }

    /// <summary>
    /// Verifies that a fix catalogued in several rows numbers its cases 1, 2, ... in file order, and that a fix
    /// catalogued in one row leaves its case empty.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenFixesAreRead_ShouldNumberTheirCasesInOrder()
    {
        var problems = new List<string>();
        foreach (ReleaseNoteCatalog catalog in s_catalogs.Value)
        {
            foreach (IGrouping<(string, string, string), ReleaseNoteFix> fix in catalog.Rows.GroupBy(FixKey))
            {
                ReleaseNoteFix[] rows = [.. fix];
                string[] cases = [.. rows.Select(row => row.Case)];
                string[] expected = rows.Length == 1
                    ? [string.Empty]
                    : [.. Enumerable.Range(1, rows.Length).Select(number => number.ToString(CultureInfo.InvariantCulture))];

                if (!cases.SequenceEqual(expected, StringComparer.Ordinal))
                {
                    problems.Add(
                        $"{rows[0].FileName}:{rows[0].LineNumber}: cases [{string.Join(", ", cases)}], not [{string.Join(", ", expected)}]");
                }
            }
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
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
    /// expected result in the notation its kind uses, a parse row's rendering in canonical form, and, for a rejecting
    /// row, the exception this suite asserts.
    /// </summary>
    /// <remarks>
    /// The rows themselves run in the Regression tier; this check catches a malformed row in every build. The suite
    /// has no runner for <c>write-reject</c>, which no row uses, so such a row is reported here until one is added.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenRunnableRowsAreRead_ShouldHoldWellFormedScenarios()
    {
        var problems = new List<string>();
        foreach (ReleaseNoteFix fix in AllRows.Where(row => row.IsRunnable))
        {
            try
            {
                _ = RowOptions.Parse(fix.Options);
                switch (fix.Kind)
                {
                    case "parse":
                        _ = CorpusEscapes.Decode(fix.Input);
                        RequireCanonicalRendering(fix);
                        break;

                    case "reject":
                        _ = CorpusEscapes.Decode(fix.Input);
                        RequireFormatException(fix);
                        break;

                    case "write":
                        _ = ReadWriteNotation(fix.Input);
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    case "roundtrip":
                        _ = CorpusEscapes.Decode(fix.Input);
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    default:
                        throw new FormatException($"This suite has no runner for kind {fix.Kind}.");
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
        IReadOnlyList<string> problems =
            ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(DotEnvReleaseNoteCorpusTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogues are byte-for-byte copies of the files in <c>corpus/dotenv/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(DotEnvReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Checks that a rejecting row names <see cref="DotEnvFormatException" />, the exception this suite asserts.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <exception cref="FormatException">The row names another exception.</exception>
    private static void RequireFormatException(ReleaseNoteFix fix)
    {
        if (fix.Expected != nameof(DotEnvFormatException))
        {
            throw new FormatException(
                $"The row expects {fix.Expected}; this suite asserts {nameof(DotEnvFormatException)} for kind {fix.Kind}.");
        }
    }

    /// <summary>
    /// Checks that a parse row's expected result is a rendering in canonical form: UTF-8 text in the write notation,
    /// written exactly as <see cref="Render" /> writes the entries it describes.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <exception cref="FormatException">
    /// The expected result is not valid UTF-8, not in the write notation, or written differently from its rendering.
    /// </exception>
    /// <remarks>
    /// The parse rows compare renderings byte for byte, so an expected result that escaped a character differently, or
    /// spaced its separators, would fail against a correct read.
    /// </remarks>
    private static void RequireCanonicalRendering(ReleaseNoteFix fix)
    {
        string expected = CorpusEscapes.DecodeText(fix.Expected);
        string canonical = Render(ReadWriteNotation(fix.Expected));

        if (!string.Equals(expected, canonical, StringComparison.Ordinal))
            throw new FormatException($"The expected rendering is not in canonical form: {EncodeText(canonical)}.");
    }

    /// <summary>
    /// Gets the key that groups the rows of one fix.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The row's version, reference and summary, which every row of a fix shares.</returns>
    private static (string Version, string Reference, string Summary) FixKey(ReleaseNoteFix row) =>
        (row.Version, row.Reference, row.Summary);
}
