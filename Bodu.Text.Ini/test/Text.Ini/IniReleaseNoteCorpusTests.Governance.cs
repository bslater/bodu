// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;

namespace Bodu.Text.Ini;

public sealed partial class IniReleaseNoteCorpusTests
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
    /// Verifies that each library's catalogue holds the number of rows in each class that <c>corpus/ini/README.md</c>
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
            ("configparser", 22, 7, 47, 0),
            ("go-ini", 19, 7, 48, 0),
            ("ini-parser", 21, 1, 15, 0),
            ("inih", 11, 7, 18, 0),
            ("iniparser", 10, 5, 25, 0),
            ("microsoft-extensions-configuration-ini", 5, 1, 5, 0),
            ("npm-ini", 8, 4, 9, 0),
            ("rust-ini", 14, 4, 10, 0),
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

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that every runnable row holds a scenario this suite can run: option values it maps, an input and an
    /// expected result in the notation its kind uses, for a rejecting kind the exception this suite asserts, and a kind
    /// this suite has a data-driven test for.
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
                _ = RowOptions.Parse(fix.Options);
                switch (fix.Kind)
                {
                    case "parse":
                        _ = CorpusEscapes.Decode(fix.Input);
                        _ = CorpusEscapes.DecodeText(fix.Expected);
                        break;

                    case "reject":
                        _ = CorpusEscapes.Decode(fix.Input);
                        RequireException(fix, nameof(IniFormatException));
                        break;

                    case "write-reject":
                        _ = ReadWriteNotation(fix.Input);
                        RequireException(fix, nameof(ArgumentException));
                        break;

                    case "roundtrip":
                        _ = CorpusEscapes.Decode(fix.Input);
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    default:
                        throw new FormatException($"This suite has no data-driven test for kind {fix.Kind}; add one with the row.");
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
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(IniReleaseNoteCorpusTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogues are byte-for-byte copies of the files in <c>corpus/ini/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(IniReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Checks that a rejecting row names the exception this suite asserts for its kind.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <param name="exceptionName">The name of the exception this suite asserts for the row's kind.</param>
    /// <exception cref="FormatException">The row names another exception.</exception>
    private static void RequireException(ReleaseNoteFix fix, string exceptionName)
    {
        if (fix.Expected != exceptionName)
            throw new FormatException($"The row expects {fix.Expected}; this suite asserts {exceptionName} for kind {fix.Kind}.");
    }
}
