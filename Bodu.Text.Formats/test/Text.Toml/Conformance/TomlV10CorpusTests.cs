// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlV10CorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Text.Toml;

/// <summary>
/// Drives the hand-authored Bodu TOML 1.0.0 corpus — a focused supplement to the official <c>toml-test</c> suite — as
/// tables of known-answer vectors. Every valid <see cref="TomlV10ValidKat" /> must parse under the strict v1.0.0 default
/// and match its expected value tree; every invalid <see cref="TomlV10InvalidKat" /> (which includes documents using
/// TOML 1.1.0 syntax such as <c>\e</c>, <c>\xHH</c>, optional seconds, and multi-line / trailing-comma inline tables)
/// must be rejected, and each carries the v1.0.0 rule it violates.
/// </summary>
[TestClass]
public sealed class TomlV10CorpusTests
{
    /// <summary>
    /// Provides the valid TOML 1.0.0 vectors, each carrying its expected tagged-JSON value tree.
    /// </summary>
    /// <returns>One <see cref="TomlV10ValidKat" /> per valid document.</returns>
    public static IEnumerable<object[]> ValidVectors()
    {
        using JsonDocument document = LoadCorpus("bodu-toml10-valid.json");
        var rows = new List<object[]>();
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            rows.Add([new TomlV10ValidKat(
                element.GetProperty("name").GetString()!,
                element.GetProperty("toml").GetString()!,
                element.GetProperty("expected").GetRawText())]);
        }

        return rows;
    }

    /// <summary>
    /// Provides the invalid TOML 1.0.0 vectors a strict v1.0.0 parser must reject, each carrying the rule it violates.
    /// </summary>
    /// <returns>One <see cref="TomlV10InvalidKat" /> per invalid document.</returns>
    public static IEnumerable<object[]> InvalidVectors()
    {
        using JsonDocument document = LoadCorpus("bodu-toml10-invalid.json");
        var rows = new List<object[]>();
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            rows.Add([new TomlV10InvalidKat(
                element.GetProperty("name").GetString()!,
                element.GetProperty("toml").GetString()!,
                element.GetProperty("rule").GetString()!)]);
        }

        return rows;
    }

#pragma warning disable CA1062 // Vectors are statically constructed by the data sources and are never null.

    /// <summary>
    /// Verifies that each valid TOML 1.0.0 vector parses under the strict v1.0.0 default profile and matches its
    /// expected tagged-JSON value tree.
    /// </summary>
    /// <param name="vector">The known-answer vector.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(ValidVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenValidVectorUnderV10_ShouldMatchExpectedModel(TomlV10ValidKat vector)
    {
        TomlTable document = Toml.Parse(vector.Toml);
        JsonNode actual = TomlTestEncoder.Encode(document);
        JsonNode expected = JsonNode.Parse(vector.Expected)!;

        TaggedJson.AssertEquivalent(vector.Name, expected, actual, vector.Name);
    }

    /// <summary>
    /// Verifies that each invalid TOML 1.0.0 vector is rejected under the strict v1.0.0 default profile, citing the rule
    /// it violates when it is not.
    /// </summary>
    /// <param name="vector">The known-answer vector.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(InvalidVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalidVectorUnderV10_ShouldThrowTomlFormatException(TomlV10InvalidKat vector)
    {
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(vector.Toml), $"Vector '{vector.Name}' should have been rejected under strict TOML v1.0.0: {vector.Rule}");
    }

    /// <summary>
    /// Opens the named embedded corpus resource as a <see cref="JsonDocument" />.
    /// </summary>
    /// <param name="resourceSuffix">The trailing portion of the embedded resource name.</param>
    /// <returns>The parsed corpus document, which the caller disposes.</returns>
    private static JsonDocument LoadCorpus(string resourceSuffix)
    {
        Assembly assembly = typeof(TomlV10CorpusTests).Assembly;
        var resourceName = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith(resourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded corpus resource '{resourceSuffix}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        return JsonDocument.Parse(stream);
    }

    /// <summary>
    /// A known-answer vector for a valid Bodu TOML 1.0.0 document: a source document and its expected value tree in
    /// toml-test's tagged-JSON encoding.
    /// </summary>
    /// <param name="Name">The vector name.</param>
    /// <param name="Toml">The TOML source document.</param>
    /// <param name="Expected">The expected tagged-JSON value tree.</param>
    public sealed record TomlV10ValidKat(string Name, string Toml, string Expected) : IKat;

    /// <summary>
    /// A known-answer vector for an invalid Bodu TOML 1.0.0 document: a source document and the strict v1.0.0 rule it
    /// violates.
    /// </summary>
    /// <param name="Name">The vector name.</param>
    /// <param name="Toml">The TOML source document.</param>
    /// <param name="Rule">The TOML 1.0.0 rule the document violates.</param>
    public sealed record TomlV10InvalidKat(string Name, string Toml, string Rule) : IKat;
}
