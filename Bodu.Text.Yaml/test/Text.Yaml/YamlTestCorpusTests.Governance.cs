// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Text.Yaml;

/// <summary>
/// Governs the integrity of the vendored conformance corpus independently of the conformance run: every vendored case
/// is classified exactly once, every classification entry resolves to a vendored case, every classification category
/// is recognized, and the vendored and classified counts match the pinned provenance.
/// </summary>
public sealed partial class YamlTestCorpusTests
{
    /// <summary>
    /// The recognized classification categories.
    /// </summary>
    private static readonly HashSet<string> KnownCategories = new(StringComparer.Ordinal)
    {
        "SupportedPass",
        "SupportedParseOnly",
        "SupportedFail",
        "UnsupportedFeatureRejected",
        "KnownGap",
    };

    /// <summary>
    /// Verifies that every vendored case (a directory containing <c>in.yaml</c>) is classified exactly once.
    /// </summary>
    [TestMethod]
    public void Corpus_EveryCaseIsClassifiedOnce()
    {
        var classified = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, _, _) in ReadManifest())
        {
            Assert.IsTrue(classified.Add(id), $"The case '{id}' is classified more than once.");
        }

        foreach (var file in Directory.EnumerateFiles(CorpusRoot, "in.yaml", SearchOption.AllDirectories))
        {
            var id = Path.GetRelativePath(CorpusRoot, Path.GetDirectoryName(file)!).Replace('\\', '/');
            Assert.IsTrue(classified.Contains(id), $"The vendored case '{id}' is not classified in classification.tsv.");
        }
    }

    /// <summary>
    /// Verifies that every classification entry refers to a vendored case and uses a recognized category.
    /// </summary>
    [TestMethod]
    public void Corpus_EveryManifestEntryResolvesAndIsRecognized()
    {
        foreach (var (id, _, category) in ReadManifest())
        {
            Assert.IsTrue(
                File.Exists(Path.Combine(CorpusRoot, id.Replace('/', Path.DirectorySeparatorChar), "in.yaml")),
                $"The classification lists '{id}', which is not present in the vendored corpus.");

            Assert.IsTrue(KnownCategories.Contains(category), $"The case '{id}' uses an unrecognized category '{category}'.");
        }
    }

    /// <summary>
    /// Verifies that every supported-valid case is accompanied by its <c>in.json</c> expectation file.
    /// </summary>
    [TestMethod]
    public void Corpus_EverySupportedValidCaseHasAJsonExpectation()
    {
        foreach (var (id, _, category) in ReadManifest())
        {
            if (!string.Equals(category, "SupportedPass", StringComparison.Ordinal))
                continue;

            Assert.IsTrue(
                File.Exists(Path.Combine(CorpusRoot, id.Replace('/', Path.DirectorySeparatorChar), "in.json")),
                $"The supported-valid case '{id}' has no in.json expectation.");
        }
    }

    /// <summary>
    /// Verifies that no vendored case is classified <c>KnownGap</c>, enforcing the project's zero-gap conformance goal
    /// independently of the pinned provenance counts (which a manual edit could otherwise mask).
    /// </summary>
    [TestMethod]
    public void Corpus_HasNoKnownGaps()
    {
        var gaps = ReadManifest()
            .Where(entry => string.Equals(entry.Category, "KnownGap", StringComparison.Ordinal))
            .Select(entry => entry.Id)
            .ToList();

        Assert.AreEqual(
            0,
            gaps.Count,
            $"The conformance corpus must have zero KnownGap cases, but found: {string.Join(", ", gaps)}.");
    }

    /// <summary>
    /// Verifies that the vendored and classified counts match the pinned <c>provenance.json</c>, catching accidental
    /// drift when the corpus or its classification is updated.
    /// </summary>
    [TestMethod]
    public void Corpus_MatchesPinnedProvenance()
    {
        using var provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(CorpusRoot, "provenance.json")));
        var root = provenance.RootElement;

        var actualCases = Directory.EnumerateFiles(CorpusRoot, "in.yaml", SearchOption.AllDirectories).Count();
        Assert.AreEqual(root.GetProperty("caseCount").GetInt32(), actualCases, "The vendored case count drifted from the pinned provenance.");

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (_, _, category) in ReadManifest())
            counts[category] = counts.GetValueOrDefault(category) + 1;

        var pinned = root.GetProperty("classification");
        foreach (var category in KnownCategories)
        {
            Assert.AreEqual(
                pinned.GetProperty(category).GetInt32(),
                counts.GetValueOrDefault(category),
                $"The '{category}' count drifted from the pinned provenance.");
        }
    }
}
