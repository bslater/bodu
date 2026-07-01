// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusTests.Governance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Governs the integrity of the vendored conformance corpus independently of the conformance run: every vendored case
/// resolves to exactly one recognized classification, every by-name profile classification refers to a real case, the
/// derived structure is consistent, and the case and category counts match the values pinned in
/// <see cref="YamlTestCorpusReader" />.
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
    };

    /// <summary>
    /// Verifies that every vendored case (a directory containing <c>in.yaml</c>) resolves to exactly one recognized
    /// category, leaving no case unattributed (the zero-gap conformance goal).
    /// </summary>
    [TestMethod]
    public void Corpus_EveryCaseResolvesToExactlyOneKnownCategory()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string? id, string _, string? category) in YamlTestCorpusReader.EnumerateCases())
        {
            Assert.IsTrue(seen.Add(id), $"The case '{id}' was enumerated more than once.");
            Assert.IsTrue(
                KnownCategories.Contains(category),
                $"The case '{id}' resolved to an unrecognized category '{category}'.");
        }
    }

    /// <summary>
    /// Verifies that every identifier the profile classifies by name still refers to a vendored case, so a corpus
    /// update that renames or removes a case cannot leave a stale entry silently classifying nothing.
    /// </summary>
    [TestMethod]
    public void Corpus_EveryProfileClassifiedIdResolvesToACase()
    {
        var corpus = new HashSet<string>(YamlTestCorpusReader.EnumerateCaseIds(), StringComparer.Ordinal);
        foreach (string id in YamlTestCorpusReader.ProfileClassifiedIds)
        {
            Assert.IsTrue(
                corpus.Contains(id),
                $"The by-name profile classification lists '{id}', which is not present in the corpus submodule.");
        }
    }

    /// <summary>
    /// Verifies that every supported-valid case is accompanied by its <c>in.json</c> expectation file.
    /// </summary>
    [TestMethod]
    public void Corpus_EverySupportedValidCaseHasAJsonExpectation()
    {
        foreach ((string? id, string _, string? category) in YamlTestCorpusReader.EnumerateCases())
        {
            if (!string.Equals(category, "SupportedPass", StringComparison.Ordinal))
                continue;

            Assert.IsTrue(
                File.Exists(Path.Combine(YamlTestCorpusReader.VectorDirectory(id), "in.json")),
                $"The supported-valid case '{id}' has no in.json expectation.");
        }
    }

    /// <summary>
    /// Verifies that every vendored case mirrors the upstream layout by carrying its <c>===</c> description file, so
    /// the corpus stays a faithful structural copy of the yaml-test-suite repository.
    /// </summary>
    [TestMethod]
    public void Corpus_EveryCaseHasUpstreamDescriptionFile()
    {
        foreach (string id in YamlTestCorpusReader.EnumerateCaseIds())
        {
            Assert.IsTrue(
                File.Exists(Path.Combine(YamlTestCorpusReader.VectorDirectory(id), "===")),
                $"The case '{id}' is missing its upstream '===' description file.");
        }
    }

    /// <summary>
    /// Verifies that the vendored case count and the per-category counts match the values pinned in
    /// <see cref="YamlTestCorpusReader" />, catching accidental drift when the corpus or its classification changes.
    /// </summary>
    [TestMethod]
    public void Corpus_MatchesPinnedCounts()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        int total = 0;
        foreach ((string _, string _, string? category) in YamlTestCorpusReader.EnumerateCases())
        {
            counts[category] = counts.GetValueOrDefault(category) + 1;
            total++;
        }

        Assert.AreEqual(YamlTestCorpusReader.ExpectedCaseCount, total, "The vendored case count drifted from the pinned expectation.");

        foreach ((string? category, int expected) in YamlTestCorpusReader.ExpectedCategoryCounts)
        {
            Assert.AreEqual(
                expected,
                counts.GetValueOrDefault(category),
                $"The '{category}' count drifted from the pinned expectation.");
        }
    }
}
