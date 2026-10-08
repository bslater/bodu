// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;

namespace Bodu.Text.Toml;

public sealed partial class TomlReleaseNoteCorpusTests
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
    /// Verifies that each library's catalogue holds the number of rows in each class that <c>corpus/toml/README.md</c>
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
            ("burntsushi", 73, 4, 26, 0),
            ("go-toml", 192, 10, 71, 0),
            ("serde_spanned", 0, 0, 1, 0),
            ("smol-toml", 49, 4, 11, 0),
            ("toml", 119, 12, 28, 2),
            ("toml_datetime", 12, 3, 3, 0),
            ("toml_edit", 70, 9, 51, 0),
            ("toml_parser", 12, 0, 7, 0),
            ("toml_writer", 2, 0, 0, 0),
            ("tomlet", 43, 1, 10, 0),
            ("tomli", 47, 6, 8, 0),
            ("tomllib", 0, 0, 2, 0),
            ("tomlplusplus", 47, 2, 96, 0),
            ("tomlyn", 50, 4, 31, 1),
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
                        CheckTaggedTable(CorpusEscapes.Decode(fix.Expected));
                        break;

                    case "reject":
                        _ = CorpusEscapes.Decode(fix.Input);
                        RequireException(fix, nameof(TomlFormatException));
                        break;

                    case "write":
                        CheckTaggedTable(CorpusEscapes.Decode(fix.Input));
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    case "write-reject":
                        CheckTaggedTable(CorpusEscapes.Decode(fix.Input));
                        RequireException(fix, nameof(TomlSerializationException));
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
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(TomlReleaseNoteCorpusTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogues are byte-for-byte copies of the files in <c>corpus/toml/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(TomlReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Checks that a rejecting row names the exception this suite asserts for its kind.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <param name="exceptionName">
    /// The exception the suite asserts: <see cref="TomlFormatException" /> for a read and
    /// <see cref="TomlSerializationException" /> for a write.
    /// </param>
    /// <exception cref="FormatException">The row names another exception.</exception>
    private static void RequireException(ReleaseNoteFix fix, string exceptionName)
    {
        if (fix.Expected != exceptionName)
            throw new FormatException($"The row expects {fix.Expected}; this suite asserts {exceptionName} for kind {fix.Kind}.");
    }
}
