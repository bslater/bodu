// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConformanceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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

        AssertEquivalent(testCase.Name, expected, actual, testCase.Name);
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

        AssertEquivalent(testCase.Name, expected, actual, testCase.Name);
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
    /// Asserts that two tagged-JSON trees are semantically equivalent, comparing scalars by type with value semantics
    /// (numeric floats, parsed date-times) rather than by raw text.
    /// </summary>
    /// <param name="caseName">The conformance case name, for diagnostics.</param>
    /// <param name="expected">The expected node.</param>
    /// <param name="actual">The actual node.</param>
    /// <param name="path">The current node path, for diagnostics.</param>
    private static void AssertEquivalent(string caseName, JsonNode? expected, JsonNode? actual, string path)
    {
        if (expected is JsonObject expectedObject && IsLeaf(expectedObject))
        {
            Assert.IsInstanceOfType<JsonObject>(actual, $"[{caseName}] at '{path}': expected a scalar.");
            AssertScalarEquivalent(caseName, expectedObject, (JsonObject)actual!, path);
            return;
        }

        switch (expected)
        {
            case JsonObject expectedTable:
                var actualTable = actual as JsonObject ?? throw Fail(caseName, path, "expected a table");
                Assert.AreEqual(expectedTable.Count, actualTable.Count, $"[{caseName}] at '{path}': table size differs.");
                foreach (var pair in expectedTable)
                {
                    Assert.IsTrue(actualTable.ContainsKey(pair.Key), $"[{caseName}] at '{path}': missing key '{pair.Key}'.");
                    AssertEquivalent(caseName, pair.Value, actualTable[pair.Key], path + "/" + pair.Key);
                }

                break;

            case JsonArray expectedArray:
                var actualArray = actual as JsonArray ?? throw Fail(caseName, path, "expected an array");
                Assert.AreEqual(expectedArray.Count, actualArray.Count, $"[{caseName}] at '{path}': array length differs.");
                for (var i = 0; i < expectedArray.Count; i++)
                    AssertEquivalent(caseName, expectedArray[i], actualArray[i], path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]");
                break;

            default:
                throw Fail(caseName, path, "unexpected expected-node shape");
        }
    }

    /// <summary>
    /// Compares two tagged-JSON scalar leaves by type and value semantics.
    /// </summary>
    /// <param name="caseName">The conformance case name, for diagnostics.</param>
    /// <param name="expected">The expected leaf.</param>
    /// <param name="actual">The actual leaf.</param>
    /// <param name="path">The current node path, for diagnostics.</param>
    private static void AssertScalarEquivalent(string caseName, JsonObject expected, JsonObject actual, string path)
    {
        var expectedType = expected["type"]!.GetValue<string>();
        var actualType = actual["type"]?.GetValue<string>();
        Assert.AreEqual(expectedType, actualType, $"[{caseName}] at '{path}': value type differs.");

        var expectedValue = expected["value"]!.GetValue<string>();
        var actualValue = actual["value"]!.GetValue<string>();

        switch (expectedType)
        {
            case "integer":
                Assert.AreEqual(long.Parse(expectedValue, CultureInfo.InvariantCulture), long.Parse(actualValue, CultureInfo.InvariantCulture), $"[{caseName}] at '{path}'.");
                break;
            case "float":
                AssertFloatEquivalent(caseName, expectedValue, actualValue, path);
                break;
            case "datetime":
                Assert.AreEqual(DateTimeOffset.Parse(expectedValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeOffset.Parse(actualValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), $"[{caseName}] at '{path}'.");
                break;
            case "datetime-local":
                Assert.AreEqual(DateTime.Parse(expectedValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTime.Parse(actualValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), $"[{caseName}] at '{path}'.");
                break;
            case "date-local":
                Assert.AreEqual(DateOnly.Parse(expectedValue, CultureInfo.InvariantCulture), DateOnly.Parse(actualValue, CultureInfo.InvariantCulture), $"[{caseName}] at '{path}'.");
                break;
            case "time-local":
                Assert.AreEqual(TimeOnly.Parse(expectedValue, CultureInfo.InvariantCulture), TimeOnly.Parse(actualValue, CultureInfo.InvariantCulture), $"[{caseName}] at '{path}'.");
                break;
            default:
                Assert.AreEqual(expectedValue, actualValue, $"[{caseName}] at '{path}'.");
                break;
        }
    }

    /// <summary>
    /// Compares two float string forms numerically, treating <c>nan</c> as equal to <c>nan</c>.
    /// </summary>
    /// <param name="caseName">The conformance case name, for diagnostics.</param>
    /// <param name="expectedValue">The expected float string.</param>
    /// <param name="actualValue">The actual float string.</param>
    /// <param name="path">The current node path, for diagnostics.</param>
    private static void AssertFloatEquivalent(string caseName, string expectedValue, string actualValue, string path)
    {
        var expected = ParseFloat(expectedValue);
        var actual = ParseFloat(actualValue);

        if (double.IsNaN(expected))
        {
            Assert.IsTrue(double.IsNaN(actual), $"[{caseName}] at '{path}': expected nan.");
            return;
        }

        Assert.AreEqual(expected, actual, $"[{caseName}] at '{path}'.");
    }

    /// <summary>
    /// Parses a toml-test float string, including the special values.
    /// </summary>
    /// <param name="value">The float string.</param>
    /// <returns>The parsed double.</returns>
    private static double ParseFloat(string value) =>
        value switch
        {
            "inf" or "+inf" => double.PositiveInfinity,
            "-inf" => double.NegativeInfinity,
            "nan" or "+nan" or "-nan" => double.NaN,
            _ => double.Parse(value, CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Determines whether <paramref name="node" /> is a tagged-JSON scalar leaf (exactly <c>type</c> and <c>value</c>,
    /// both strings).
    /// </summary>
    /// <param name="node">The candidate node.</param>
    /// <returns><see langword="true" /> when the node is a scalar leaf.</returns>
    private static bool IsLeaf(JsonObject node) =>
        node.Count == 2 && node["type"] is JsonValue typeValue && typeValue.TryGetValue<string>(out _)
        && node["value"] is JsonValue dataValue && dataValue.TryGetValue<string>(out _);

    /// <summary>
    /// Builds a failure exception for a structural mismatch.
    /// </summary>
    /// <param name="caseName">The conformance case name.</param>
    /// <param name="path">The node path.</param>
    /// <param name="reason">The mismatch reason.</param>
    /// <returns>An assertion failure.</returns>
    private static Exception Fail(string caseName, string path, string reason)
    {
        Assert.Fail($"[{caseName}] at '{path}': {reason}.");
        return new InvalidOperationException();
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
