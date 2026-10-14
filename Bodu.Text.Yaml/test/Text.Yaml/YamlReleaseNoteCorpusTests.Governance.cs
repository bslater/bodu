// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReleaseNoteCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

using Bodu.Test.Corpus;

namespace Bodu.Text.Yaml;

public sealed partial class YamlReleaseNoteCorpusTests
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
    /// Verifies that each library's catalog holds the number of rows in each class that <c>corpus/yaml/README.md</c>
    /// records.
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
            ("eemeli-yaml", 212, 11, 102, 0),
            ("go-yaml", 79, 12, 61, 0),
            ("js-yaml", 115, 14, 49, 1),
            ("libyaml", 24, 3, 21, 0),
            ("pyyaml", 22, 6, 44, 0),
            ("ruamel-yaml", 81, 8, 145, 1),
            ("snakeyaml-engine", 160, 14, 11, 0),
            ("yaml-cpp", 48, 4, 27, 0),
            ("yamldotnet", 148, 14, 66, 2),
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
    /// expected result in the notation its kind uses, and, for a rejecting row, the exception this suite asserts.
    /// </summary>
    /// <remarks>
    /// The rows themselves run in the Regression tier; this check catches a malformed row in every build. The suite
    /// has no runner for <c>write-reject</c>, which no row uses, so such a row is reported rather than silently skipped.
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
                byte[] input = CorpusEscapes.Decode(fix.Input);
                switch (fix.Kind)
                {
                    case "parse":
                        _ = ReadExpectedDocuments(fix.Expected);
                        break;

                    case "reject":
                        RequireFormatException(fix);
                        break;

                    case "write":
                        RequireNoOptions(fix);
                        JsonDocument.Parse(input).Dispose();
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    case "roundtrip":
                        _ = CorpusEscapes.Decode(fix.Expected);
                        break;

                    default:
                        throw new FormatException($"This suite has no runner for kind {fix.Kind}.");
                }
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
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
        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(AllRows, typeof(YamlReleaseNoteCorpusTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that the embedded catalogs are byte-for-byte copies of the files in <c>corpus/yaml/fixes/</c>.
    /// </summary>
    [TestMethod]
    public void FixCatalogues_WhenComparedWithTheCorpusTree_ShouldMatchByteForByte()
    {
        CorpusTree.AssertEmbeddedCopiesMatch(typeof(YamlReleaseNoteCorpusTests).Assembly, Area);
    }

    /// <summary>
    /// Checks that a rejecting row names <see cref="YamlFormatException" />, the exception this suite asserts.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <exception cref="FormatException">The row names another exception.</exception>
    private static void RequireFormatException(ReleaseNoteFix fix)
    {
        if (fix.Expected != nameof(YamlFormatException))
            throw new FormatException($"The row expects {fix.Expected}; this suite asserts {nameof(YamlFormatException)} for kind {fix.Kind}.");
    }

    /// <summary>
    /// Checks that a <c>write</c> row carries no options, since the writer runs with its default options and the
    /// options this suite maps configure only reading.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <exception cref="FormatException">The row carries options.</exception>
    private static void RequireNoOptions(ReleaseNoteFix fix)
    {
        if (fix.Options.Length != 0)
            throw new FormatException($"A write row takes no options, but this row sets {fix.Options}.");
    }
}
