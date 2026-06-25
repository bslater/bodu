// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using Bodu.Test.Kat;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Runs the vendored <c>yaml/yaml-test-suite</c> conformance corpus against the library's YAML 1.2 core,
/// JSON-compatible tree profile. Every vendored case carries a classification in <c>classification.tsv</c>, and each
/// classification is asserted: supported-valid cases parse and match their JSON expectation, supported-invalid and
/// profile-unsupported cases are rejected, and documented known gaps are skipped by the run but pinned by the
/// governance suite.
/// </summary>
/// <remarks>
/// The corpus and its classification are copied to the test output by the project's <c>YamlTestCorpus\**</c> glob. The
/// classification was produced once against this library and is checked in; the governance suite guarantees it stays
/// exhaustive and matches the pinned provenance.
/// </remarks>
[TestClass]
public sealed partial class YamlTestCorpusTests
{
    /// <summary>
    /// The root directory of the vendored corpus within the test output.
    /// </summary>
    private static readonly string CorpusRoot = Path.Combine(AppContext.BaseDirectory, "YamlTestCorpus");

    /// <summary>
    /// Yields the corpus cases classified as supported-valid with a JSON expectation.
    /// </summary>
    /// <returns>One row per supported-valid case.</returns>
    public static IEnumerable<object[]> SupportedPassCases() => CasesIn("SupportedPass");

    /// <summary>
    /// Yields the corpus cases classified as supported-valid without a JSON expectation.
    /// </summary>
    /// <returns>One row per parse-only case.</returns>
    public static IEnumerable<object[]> SupportedParseOnlyCases() => CasesIn("SupportedParseOnly");

    /// <summary>
    /// Yields the corpus cases classified as supported-invalid (the suite expects a parse error).
    /// </summary>
    /// <returns>One row per supported-invalid case.</returns>
    public static IEnumerable<object[]> SupportedFailCases() => CasesIn("SupportedFail");

    /// <summary>
    /// Yields the corpus cases classified as valid YAML that the profile deliberately rejects.
    /// </summary>
    /// <returns>One row per profile-unsupported case.</returns>
    public static IEnumerable<object[]> UnsupportedFeatureCases() => CasesIn("UnsupportedFeatureRejected");

    /// <summary>
    /// Verifies that every supported-valid corpus document parses and matches its JSON expectation.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedPassCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenSupportedValidCorpusDocument_ShouldMatchExpectation(CorpusKat kat)
    {
        var yaml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath, "in.yaml"));
        var jsonBytes = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath, "in.json"));

        using var doc = YamlDocument.Parse(yaml);

        if (Encoding.UTF8.GetString(jsonBytes).Trim().Length == 0)
        {
            Assert.AreEqual(YamlValueKind.Null, doc.RootElement.ValueKind, kat.RelativePath);
            return;
        }

        using var expected = JsonDocument.Parse(jsonBytes);
        Assert.IsTrue(CorpusCompare.Matches(expected.RootElement, doc.RootElement), kat.RelativePath);
    }

    /// <summary>
    /// Verifies that every parse-only corpus document parses without error.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedParseOnlyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenSupportedParseOnlyCorpusDocument_ShouldParse(CorpusKat kat)
    {
        var yaml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath, "in.yaml"));

        using var doc = YamlDocument.Parse(yaml);
        Assert.IsNotNull(doc);
    }

    /// <summary>
    /// Verifies that every supported-invalid corpus document is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedFailCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenInvalidCorpusDocument_ShouldThrowYamlFormatException(CorpusKat kat) =>
        AssertRejected(kat);

    /// <summary>
    /// Verifies that every profile-unsupported corpus document is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    [TestMethod]
    [DynamicData(nameof(UnsupportedFeatureCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenUnsupportedFeatureCorpusDocument_ShouldThrowYamlFormatException(CorpusKat kat) =>
        AssertRejected(kat);

    /// <summary>
    /// Asserts that a corpus document is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus case under test.</param>
    private static void AssertRejected(CorpusKat kat)
    {
        var yaml = File.ReadAllBytes(Path.Combine(CorpusRoot, kat.RelativePath, "in.yaml"));

        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse(yaml);
        });
    }

    /// <summary>
    /// Reads the classification manifest and yields the cases in the requested category.
    /// </summary>
    /// <param name="category">The classification category to select.</param>
    /// <returns>One row per case in the category.</returns>
    private static IEnumerable<object[]> CasesIn(string category)
    {
        foreach (var (id, _, cat) in ReadManifest())
        {
            if (string.Equals(cat, category, StringComparison.Ordinal))
                yield return [new CorpusKat(id)];
        }
    }

    /// <summary>
    /// Reads the classification manifest rows.
    /// </summary>
    /// <returns>The manifest rows as identifier, suite kind, and classification triples.</returns>
    internal static IEnumerable<(string Id, string Kind, string Category)> ReadManifest()
    {
        foreach (var line in File.ReadLines(Path.Combine(CorpusRoot, "classification.tsv")))
        {
            if (line.Length == 0)
                continue;

            var parts = line.Split('\t');
            yield return (parts[0], parts[1], parts[2]);
        }
    }

    /// <summary>
    /// A corpus case row identified by its path relative to the corpus root.
    /// </summary>
    /// <param name="RelativePath">The case path relative to the corpus root.</param>
    public sealed record CorpusKat(string RelativePath) : IKat
    {
        /// <inheritdoc />
        public string Name => RelativePath;
    }
}
