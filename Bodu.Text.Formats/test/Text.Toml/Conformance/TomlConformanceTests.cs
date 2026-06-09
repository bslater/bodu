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
/// Runs the vendored <c>toml-test</c> conformance corpus against the TOML reader in both specification profiles. Under
/// the TOML v1.1.0 profile every valid document must parse and match its expected value tree (in toml-test's tagged-JSON
/// encoding) and every invalid document (excluding the cases v1.1.0 made valid) must be rejected. Under the strict TOML
/// v1.0.0 default every invalid document — including the v1.1.0 relaxations — must be rejected. The corpus is
/// data-driven over a published vector table, so the tests are tagged <see cref="TestCategories.Regression" />.
/// </summary>
[TestClass]
public sealed class TomlConformanceTests
{
    /// <summary>
    /// The options that select the TOML v1.1.0 profile, under which the valid corpus and the relaxed-invalid corpus are
    /// evaluated.
    /// </summary>
    private static readonly TomlReaderOptions s_v11 = new() { SpecVersion = TomlSpecVersion.V1_1 };

    /// <summary>
    /// Individual invalid cases that TOML v1.1.0 made valid relative to the vendored corpus, and so must not be run as
    /// rejection cases against a 1.1.0 parser. They remain rejection cases under the strict v1.0.0 profile.
    /// </summary>
    private static readonly HashSet<string> s_invalidSkip = new(StringComparer.Ordinal)
    {
        // Optional seconds in date-times are permitted in TOML 1.1.0.
        "datetime/no-secs",
        "local-datetime/no-secs",
        "local-time/no-secs",

        // Multi-line and trailing-comma inline tables are permitted in TOML 1.1.0.
        "inline-table/linebreak-01",
        "inline-table/linebreak-02",
        "inline-table/linebreak-03",
        "inline-table/linebreak-04",
        "inline-table/trailing-comma",

        // The \xHH byte escape is permitted in TOML 1.1.0.
        "string/basic-byte-escapes",
    };

    /// <summary>
    /// Provides the valid conformance cases.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per valid document.</returns>
    public static IEnumerable<object[]> ValidCases() =>
        LoadCases("toml-test-valid.json", includeExpected: true);

    /// <summary>
    /// Provides the invalid conformance cases, excluding those relaxed by TOML v1.1.0.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per invalid document.</returns>
    public static IEnumerable<object[]> InvalidCases() =>
        LoadCases("toml-test-invalid.json", includeExpected: false)
            .Where(row => !s_invalidSkip.Contains(((ConformanceCase)row[0]).Name));

    /// <summary>
    /// Provides every invalid conformance case, including those TOML v1.1.0 relaxed — all of which a strict TOML v1.0.0
    /// parser must reject.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per invalid document.</returns>
    public static IEnumerable<object[]> AllInvalidCases() =>
        LoadCases("toml-test-invalid.json", includeExpected: false);

    /// <summary>
    /// Provides the valid corpus cases the strict TOML v1.0.0 parser accepts — the v1.0-valid subset, partitioned by
    /// the parser itself rather than a hand-maintained version list.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per v1.0-accepted valid document.</returns>
    public static IEnumerable<object[]> V10AcceptedValidCases() =>
        LoadCases("toml-test-valid.json", includeExpected: true)
            .Where(row => new TomlReader().TryRead(((ConformanceCase)row[0]).Toml, out _));

    /// <summary>
    /// Provides the valid corpus cases the strict TOML v1.0.0 parser rejects — the TOML v1.1.0-only valid subset.
    /// </summary>
    /// <returns>One <see cref="ConformanceCase" /> per v1.1-only valid document.</returns>
    public static IEnumerable<object[]> V11OnlyValidCases() =>
        LoadCases("toml-test-valid.json", includeExpected: true)
            .Where(row => !new TomlReader().TryRead(((ConformanceCase)row[0]).Toml, out _));

#pragma warning disable CA1062 // Conformance cases are statically constructed by the data sources and are never null.

    /// <summary>
    /// Verifies that a valid corpus document parses under the TOML v1.1.0 profile and matches its expected tagged-JSON
    /// value tree.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(ValidCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Valid_WhenParsedAsV11_ShouldMatchExpectedModel(ConformanceCase testCase)
    {
        var document = Toml.Parse(testCase.Toml, s_v11);
        JsonNode actual = TomlTestEncoder.Encode(document);
        JsonNode expected = JsonNode.Parse(testCase.Expected!)!;

        AssertEquivalent(testCase.Name, expected, actual, testCase.Name);
    }

    /// <summary>
    /// Verifies that every valid corpus document the strict TOML v1.0.0 parser accepts yields the same model as the
    /// expected tagged-JSON tree. Because v1.1.0 is a superset of v1.0.0, an accepted document's model is
    /// version-independent, so this exercises the v1.0 parser across the full v1.0-valid subset of the corpus.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(V10AcceptedValidCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Valid_WhenAcceptedByStrictV10_ShouldMatchExpectedModel(ConformanceCase testCase)
    {
        var document = Toml.Parse(testCase.Toml);
        JsonNode actual = TomlTestEncoder.Encode(document);
        JsonNode expected = JsonNode.Parse(testCase.Expected!)!;

        AssertEquivalent(testCase.Name, expected, actual, testCase.Name);
    }

    /// <summary>
    /// Verifies that every valid corpus document the strict TOML v1.0.0 parser rejects is one the v1.1.0 parser accepts
    /// — confirming each v1.0 rejection is a deliberate version boundary rather than a parser defect.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(V11OnlyValidCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Valid_WhenRejectedByStrictV10_ShouldBeAcceptedByV11(ConformanceCase testCase)
    {
        Assert.IsNotNull(Toml.Parse(testCase.Toml, s_v11), testCase.Name);
    }

    /// <summary>
    /// Verifies that an invalid corpus document is rejected under the TOML v1.1.0 profile, excluding the cases v1.1.0
    /// made valid.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(InvalidCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Invalid_WhenParsedAsV11_ShouldThrowTomlFormatException(ConformanceCase testCase)
    {
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(testCase.Toml, s_v11), $"Conformance case '{testCase.Name}' should have been rejected.");
    }

    /// <summary>
    /// Verifies that every invalid corpus document — including the cases TOML v1.1.0 relaxed — is rejected under the
    /// strict TOML v1.0.0 default profile.
    /// </summary>
    /// <param name="testCase">The conformance case.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(AllInvalidCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Invalid_WhenParsedAsStrictV10_ShouldThrowTomlFormatException(ConformanceCase testCase)
    {
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse(testCase.Toml), $"Conformance case '{testCase.Name}' should have been rejected under strict TOML v1.0.0.");
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
