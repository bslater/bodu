// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlTestCorpusTests.Events.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Yaml.Reader;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that the <see cref="Utf8YamlReader" /> token stream matches the structural shape of each supported
/// vector's upstream <c>test.event</c> file — the canonical event sequence produced by a conforming YAML parser.
/// </summary>
/// <remarks>
/// The comparison is structural: container nesting and scalar positions are compared, while scalar text, style,
/// anchors, and tags are not (the tree profile resolves typed scalars and does not surface anchors or tags as events).
/// Vectors that the profile resolves differently from the event model — those using aliases (resolved transparently)
/// or spanning multiple documents — are outside the comparable subset and are skipped; the coverage test keeps that
/// subset from silently shrinking.
/// </remarks>
public partial class YamlTestCorpusTests
{
    /// <summary>
    /// Verifies that a comparable supported vector's reader token shape equals its <c>test.event</c> shape.
    /// </summary>
    /// <param name="kat">The corpus vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(SupportedPassCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Events_WhenSupportedValidCase_ShouldMatchEventShape(YamlTestVector kat)
    {
        if (!TryGetComparableEventShape(kat, out List<string>? expectedShape))
            return;

        List<string> actualShape = ReaderShape(kat.Yaml);
        CollectionAssert.AreEqual(
            expectedShape,
            actualShape,
            $"{kat.Id}: reader token shape does not match test.event.\n  expected: {string.Join(' ', expectedShape)}\n  actual:   {string.Join(' ', actualShape)}");
    }

    /// <summary>
    /// Verifies that the event-comparable subset of supported vectors stays broad, so a regression cannot silently
    /// shrink the structural event coverage to nothing.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Events_ComparableSubset_ShouldCoverMostSupportedCases()
    {
        int comparable = YamlTestCorpusReader.LoadByCategory("SupportedPass")
            .Count(kat => TryGetComparableEventShape(kat, out _));

        Assert.IsTrue(
            comparable >= 190,
            $"Only {comparable} supported vectors are event-comparable; the structural event coverage dropped unexpectedly.");
    }

    /// <summary>
    /// Attempts to read a vector's <c>test.event</c> shape, returning <see langword="false" /> when the vector is
    /// outside the comparable subset (an alias-bearing, multi-document, or empty-document case).
    /// </summary>
    /// <param name="kat">The vector to inspect.</param>
    /// <param name="shape">When comparable, the normalized event shape.</param>
    /// <returns><see langword="true" /> when the vector is event-comparable.</returns>
    private static bool TryGetComparableEventShape(YamlTestVector kat, out List<string> shape)
    {
        shape = [];

        string eventPath = Path.Combine(YamlTestCorpusReader.VectorDirectory(kat.Id), "test.event");
        if (!File.Exists(eventPath))
            return false;

        // An empty JSON expectation denotes an empty (null-root) document, which has no body events to compare.
        if (kat.Json is null || Encoding.UTF8.GetString(kat.Json).Trim().Length == 0)
            return false;

        bool hasAlias = false;
        int documentCount = 0;
        foreach (string raw in File.ReadLines(eventPath))
        {
            ReadOnlySpan<char> line = raw.AsSpan().Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("+DOC"))
            {
                documentCount++;
            }
            else if (line.StartsWith("+STR") || line.StartsWith("-STR") || line.StartsWith("-DOC"))
            {
                // Stream and document framing is not surfaced by the buffered single-document reader.
            }
            else if (line.StartsWith("+MAP"))
            {
                shape.Add("MAP{");
            }
            else if (line.StartsWith("-MAP"))
            {
                shape.Add("}MAP");
            }
            else if (line.StartsWith("+SEQ"))
            {
                shape.Add("SEQ[");
            }
            else if (line.StartsWith("-SEQ"))
            {
                shape.Add("]SEQ");
            }
            else if (line.StartsWith("=VAL"))
            {
                shape.Add("VAL");
            }
            else if (line.StartsWith("=ALI"))
            {
                hasAlias = true;
            }
        }

        // Aliases are resolved transparently and multi-document streams are read one document at a time, so neither is
        // structurally comparable through the single-document reader.
        if (hasAlias || documentCount > 1)
        {
            shape = [];
            return false;
        }

        return true;
    }

    /// <summary>
    /// Projects the reader's token stream over a document to the same structural shape vocabulary as the event file.
    /// </summary>
    /// <param name="yaml">The UTF-8 YAML input.</param>
    /// <returns>The structural shape tokens.</returns>
    private static List<string> ReaderShape(byte[] yaml)
    {
        var reader = new Utf8YamlReader(yaml);
        var shape = new List<string>();
        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case YamlTokenType.StartMapping:
                    shape.Add("MAP{");
                    break;

                case YamlTokenType.EndMapping:
                    shape.Add("}MAP");
                    break;

                case YamlTokenType.StartSequence:
                    shape.Add("SEQ[");
                    break;

                case YamlTokenType.EndSequence:
                    shape.Add("]SEQ");
                    break;

                case YamlTokenType.PropertyName:
                case YamlTokenType.String:
                case YamlTokenType.Integer:
                case YamlTokenType.Float:
                case YamlTokenType.Boolean:
                case YamlTokenType.Null:
                    shape.Add("VAL");
                    break;
            }
        }

        return shape;
    }
}
