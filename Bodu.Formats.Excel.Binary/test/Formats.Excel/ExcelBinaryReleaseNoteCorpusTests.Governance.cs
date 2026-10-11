// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that every embedded catalogue loads without a problem: printable ASCII, the header lines, the column
    /// line, a reason on every row, known classes and kinds, valid escapes, and no repeated row name.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenLoaded_ShouldReportNoProblems()
    {
        string[] problems = [.. s_catalogs.Value.SelectMany(catalog => catalog.Problems)];

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that each library's catalogue holds the number of rows in each class that <c>corpus/biff/README.md</c>
    /// records.
    /// </summary>
    /// <remarks>
    /// A catalogue that silently lost rows, or moved a fix from <c>applies</c> to <c>n/a</c>, would quietly shrink the
    /// fixes this suite holds Bodu to.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenCounted_ShouldHoldTheRecordedRowsInEachClass()
    {
        (string Library, int Applies, int Dialect, int NotApplicable, int Unknown)[] recorded =
        [
            ("apache-poi", 107, 10, 287, 6),
            ("calamine", 42, 5, 8, 0),
            ("exceldatareader", 63, 14, 10, 1),
            ("libxls", 38, 4, 16, 0),
            ("spreadsheet-parseexcel", 51, 1, 65, 3),
            ("xlrd", 69, 9, 57, 1),
        ];

        CollectionAssert.AreEqual(
            recorded.Select(r => r.Library).ToArray(),
            s_catalogs.Value.Select(catalog => catalog.Library).ToArray(),
            "The recorded counts do not cover the embedded catalogues.");

        foreach ((ReleaseNoteCatalog catalog, (string library, int applies, int dialect, int notApplicable, int unknown)) in s_catalogs.Value.Zip(recorded))
        {
            IReadOnlyDictionary<string, int> counts = catalog.CountByClass();

            Assert.AreEqual(applies, counts["applies"], $"{library}: the applies row count changed.");
            Assert.AreEqual(dialect, counts["dialect"], $"{library}: the dialect row count changed.");
            Assert.AreEqual(notApplicable, counts["n/a"], $"{library}: the n/a row count changed.");
            Assert.AreEqual(unknown, counts["unknown"], $"{library}: the unknown row count changed.");
        }
    }

    /// <summary>
    /// Verifies that every row's options are well formed and name only the options this suite maps.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenOptionsAreRead_ShouldNameOnlyKnownOptions()
    {
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindUnknownOptions(AllRows, s_optionNames);

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that every runnable row holds a scenario this suite can run: option values it maps, a generator
    /// expression the generators build, and an expected result in the notation its kind uses; for a <c>reject</c> row,
    /// an exception the row's surface throws.
    /// </summary>
    /// <remarks>
    /// The rows themselves run in the Regression tier; this check catches a malformed row in every build. It builds
    /// each record stream but not the compound file around it, so a container patch is checked for its shape only.
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
                _ = InputGenerator.Generate(fix.Input, options.LayoutVersion);
                switch (fix.Kind)
                {
                    case "parse":
                        _ = CorpusEscapes.DecodeText(fix.Expected);
                        break;

                    case "reject":
                        RequireException(fix, options.ReadsRecords ? s_recordsExceptions : s_workbookExceptions);
                        break;

                    case "roundtrip":
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    default:
                        throw new FormatException($"This suite does not run rows of kind {fix.Kind}.");
                }
            }
            catch (FormatException ex)
            {
                problems.Add($"{fix}: {ex.Message}");
            }
        }

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that every <c>unit</c> row names a test method that exists in this assembly.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenUnitRowsAreRead_ShouldNameExistingTests()
    {
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(ExcelBinaryReleaseNoteCorpusTests).Assembly);

        Assert.IsEmpty(problems, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogues are byte-for-byte copies of the files in <c>corpus/biff/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(ExcelBinaryReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Checks that a <c>reject</c> row names an exception its surface throws.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <param name="exceptionNames">
    /// The exceptions the row's surface throws: <see cref="s_workbookExceptions" /> or
    /// <see cref="s_recordsExceptions" />.
    /// </param>
    /// <exception cref="FormatException">The row names another exception.</exception>
    private static void RequireException(ReleaseNoteFix fix, string[] exceptionNames)
    {
        if (!exceptionNames.Contains(fix.Expected, StringComparer.Ordinal))
            throw new FormatException($"The row expects {fix.Expected}; this surface throws {string.Join(", ", exceptionNames)}.");
    }
}
