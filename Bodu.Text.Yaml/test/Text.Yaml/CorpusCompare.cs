// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusCompare.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Compares a parsed <see cref="YamlElement" /> against the canonical JSON expectation published by the
/// <c>yaml-test-suite</c>, applying the JSON-compatible value mapping of the library's YAML profile.
/// </summary>
internal static class CorpusCompare
{
    /// <summary>
    /// Determines whether a parsed YAML element matches its JSON expectation.
    /// </summary>
    /// <param name="expected">The JSON expectation element.</param>
    /// <param name="actual">The parsed YAML element.</param>
    /// <returns><see langword="true" /> when the two represent the same data.</returns>
    public static bool Matches(JsonElement expected, YamlElement actual)
    {
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                return MatchesObject(expected, actual);

            case JsonValueKind.Array:
                return MatchesArray(expected, actual);

            case JsonValueKind.String:
                return actual.ValueKind == YamlValueKind.String
                    && string.Equals(actual.GetString(), expected.GetString(), StringComparison.Ordinal);

            case JsonValueKind.Number:
                return MatchesNumber(expected, actual);

            case JsonValueKind.True:
            case JsonValueKind.False:
                return actual.ValueKind == YamlValueKind.Boolean && actual.GetBoolean() == expected.GetBoolean();

            case JsonValueKind.Null:
                return actual.ValueKind == YamlValueKind.Null;

            default:
                return false;
        }
    }

    private static bool MatchesObject(JsonElement expected, YamlElement actual)
    {
        if (actual.ValueKind != YamlValueKind.Mapping)
            return false;

        var entries = new Dictionary<string, YamlElement>(StringComparer.Ordinal);
        foreach (YamlProperty property in actual.EnumerateMapping())
            entries[property.Name] = property.Value;

        int expectedCount = 0;
        foreach (JsonProperty property in expected.EnumerateObject())
        {
            expectedCount++;
            if (!entries.TryGetValue(property.Name, out YamlElement child) || !Matches(property.Value, child))
                return false;
        }

        return expectedCount == entries.Count;
    }

    private static bool MatchesArray(JsonElement expected, YamlElement actual)
    {
        if (actual.ValueKind != YamlValueKind.Sequence)
            return false;

        int index = 0;
        int length = actual.GetSequenceLength();
        foreach (JsonElement element in expected.EnumerateArray())
        {
            if (index >= length || !Matches(element, actual[index]))
                return false;

            index++;
        }

        return index == length;
    }

    private static bool MatchesNumber(JsonElement expected, YamlElement actual)
    {
        // Compare by numeric category instead of forcing every value through double, which would mask differences for
        // integers above 2^53 and other precision-sensitive cases. A YAML integer is compared exactly as a 64-bit
        // integer; only genuine floating-point values (and integers too large for the profile's Int64 store, which
        // resolve as floats) fall back to a double comparison.
        switch (actual.ValueKind)
        {
            case YamlValueKind.Integer:
                return expected.TryGetInt64(out long expectedInt) && actual.GetInt64() == expectedInt;

            case YamlValueKind.Float:
                return expected.TryGetDouble(out double expectedDouble) && NumbersEqual(actual.GetDouble(), expectedDouble);

            default:
                return false;
        }
    }

    private static bool NumbersEqual(double a, double b) =>
        a == b || (double.IsNaN(a) && double.IsNaN(b)) ||
        string.Equals(a.ToString("R", CultureInfo.InvariantCulture), b.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal);

    /// <summary>
    /// Splits a buffer that may contain several concatenated top-level JSON values into one detached element per value.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 JSON buffer.</param>
    /// <returns>The top-level values, in order.</returns>
    public static List<JsonElement> SplitJsonValues(byte[] utf8Json)
    {
        var result = new List<JsonElement>();
        var remaining = new ReadOnlySpan<byte>(utf8Json);

        while (true)
        {
            int skip = 0;
            while (skip < remaining.Length && remaining[skip] is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r')
                skip++;

            remaining = remaining[skip..];
            if (remaining.IsEmpty)
                break;

            int consumed = MeasureFirstValue(remaining);

            using var document = JsonDocument.Parse(remaining[..consumed].ToArray());
            result.Add(document.RootElement.Clone());
            remaining = remaining[consumed..];
        }

        return result;
    }

    /// <summary>
    /// Measures the byte length of the first complete JSON value at the start of a buffer that may hold more values.
    /// </summary>
    /// <param name="buffer">The buffer positioned at the first value's first byte.</param>
    /// <returns>The number of bytes the first value occupies.</returns>
    private static int MeasureFirstValue(ReadOnlySpan<byte> buffer)
    {
        var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);
        if (!reader.Read())
        {
            reader = new Utf8JsonReader(buffer, isFinalBlock: true, state: default);
            reader.Read();
        }

        int depth = 0;
        do
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    depth++;
                    break;

                case JsonTokenType.EndObject:
                case JsonTokenType.EndArray:
                    depth--;
                    break;
            }
        }
        while (depth > 0 && reader.Read());

        return (int)reader.BytesConsumed;
    }
}
