// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TaggedJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Nodes;

namespace Bodu.Text.Toml;

/// <summary>
/// Compares two trees in toml-test's tagged-JSON encoding for semantic equivalence — matching scalars by type with
/// value semantics (numeric floats, parsed date-times) rather than by raw text. Shared by every test that validates a
/// parsed document against an expected toml-test value tree.
/// </summary>
internal static class TaggedJson
{
    /// <summary>
    /// Asserts that two tagged-JSON trees are semantically equivalent.
    /// </summary>
    /// <param name="caseName">The conformance case name, for diagnostics.</param>
    /// <param name="expected">The expected node.</param>
    /// <param name="actual">The actual node.</param>
    /// <param name="path">The current node path, for diagnostics.</param>
    public static void AssertEquivalent(string caseName, JsonNode? expected, JsonNode? actual, string path)
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
                JsonObject actualTable = actual as JsonObject ?? throw Fail(caseName, path, "expected a table");
                Assert.AreEqual(expectedTable.Count, actualTable.Count, $"[{caseName}] at '{path}': table size differs.");
                foreach (KeyValuePair<string, JsonNode?> pair in expectedTable)
                {
                    Assert.IsTrue(actualTable.ContainsKey(pair.Key), $"[{caseName}] at '{path}': missing key '{pair.Key}'.");
                    AssertEquivalent(caseName, pair.Value, actualTable[pair.Key], path + "/" + pair.Key);
                }

                break;

            case JsonArray expectedArray:
                JsonArray actualArray = actual as JsonArray ?? throw Fail(caseName, path, "expected an array");
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
}
