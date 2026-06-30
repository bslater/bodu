// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using Bodu.Test.Kat;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that the writer emits parse-equivalent YAML for the supported corpus: a single-document supported-pass
/// case re-serialized through <see cref="YamlSerializer" /> and reparsed still matches its JSON expectation.
/// </summary>
public partial class YamlTestCorpusTests
{
    /// <summary>
    /// Verifies that re-serializing a supported-valid single-document case and reparsing it preserves its value.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedPassCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void RoundTrip_WhenSupportedValidSingleDocument_ShouldRemainEquivalent(YamlTestVector kat)
    {
        // The writer round-trip is exercised over single-document cases with a value expectation; multi-document and
        // empty-document cases are validated by the parse-only and parse paths.
        if (Encoding.UTF8.GetString(kat.Json!).Trim().Length == 0)
            return;

        List<JsonElement> expectedValues = CorpusCompare.SplitJsonValues(kat.Json!);
        if (expectedValues.Count != 1)
            return;

        object? value = YamlSerializer.Deserialize<object>(kat.Yaml);
        string reemitted = YamlSerializer.Serialize(value);

        using var doc = YamlDocument.Parse(reemitted);
        Assert.IsTrue(CorpusCompare.Matches(expectedValues[0], doc.RootElement), $"{kat.Id}\n--- re-emitted ---\n{reemitted}");
    }

    /// <summary>
    /// Verifies that the forward-only <see cref="Utf8YamlReader" /> reads every token of a supported-valid case to
    /// completion, exercising the reader surface independently of the document object model.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedPassCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Reader_WhenSupportedValidCase_ShouldReadAllTokens(YamlTestVector kat)
    {
        var reader = new Utf8YamlReader(kat.Yaml);
        int depth = 0;
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case YamlTokenType.StartMapping:
                case YamlTokenType.StartSequence:
                    depth++;
                    break;

                case YamlTokenType.EndMapping:
                case YamlTokenType.EndSequence:
                    depth--;
                    break;
            }
        }

        Assert.AreEqual(0, depth, $"{kat.Id}: unbalanced container tokens.");
    }

    /// <summary>
    /// Verifies that a parse-only case (one without a JSON expectation) round-trips to a stable value graph, giving it
    /// a semantic check stronger than merely parsing without error.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedParseOnlyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void RoundTrip_WhenParseOnlyCase_ShouldProduceStableGraph(YamlTestVector kat)
    {
        object? first = YamlSerializer.Deserialize<object>(kat.Yaml);
        string reemitted = YamlSerializer.Serialize(first);
        object? second = YamlSerializer.Deserialize<object>(reemitted);

        Assert.AreEqual(
            JsonSerializer.Serialize(first),
            JsonSerializer.Serialize(second),
            $"{kat.Id}\n--- re-emitted ---\n{reemitted}");
    }
}
