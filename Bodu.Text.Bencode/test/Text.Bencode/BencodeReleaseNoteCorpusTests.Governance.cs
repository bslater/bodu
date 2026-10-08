// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
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
    /// Verifies that each library's catalogue holds the number of rows in each class that <c>corpus/bencode/README.md</c>
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
            ("bencode-py", 8, 1, 5, 0),
            ("bencodenet", 10, 0, 16, 0),
            ("bendy", 0, 0, 7, 0),
            ("libtorrent", 17, 0, 4, 0),
            ("node-bencode", 15, 7, 4, 0),
            ("serde-bencode", 11, 3, 4, 0),
            ("transmission", 16, 1, 3, 0),
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
                _ = RowOptions.Parse(fix.Options);
                switch (fix.Kind)
                {
                    case "parse":
                        _ = CorpusEscapes.Decode(fix.Input);
                        _ = NormalizeTranscript(fix.Expected);
                        break;

                    case "reject":
                        _ = CorpusEscapes.Decode(fix.Input);
                        RequireFormatException(fix);
                        break;

                    case "write":
                        _ = ReadWriteNotation(fix.Input);
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    case "write-reject":
                        _ = ReadWriteNotation(fix.Input);
                        RequireFormatException(fix);
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
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(BencodeReleaseNoteCorpusTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogues are byte-for-byte copies of the files in <c>corpus/bencode/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(BencodeReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Checks that a rejecting row names <see cref="BencodeFormatException" />, the exception this suite asserts.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <exception cref="FormatException">The row names another exception.</exception>
    private static void RequireFormatException(ReleaseNoteFix fix)
    {
        if (fix.Expected != nameof(BencodeFormatException))
            throw new FormatException($"The row expects {fix.Expected}; this suite asserts {nameof(BencodeFormatException)} for kind {fix.Kind}.");
    }
}
