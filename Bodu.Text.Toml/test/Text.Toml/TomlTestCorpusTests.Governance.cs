// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTestCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Governs the integrity of the vendored conformance corpus independently of the conformance run: every vendored case
/// is classified by a manifest, every manifest entry resolves to a file, every valid case has an expectation, the skip
/// list refers to real cases, the vendored counts match the pinned provenance, and every valid case survives a
/// write/reparse round-trip.
/// </summary>
public sealed partial class TomlTestCorpusTests
{
    /// <summary>
    /// Verifies that every <c>.toml</c> and <c>.json</c> entry in each version manifest resolves to a vendored file, so
    /// a manifest never references a case that is absent from the corpus.
    /// </summary>
    [TestMethod]
    public void Corpus_EveryManifestEntryRefersToAnExistingFile()
    {
        foreach (string manifest in ManifestFileNames)
        {
            foreach (string entry in ReadManifestPaths(manifest))
            {
                string path = Path.Combine(CorpusRoot, entry);
                Assert.IsTrue(File.Exists(path), $"{manifest}: lists '{entry}', which is not present in the vendored corpus.");
            }
        }
    }

    /// <summary>
    /// Verifies that every vendored <c>.toml</c> and <c>.json</c> file is listed in at least one version manifest, so a
    /// case can never be silently added to the corpus without being classified.
    /// </summary>
    [TestMethod]
    public void Corpus_EveryDocumentFileIsListedInAManifest()
    {
        HashSet<string> listed = AllManifestPaths();

        foreach (string directory in CaseDirectoryNames)
        {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(CorpusRoot, directory), "*.*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".toml", StringComparison.Ordinal) && !file.EndsWith(".json", StringComparison.Ordinal))
                    continue;

                string relative = Path.GetRelativePath(CorpusRoot, file).Replace('\\', '/');
                Assert.IsTrue(listed.Contains(relative), $"The corpus file '{relative}' is not listed in any version manifest.");
            }
        }
    }

    /// <summary>
    /// Verifies that every valid case named by a manifest is accompanied by its <c>.json</c> expectation file, so the
    /// conformance run can always compare against a pinned expectation.
    /// </summary>
    [TestMethod]
    public void Corpus_EveryValidCaseHasAnExpectationFile()
    {
        var validCases = AllManifestPaths()
            .Where(static entry => entry.StartsWith("valid/", StringComparison.Ordinal) && entry.EndsWith(".toml", StringComparison.Ordinal))
            .ToList();

        Assert.AreNotEqual(0, validCases.Count, "No valid cases were found in the version manifests.");
        foreach (string? entry in validCases)
        {
            string expectation = Path.Combine(CorpusRoot, Path.ChangeExtension(entry, ".json"));
            Assert.IsTrue(File.Exists(expectation), $"The valid case '{entry}' has no .json expectation file.");
        }
    }

    /// <summary>
    /// Verifies that every entry in the skip list refers to a vendored case that a manifest classifies, so a skip never
    /// silently does nothing because its path is stale.
    /// </summary>
    [TestMethod]
    public void Corpus_EverySkippedCaseExistsAndIsListed()
    {
        HashSet<string> listed = AllManifestPaths();

        foreach (string skipped in SkippedCases.Keys)
        {
            Assert.IsTrue(File.Exists(Path.Combine(CorpusRoot, skipped)), $"Skipped case '{skipped}' does not exist in the vendored corpus.");
            Assert.IsTrue(listed.Contains(skipped), $"Skipped case '{skipped}' is not listed in any version manifest.");
        }
    }

    /// <summary>
    /// Verifies that the vendored case counts match the counts pinned in <c>provenance.json</c>, catching an accidental
    /// addition or removal of cases when the corpus is updated.
    /// </summary>
    [TestMethod]
    public void Corpus_MatchesPinnedProvenance()
    {
        using var provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(CorpusRoot, "provenance.json")));
        JsonElement root = provenance.RootElement;

        int actualValid = Directory.EnumerateFiles(Path.Combine(CorpusRoot, "valid"), "*.toml", SearchOption.AllDirectories).Count();
        int actualInvalid = Directory.EnumerateFiles(Path.Combine(CorpusRoot, "invalid"), "*.toml", SearchOption.AllDirectories).Count();

        Assert.AreEqual(root.GetProperty("validCaseCount").GetInt32(), actualValid, "The vendored valid-case count drifted from the pinned provenance.");
        Assert.AreEqual(root.GetProperty("invalidCaseCount").GetInt32(), actualInvalid, "The vendored invalid-case count drifted from the pinned provenance.");
    }

    /// <summary>
    /// Verifies that every valid v1.0.0 case, after being parsed into a node tree, written back to TOML, and reparsed,
    /// still decodes to the values pinned by its JSON expectation — confirming the writer round-trips the corpus.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV10), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void RoundTrip_WhenValidCorpusDocument_ForV10_ShouldReparseToExpectedValues(CorpusKat kat) =>
        AssertValidCaseRoundTrips(kat);

    /// <summary>
    /// Verifies that every valid v1.1.0 case, after being parsed into a node tree, written back to TOML, and reparsed,
    /// still decodes to the values pinned by its JSON expectation — confirming the writer round-trips the corpus.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidCasesV11), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void RoundTrip_WhenValidCorpusDocument_ForV11_ShouldReparseToExpectedValues(CorpusKat kat) =>
        AssertValidCaseRoundTrips(kat);

    /// <summary>
    /// Parses a valid case under its profile, serializes the resulting node tree back to TOML, reparses it, and asserts
    /// the reparsed model still matches the case's JSON expectation.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    private static void AssertValidCaseRoundTrips(CorpusKat kat)
    {
        byte[] toml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath));
        string expectationPath = Path.Combine(CorpusRoot, Path.ChangeExtension(kat.RelativePath, ".json"));

        byte[] rewritten;
        var reader = new TomlDocumentReader(toml, new TomlReaderOptions { SpecVersion = kat.SpecVersion });
        Assert.IsTrue(reader.Read(), $"{kat.RelativePath}: the document produced no tokens.");
        rewritten = TomlNode.ReadFrom(ref reader).ToUtf8Bytes();

        var roundTripReader = new TomlDocumentReader(rewritten, new TomlReaderOptions { SpecVersion = kat.SpecVersion });
        Assert.IsTrue(roundTripReader.Read(), $"{kat.RelativePath}: the rewritten document produced no tokens.");
        object actual = BuildValue(ref roundTripReader);

        using var expected = JsonDocument.Parse(File.ReadAllBytes(expectationPath));
        AssertMatches(expected.RootElement, actual, kat.RelativePath);
    }

    /// <summary>
    /// Gets the version manifest file names that classify the corpus into the v1.0.0 and v1.1.0 profiles.
    /// </summary>
    /// <returns>The manifest file names.</returns>
    private static string[] ManifestFileNames =>
        ["files-toml-1.0.0", "files-toml-1.1.0"];

    /// <summary>
    /// Gets the corpus subdirectories that hold case files, used to scope the completeness sweep to vendored cases and
    /// exclude top-level files such as the licence and provenance.
    /// </summary>
    /// <returns>The case subdirectory names.</returns>
    private static string[] CaseDirectoryNames =>
        ["valid", "invalid"];

    /// <summary>
    /// Reads the <c>.toml</c> and <c>.json</c> case paths from the named manifest.
    /// </summary>
    /// <param name="manifest">The manifest file name.</param>
    /// <returns>The case paths the manifest lists.</returns>
    private static IEnumerable<string> ReadManifestPaths(string manifest) =>
        File.ReadLines(Path.Combine(CorpusRoot, manifest))
            .Where(static line => line.EndsWith(".toml", StringComparison.Ordinal) || line.EndsWith(".json", StringComparison.Ordinal));

    /// <summary>
    /// Builds the set of all case paths listed across every version manifest.
    /// </summary>
    /// <returns>The union of the case paths the manifests list.</returns>
    private static HashSet<string> AllManifestPaths()
    {
        var listed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string manifest in ManifestFileNames)
        {
            foreach (string entry in ReadManifestPaths(manifest))
                listed.Add(entry.Replace('\\', '/'));
        }

        return listed;
    }
}
