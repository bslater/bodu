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
/// Drives the hand-authored Bodu TOML 1.0.0 corpus — a focused supplement to the official <c>toml-test</c> suite —
/// as a table of <see cref="TomlV10Kat" /> known-answer vectors. Every valid vector must parse under the strict v1.0.0
/// default and match its expected value tree; every invalid vector (which includes documents using TOML 1.1.0 syntax
/// such as <c>\e</c>, <c>\xHH</c>, optional seconds, and multi-line / trailing-comma inline tables) must be rejected.
/// </summary>
[TestClass]
public sealed class TomlV10CorpusTests
{
    /// <summary>
    /// Provides the valid TOML 1.0.0 vectors, each carrying its expected tagged-JSON value tree.
    /// </summary>
    /// <returns>One <see cref="TomlV10Kat" /> per valid document.</returns>
    public static IEnumerable<object[]> ValidVectors() =>
        LoadVectors("bodu-toml10-valid.json", includeExpected: true);

    /// <summary>
    /// Provides the invalid TOML 1.0.0 vectors that a strict v1.0.0 parser must reject.
    /// </summary>
    /// <returns>One <see cref="TomlV10Kat" /> per invalid document.</returns>
    public static IEnumerable<object[]> InvalidVectors() =>
        LoadVectors("bodu-toml10-invalid.json", includeExpected: false);

#pragma warning disable CA1062 // Vectors are statically constructed by the data sources and are never null.

    /// <summary>
    /// Verifies that each valid TOML 1.0.0 vector parses under the strict v1.0.0 default profile and matches its
    /// expected tagged-JSON value tree.
    /// </summary>
    /// <param name="vector">The known-answer vector.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(ValidVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenValidVectorUnderV10_ShouldMatchExpectedModel(TomlV10Kat vector)
    {
        var document = Toml.Parse(vector.Toml);
        JsonNode actual = TomlTestEncoder.Encode(document);
        JsonNode expected = JsonNode.Parse(vector.Expected!)!;

        TaggedJson.AssertEquivalent(vector.Name, expected, actual, vector.Name);
    }

    /// <summary>
    /// Verifies that each invalid TOML 1.0.0 vector is rejected under the strict v1.0.0 default profile.
    /// </summary>
    /// <param name="vector">The known-answer vector.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(InvalidVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalidVectorUnderV10_ShouldThrowTomlFormatException(TomlV10Kat vector)
    {
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(vector.Toml), $"Vector '{vector.Name}' should have been rejected under strict TOML v1.0.0.");
    }

    /// <summary>
    /// Loads the known-answer vectors from the named embedded corpus resource.
    /// </summary>
    /// <param name="resourceSuffix">The trailing portion of the embedded resource name.</param>
    /// <param name="includeExpected">Whether the corpus carries an <c>expected</c> value tree.</param>
    /// <returns>The vectors, each wrapped as a single-element argument array.</returns>
    private static IEnumerable<object[]> LoadVectors(string resourceSuffix, bool includeExpected)
    {
        Assembly assembly = typeof(TomlV10CorpusTests).Assembly;
        var resourceName = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith(resourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded corpus resource '{resourceSuffix}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using JsonDocument document = JsonDocument.Parse(stream);

        var rows = new List<object[]>();
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            var name = element.GetProperty("name").GetString()!;
            var toml = element.GetProperty("toml").GetString()!;
            var expected = includeExpected ? element.GetProperty("expected").GetRawText() : null;
            rows.Add([new TomlV10Kat(name, toml, expected)]);
        }

        return rows;
    }

    /// <summary>
    /// A known-answer vector for the Bodu TOML 1.0.0 corpus: a source document, and (for valid documents) the expected
    /// value tree in toml-test's tagged-JSON encoding.
    /// </summary>
    /// <param name="Name">The vector name.</param>
    /// <param name="Toml">The TOML source document.</param>
    /// <param name="Expected">The expected tagged-JSON value tree, or <see langword="null" /> for invalid documents.</param>
    public sealed record TomlV10Kat(string Name, string Toml, string? Expected) : IKat;
}
