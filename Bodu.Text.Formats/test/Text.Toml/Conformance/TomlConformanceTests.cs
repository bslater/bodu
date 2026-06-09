// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConformanceTests.cs" company="Bodu Pty. Ltd.">
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
/// Runs the vendored <c>toml-test</c> conformance corpus against the TOML reader, gating each specification version
/// against its own authoritative <c>files-toml-&lt;version&gt;</c> manifest from toml-test. A parser at version X must
/// accept every valid document the X manifest lists and reject every invalid document it lists; the per-version split is
/// taken from those manifests, not inferred. The corpus is data-driven over a published vector table, so the tests are
/// tagged <see cref="TestCategories.Regression" />.
/// </summary>
[TestClass]
public sealed class TomlConformanceTests
{
    /// <summary>
    /// The options that select the TOML v1.1.0 profile.
    /// </summary>
    private static readonly TomlReaderOptions s_v11 = new() { SpecVersion = TomlSpecVersion.V1_1 };

    /// <summary>
    /// The TOML v1.0.0 suite membership (valid and invalid case names) from <c>files-toml-1.0.0</c>.
    /// </summary>
    private static readonly (HashSet<string> Valid, HashSet<string> Invalid) s_manifestV10 = LoadManifest("toml-test-files-1.0.0.txt");

    /// <summary>
    /// The TOML v1.1.0 suite membership (valid and invalid case names) from <c>files-toml-1.1.0</c>.
    /// </summary>
    private static readonly (HashSet<string> Valid, HashSet<string> Invalid) s_manifestV11 = LoadManifest("toml-test-files-1.1.0.txt");

    /// <summary>
    /// Provides every valid conformance case. TOML v1.1.0 is a superset of v1.0.0, so the v1.1.0 parser must accept all
    /// of them.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per valid document.</returns>
    public static IEnumerable<object[]> ValidCases() =>
        LoadCases("toml-test-valid.json", includeExpected: true);

    /// <summary>
    /// Provides the valid cases that belong to the TOML v1.0.0 suite — the documents a strict v1.0.0 parser must accept.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per v1.0.0-valid document.</returns>
    public static IEnumerable<object[]> ValidCasesV10() =>
        ValidCases().Where(row => s_manifestV10.Valid.Contains(((ConformanceCase)row[0]).Name));

    /// <summary>
    /// Provides the invalid cases that belong to the TOML v1.0.0 suite — the documents a strict v1.0.0 parser must
    /// reject.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per v1.0.0-invalid document.</returns>
    public static IEnumerable<object[]> InvalidCasesV10() =>
        LoadCases("toml-test-invalid.json", includeExpected: false)
            .Where(row => s_manifestV10.Invalid.Contains(((ConformanceCase)row[0]).Name));

    /// <summary>
    /// Provides the invalid cases that belong to the TOML v1.1.0 suite — the documents a v1.1.0 parser must reject (the
    /// cases v1.1.0 relaxed to valid are excluded by the manifest).
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per v1.1.0-invalid document.</returns>
    public static IEnumerable<object[]> InvalidCasesV11() =>
        LoadCases("toml-test-invalid.json", includeExpected: false)
            .Where(row => s_manifestV11.Invalid.Contains(((ConformanceCase)row[0]).Name));

#pragma warning disable CA1062 // Conformance cases are statically constructed by the data sources and are never null.

    /// <summary>
    /// Verifies that every valid corpus document parses under the TOML v1.1.0 profile and matches its expected
    /// tagged-JSON value tree.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(ValidCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenValidCorpusUnderV11_ShouldMatchExpectedModel(ConformanceCase testCase)
    {
        var document = Toml.Parse(testCase.Toml, s_v11);
        JsonNode actual = TomlTestEncoder.Encode(document);
        JsonNode expected = JsonNode.Parse(testCase.Expected!)!;

        TaggedJson.AssertEquivalent(testCase.Name, expected, actual, testCase.Name);
    }

    /// <summary>
    /// Verifies that every document in the TOML v1.0.0 valid suite parses under the strict v1.0.0 default profile and
    /// matches its expected tagged-JSON value tree.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(ValidCasesV10), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenValidCorpusUnderV10_ShouldMatchExpectedModel(ConformanceCase testCase)
    {
        var document = Toml.Parse(testCase.Toml);
        JsonNode actual = TomlTestEncoder.Encode(document);
        JsonNode expected = JsonNode.Parse(testCase.Expected!)!;

        TaggedJson.AssertEquivalent(testCase.Name, expected, actual, testCase.Name);
    }

    /// <summary>
    /// Verifies that every document in the TOML v1.1.0 invalid suite is rejected under the v1.1.0 profile.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(InvalidCasesV11), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalidCorpusUnderV11_ShouldThrowTomlFormatException(ConformanceCase testCase)
    {
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(testCase.Toml, s_v11), $"Conformance case '{testCase.Name}' should have been rejected under TOML v1.1.0.");
    }

    /// <summary>
    /// Verifies that every document in the TOML v1.0.0 invalid suite is rejected under the strict v1.0.0 default profile.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(InvalidCasesV10), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalidCorpusUnderV10_ShouldThrowTomlFormatException(ConformanceCase testCase)
    {
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(testCase.Toml), $"Conformance case '{testCase.Name}' should have been rejected under strict TOML v1.0.0.");
    }

    /// <summary>
    /// Verifies that every vendored corpus case is classified by at least one version manifest, so no document is
    /// silently excluded from both per-version suites when the corpus or manifests are re-vendored.
    /// </summary>
    [TestMethod]
    public void Manifest_WhenLoaded_ShouldClassifyEveryCorpusCase()
    {
        foreach (var row in ValidCases())
        {
            var name = ((ConformanceCase)row[0]).Name;
            Assert.IsTrue(
                s_manifestV10.Valid.Contains(name) || s_manifestV11.Valid.Contains(name),
                $"Valid corpus case '{name}' is not listed in any toml-test version manifest.");
        }

        foreach (var row in LoadCases("toml-test-invalid.json", includeExpected: false))
        {
            var name = ((ConformanceCase)row[0]).Name;
            Assert.IsTrue(
                s_manifestV10.Invalid.Contains(name) || s_manifestV11.Invalid.Contains(name),
                $"Invalid corpus case '{name}' is not listed in any toml-test version manifest.");
        }
    }

    /// <summary>
    /// Loads a vendored <c>files-toml-&lt;version&gt;</c> manifest into the set of valid and invalid case names it
    /// lists, keyed the same way as the corpus (the path under <c>valid/</c> or <c>invalid/</c> without the
    /// <c>.toml</c> extension). The accompanying <c>.json</c> expected-value entries are ignored.
    /// </summary>
    /// <param name="resourceSuffix">The trailing portion of the embedded manifest resource name.</param>
    /// <returns>The valid and invalid case-name sets.</returns>
    private static (HashSet<string> Valid, HashSet<string> Invalid) LoadManifest(string resourceSuffix)
    {
        var valid = new HashSet<string>(StringComparer.Ordinal);
        var invalid = new HashSet<string>(StringComparer.Ordinal);

        Assembly assembly = typeof(TomlConformanceTests).Assembly;
        var resourceName = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith(resourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded manifest resource '{resourceSuffix}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (!line.EndsWith(".toml", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("valid/", StringComparison.Ordinal))
                valid.Add(line["valid/".Length..^".toml".Length]);
            else if (line.StartsWith("invalid/", StringComparison.Ordinal))
                invalid.Add(line["invalid/".Length..^".toml".Length]);
        }

        return (valid, invalid);
    }

    /// <summary>
    /// Loads conformance cases from the named embedded corpus resource.
    /// </summary>
    /// <param name="resourceSuffix">The trailing portion of the embedded resource name.</param>
    /// <param name="includeExpected">Whether the corpus carries an <c>expected</c> value tree.</param>
    /// <returns>The cases, each wrapped as a single-element argument array.</returns>
    private static IEnumerable<object[]> LoadCases(string resourceSuffix, bool includeExpected)
    {
        Assembly assembly = typeof(TomlConformanceTests).Assembly;
        var resourceName = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith(resourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded conformance resource '{resourceSuffix}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using JsonDocument document = JsonDocument.Parse(stream);

        var rows = new List<object[]>();
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            var name = element.GetProperty("name").GetString()!;
            var toml = element.GetProperty("toml").GetString()!;
            var expected = includeExpected ? element.GetProperty("expected").GetRawText() : null;
            rows.Add([new ConformanceCase(name, toml, expected)]);
        }

        return rows;
    }

    /// <summary>
    /// Represents a single conformance case: a TOML document, its name, and (for valid cases) the expected tagged-JSON
    /// value tree.
    /// </summary>
    /// <param name="Name">The case name from the corpus.</param>
    /// <param name="Toml">The TOML document.</param>
    /// <param name="Expected">The expected tagged-JSON value tree, or <see langword="null" /> for invalid cases.</param>
    public sealed record ConformanceCase(string Name, string Toml, string? Expected) : IKat;
}
