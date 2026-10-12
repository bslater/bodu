// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.IO.Pst.Internal;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that every embedded catalogue loads without a problem: printable ASCII, the header lines, the column
    /// line, a reason on every row, known classes and kinds, valid escapes, and no repeated row name.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenLoaded_ShouldReportNoProblems()
    {
        string[] problems = [.. s_catalogs.Value.SelectMany(catalog => catalog.Problems)];

        Assert.AreEqual(0, problems.Length, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that each library's catalogue holds the number of rows in each class that <c>corpus/pst/README.md</c>
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
            ("java-libpst", 49, 9, 20, 2),
            ("libpff", 104, 23, 160, 11),
            ("libpst", 54, 10, 116, 8),
            ("xstreader", 5, 0, 12, 0),
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
    /// Verifies that every runnable row holds a scenario this suite can run: options that fit together, an input whose
    /// generator pipeline builds a file, and an expected rendering, or for a <c>reject</c> row one of the exceptions
    /// this suite asserts.
    /// </summary>
    /// <remarks>
    /// The rows themselves run in the Regression tier; this check catches a malformed row in every build.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenRunnableRowsAreRead_ShouldHoldWellFormedScenarios()
    {
        string[] exceptions = [nameof(PstFileFormatException), nameof(PstUnsupportedFormatException), nameof(OutlookPstFormatException)];
        var problems = new List<string>();
        foreach (ReleaseNoteFix fix in AllRows.Where(row => row.IsRunnable))
        {
            try
            {
                _ = RowOptions.Parse(fix.Options);
                _ = GenerateInput(CorpusEscapes.DecodeText(fix.Input));
                switch (fix.Kind)
                {
                    case "parse":
                        _ = CorpusEscapes.DecodeText(fix.Expected);
                        break;

                    case "reject" when !exceptions.Contains(fix.Expected, StringComparer.Ordinal):
                        problems.Add($"{fix}: the row expects {fix.Expected}; this suite asserts one of {string.Join(", ", exceptions)}.");
                        break;

                    case "reject":
                        break;

                    default:
                        problems.Add($"{fix}: this suite runs parse and reject rows, not {fix.Kind}.");
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
    /// Verifies that every <c>unit</c> row names a test method that exists in this assembly or in the container test
    /// assembly, whose fixture builders this assembly references.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenUnitRowsAreRead_ShouldNameExistingTests()
    {
        // A row is a problem only when neither assembly holds the test it names: the container's tests live beside its
        // own fixtures, in Bodu.IO.Pst.Test.
        IReadOnlyList<string> missingHere = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(PstReleaseNoteCorpusTests).Assembly);
        IReadOnlyList<string> missingThere = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(PstFixtureBuilder).Assembly);
        string[] problems = [.. missingHere.Intersect(missingThere, StringComparer.Ordinal)];

        Assert.AreEqual(0, problems.Length, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogues are byte-for-byte copies of the files in <c>corpus/pst/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(PstReleaseNoteCorpusTests).Assembly, Area);
    }
}
