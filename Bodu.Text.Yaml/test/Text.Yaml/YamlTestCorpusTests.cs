// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Runs the vendored <c>yaml/yaml-test-suite</c> conformance corpus against the library's Bodu YAML Core Tree Profile.
/// Each vector is loaded by <see cref="YamlTestCorpusReader" /> into a <see cref="YamlTestVector" /> KAT carrying its
/// classification: supported-valid vectors parse and match their JSON expectation, supported-invalid and
/// profile-unsupported vectors are rejected, and the governance suite keeps the classification exhaustive and pinned.
/// </summary>
[TestClass]
public sealed partial class YamlTestCorpusTests
{
    /// <summary>
    /// The root directory of the vendored corpus within the test output.
    /// </summary>
    private static readonly string CorpusRoot = YamlTestCorpusReader.CorpusRoot;

    /// <summary>
    /// Yields the corpus vectors classified as supported-valid with a JSON expectation.
    /// </summary>
    /// <returns>One row per supported-valid vector.</returns>
    public static IEnumerable<object[]> SupportedPassCases() => VectorsIn("SupportedPass");

    /// <summary>
    /// Yields the corpus vectors classified as supported-valid without a JSON expectation.
    /// </summary>
    /// <returns>One row per parse-only vector.</returns>
    public static IEnumerable<object[]> SupportedParseOnlyCases() => VectorsIn("SupportedParseOnly");

    /// <summary>
    /// Yields the corpus vectors classified as supported-invalid (the suite expects a parse error).
    /// </summary>
    /// <returns>One row per supported-invalid vector.</returns>
    public static IEnumerable<object[]> SupportedFailCases() => VectorsIn("SupportedFail");

    /// <summary>
    /// Yields the corpus vectors classified as valid YAML that the profile deliberately rejects.
    /// </summary>
    /// <returns>One row per profile-unsupported vector.</returns>
    public static IEnumerable<object[]> UnsupportedFeatureCases() => VectorsIn("UnsupportedFeatureRejected");

    /// <summary>
    /// Verifies that every supported-valid corpus vector parses and matches its JSON expectation.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedPassCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenSupportedValidCorpusDocument_ShouldMatchExpectation(YamlTestVector kat)
    {
        // An empty JSON expectation denotes an empty document, which resolves to a null root.
        if (Encoding.UTF8.GetString(kat.Json!).Trim().Length == 0)
        {
            using var emptyDoc = YamlDocument.Parse(kat.Yaml);
            Assert.AreEqual(YamlValueKind.Null, emptyDoc.RootElement.ValueKind, kat.Id);
            return;
        }

        var expectedValues = CorpusCompare.SplitJsonValues(kat.Json!);
        if (expectedValues.Count == 1)
        {
            using var doc = YamlDocument.Parse(kat.Yaml);
            Assert.IsTrue(CorpusCompare.Matches(expectedValues[0], doc.RootElement), kat.Id);
            return;
        }

        var documents = YamlDocument.ParseAllDocuments(kat.Yaml);
        Assert.AreEqual(expectedValues.Count, documents.Count, $"{kat.Id}: document count.");
        for (var i = 0; i < documents.Count; i++)
            Assert.IsTrue(CorpusCompare.Matches(expectedValues[i], documents[i].RootElement), $"{kat.Id}[{i}]");
    }

    /// <summary>
    /// Verifies that every parse-only corpus vector parses without error.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedParseOnlyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenSupportedParseOnlyCorpusDocument_ShouldParse(YamlTestVector kat)
    {
        using var doc = YamlDocument.Parse(kat.Yaml);
        Assert.IsNotNull(doc);
    }

    /// <summary>
    /// Verifies that every supported-invalid corpus vector is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedFailCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenInvalidCorpusDocument_ShouldThrowYamlFormatException(YamlTestVector kat) =>
        AssertRejected(kat);

    /// <summary>
    /// Verifies that every profile-unsupported corpus vector is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(UnsupportedFeatureCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Parse_WhenUnsupportedFeatureCorpusDocument_ShouldThrowYamlFormatException(YamlTestVector kat) =>
        AssertRejected(kat);

    /// <summary>
    /// Asserts that a corpus vector is rejected with <see cref="YamlFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    private static void AssertRejected(YamlTestVector kat)
    {
        Assert.ThrowsExactly<YamlFormatException>(() =>
        {
            using var _ = YamlDocument.Parse(kat.Yaml);
        });
    }

    /// <summary>
    /// Loads the vectors in the requested classification category as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="category">The classification category to select.</param>
    /// <returns>One row per vector in the category.</returns>
    private static IEnumerable<object[]> VectorsIn(string category)
    {
        foreach (var vector in YamlTestCorpusReader.LoadByCategory(category))
            yield return [vector];
    }
}
