// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;

namespace Bodu.IO.Compound;

public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that every embedded catalog loads without a problem: printable ASCII, the header lines, the column
    /// line, a reason on every row, known classes and kinds, valid escapes, and no repeated row name.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenLoaded_ShouldReportNoProblems()
    {
        string[] problems = s_catalogs.Value.SelectMany(catalog => catalog.Problems).ToArray();

        Assert.AreEqual(0, problems.Length, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that each library's catalog holds the number of rows in each class that
    /// <c>corpus/compound/README.md</c> records.
    /// </summary>
    /// <remarks>
    /// A catalog that silently lost rows, or moved a fix from <c>applies</c> to <c>n/a</c>, would quietly shrink the
    /// fixes this suite holds Bodu to.
    /// </remarks>
    [TestMethod]
    public void FixCatalogues_WhenCounted_ShouldHoldTheRecordedRowsInEachClass()
    {
        (string Library, int Applies, int Dialect, int NotApplicable, int Unknown)[] recorded =
        [
            ("cfb", 66, 1, 4, 0),
            ("compoundfiles", 7, 2, 2, 0),
            ("olefile", 37, 12, 20, 1),
            ("openmcdf", 120, 12, 32, 1),
            ("poi", 32, 4, 22, 1),
            ("poi-hpsf", 14, 1, 5, 1),
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
    /// Verifies that every runnable row holds a scenario this suite can run: option values it maps, an input in the
    /// notation its kind uses, an expected listing that decodes, and, for a rejecting kind, an exception this suite
    /// asserts.
    /// </summary>
    /// <remarks>
    /// The rows themselves run in the Regression tier; this check catches a malformed row in every build. It reads the
    /// notation only, without building a container.
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
                ValidateRowScenario(fix);
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
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(CompoundReleaseNoteCorpusTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogs are byte-for-byte copies of the files in <c>corpus/compound/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(CompoundReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Checks the input and expected result of a runnable row against the notation of its kind.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <exception cref="FormatException">
    /// The row's input or expected result is not well formed for its kind.
    /// </exception>
    private static void ValidateRowScenario(ReleaseNoteFix fix)
    {
        switch (fix.Kind)
        {
            case "parse":
                ValidateContainerField(fix.Input, allowEdits: false);
                _ = CorpusEscapes.DecodeText(fix.Expected);
                break;

            case "reject":
                ValidateContainerField(fix.Input, allowEdits: false);
                _ = ReadExpectedCategory(fix);
                break;

            case "roundtrip":
                ValidateContainerField(fix.Input, allowEdits: true);
                _ = CorpusEscapes.DecodeText(fix.Expected);
                break;

            case "write-reject":
                string input = CorpusEscapes.DecodeText(fix.Input);
                if (!HasEdits(input))
                    throw new FormatException("A write-reject row runs an edit script.");

                ValidateInput(input, allowEdits: true);
                _ = ReadWriteRejection(fix);
                break;

            default:
                throw new FormatException($"The kind {fix.Kind} has no runner in this suite; a write-side fix is a roundtrip or unit row.");
        }
    }

    /// <summary>
    /// Checks an input field that names a container: a <c>bytes:</c> literal, or a <c>gen:</c> pipeline optionally
    /// followed by an edit script.
    /// </summary>
    /// <param name="field">The escaped input field.</param>
    /// <param name="allowEdits">Whether the row's kind runs an edit script.</param>
    /// <exception cref="FormatException">The field is not well formed.</exception>
    private static void ValidateContainerField(string field, bool allowEdits)
    {
        if (field.StartsWith(BytesPrefix, StringComparison.Ordinal))
        {
            _ = CorpusEscapes.Decode(field[BytesPrefix.Length..]);
            return;
        }

        ValidateInput(CorpusEscapes.DecodeText(field), allowEdits);
    }
}
