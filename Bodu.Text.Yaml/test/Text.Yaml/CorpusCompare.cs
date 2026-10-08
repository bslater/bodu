// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusCompare.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.Json;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

/// <summary>
/// Compares a parsed <see cref="YamlElement" /> against the canonical JSON expectation published by the
/// <c>yaml-test-suite</c>, applying the JSON-compatible value mapping of the library's YAML profile.
/// </summary>
/// <remarks>
/// The release-note fix catalogues under <c>corpus/yaml/fixes/</c> write their expectations in the same convention,
/// with two refinements that <see cref="Matches(JsonElement, YamlElement, bool)" /> and
/// <see cref="MarkNonFiniteFloats" /> support: a number written without a fraction or an exponent expects an integer,
/// and the non-finite floats, which JSON cannot hold, are written as the bare tokens <c>.inf</c>, <c>-.inf</c> and
/// <c>.nan</c>.
/// </remarks>
internal static class CorpusCompare
{
    /// <summary>
    /// The character that opens a JSON string standing for a non-finite float, as <see cref="MarkNonFiniteFloats" />
    /// writes it. U+FFFF is a noncharacter outside the YAML printable set, and no expectation in the yaml-test-suite or
    /// the release-note catalogues holds a string that begins with it.
    /// </summary>
    public const char NonFiniteFloatMark = '\uFFFF';

    /// <summary>The non-finite float tokens, longest first so that <c>-.inf</c> is not taken for <c>.inf</c>.</summary>
    private static readonly string[] s_nonFiniteTokens = ["-.inf", ".inf", ".nan"];

    /// <summary>
    /// Determines whether a parsed YAML element matches its JSON expectation.
    /// </summary>
    /// <param name="expected">The JSON expectation element.</param>
    /// <param name="actual">The parsed YAML element.</param>
    /// <param name="distinguishIntegers">
    /// <see langword="true" /> to match a JSON number written without a fraction or an exponent only with an integer
    /// scalar, and any other number only with a float scalar; <see langword="false" /> to match numbers by value alone.
    /// </param>
    /// <returns><see langword="true" /> when the two represent the same data.</returns>
    /// <remarks>
    /// A JSON string that begins with <see cref="NonFiniteFloatMark" /> matches the float scalar whose value the rest
    /// of the string names: <c>.inf</c>, <c>-.inf</c> or <c>.nan</c>.
    /// </remarks>
    public static bool Matches(JsonElement expected, YamlElement actual, bool distinguishIntegers = false)
    {
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                return MatchesObject(expected, actual, distinguishIntegers);

            case JsonValueKind.Array:
                return MatchesArray(expected, actual, distinguishIntegers);

            case JsonValueKind.String:
                string text = expected.GetString()!;
                if (text.StartsWith(NonFiniteFloatMark))
                    return MatchesNonFiniteFloat(text[1..], actual);

                return actual.ValueKind == YamlValueKind.String
                    && string.Equals(actual.GetString(), text, StringComparison.Ordinal);

            case JsonValueKind.Number:
                return MatchesNumber(expected, actual, distinguishIntegers);

            case JsonValueKind.True:
            case JsonValueKind.False:
                return actual.ValueKind == YamlValueKind.Boolean && actual.GetBoolean() == expected.GetBoolean();

            case JsonValueKind.Null:
                return actual.ValueKind == YamlValueKind.Null;

            default:
                return false;
        }
    }

    /// <summary>
    /// Compares a JSON object with a YAML mapping, ignoring entry order.
    /// </summary>
    /// <param name="expected">The JSON object.</param>
    /// <param name="actual">The parsed YAML element.</param>
    /// <param name="distinguishIntegers">Whether numbers are matched by kind as well as by value.</param>
    /// <returns><see langword="true" /> when the element is a mapping holding exactly the object's entries.</returns>
    private static bool MatchesObject(JsonElement expected, YamlElement actual, bool distinguishIntegers)
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
            if (!entries.TryGetValue(property.Name, out YamlElement child) || !Matches(property.Value, child, distinguishIntegers))
                return false;
        }

        return expectedCount == entries.Count;
    }

    /// <summary>
    /// Compares a JSON array with a YAML sequence, item by item.
    /// </summary>
    /// <param name="expected">The JSON array.</param>
    /// <param name="actual">The parsed YAML element.</param>
    /// <param name="distinguishIntegers">Whether numbers are matched by kind as well as by value.</param>
    /// <returns><see langword="true" /> when the element is a sequence matching the array item by item.</returns>
    private static bool MatchesArray(JsonElement expected, YamlElement actual, bool distinguishIntegers)
    {
        if (actual.ValueKind != YamlValueKind.Sequence)
            return false;

        int index = 0;
        int length = actual.GetSequenceLength();
        foreach (JsonElement element in expected.EnumerateArray())
        {
            if (index >= length || !Matches(element, actual[index], distinguishIntegers))
                return false;

            index++;
        }

        return index == length;
    }

    /// <summary>
    /// Compares a JSON number with a YAML integer or float scalar.
    /// </summary>
    /// <param name="expected">The JSON number.</param>
    /// <param name="actual">The parsed YAML element.</param>
    /// <param name="distinguishIntegers">
    /// Whether a number written without a fraction or an exponent matches only an integer scalar.
    /// </param>
    /// <returns><see langword="true" /> when the element is a number scalar of the same value.</returns>
    private static bool MatchesNumber(JsonElement expected, YamlElement actual, bool distinguishIntegers)
    {
        // Compare by numeric category instead of forcing every value through double, which would mask differences for
        // integers above 2^53 and other precision-sensitive cases. A YAML integer is compared exactly as a 64-bit
        // integer; only genuine floating-point values (and integers too large for the profile's Int64 store, which
        // resolve as floats) fall back to a double comparison. A number with a fraction or an exponent never reads as
        // an Int64, so only the float comparison needs the integer refinement.
        switch (actual.ValueKind)
        {
            case YamlValueKind.Integer:
                return expected.TryGetInt64(out long expectedInt) && actual.GetInt64() == expectedInt;

            case YamlValueKind.Float:
                return !(distinguishIntegers && IsIntegerLiteral(expected))
                    && expected.TryGetDouble(out double expectedDouble)
                    && NumbersEqual(actual.GetDouble(), expectedDouble);

            default:
                return false;
        }
    }

    /// <summary>
    /// Determines whether a JSON number is written without a fraction or an exponent.
    /// </summary>
    /// <param name="number">The JSON number.</param>
    /// <returns><see langword="true" /> when the number's text holds no <c>.</c>, <c>e</c> or <c>E</c>.</returns>
    private static bool IsIntegerLiteral(JsonElement number) =>
        number.GetRawText().AsSpan().IndexOfAny('.', 'e', 'E') < 0;

    /// <summary>
    /// Compares a non-finite float token with a YAML float scalar.
    /// </summary>
    /// <param name="token">The token: <c>.inf</c>, <c>-.inf</c> or <c>.nan</c>.</param>
    /// <param name="actual">The parsed YAML element.</param>
    /// <returns><see langword="true" /> when the element is a float scalar holding the value the token names.</returns>
    private static bool MatchesNonFiniteFloat(string token, YamlElement actual)
    {
        if (actual.ValueKind != YamlValueKind.Float)
            return false;

        double value = actual.GetDouble();
        return token switch
        {
            ".inf" => double.IsPositiveInfinity(value),
            "-.inf" => double.IsNegativeInfinity(value),
            ".nan" => double.IsNaN(value),
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether two doubles are equal, treating two NaN values as equal.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns><see langword="true" /> when the values are equal or have the same round-trip text.</returns>
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
    /// Rewrites the bare tokens <c>.inf</c>, <c>-.inf</c> and <c>.nan</c> that a release-note catalogue expectation
    /// writes where a number may stand into JSON strings that open with <see cref="NonFiniteFloatMark" />, which
    /// <see cref="Matches(JsonElement, YamlElement, bool)" /> compares with a float scalar.
    /// </summary>
    /// <param name="utf8Json">The expectation: JSON values, with the bare tokens where a number may stand.</param>
    /// <returns>The expectation as plain JSON, suitable for <see cref="SplitJsonValues" />.</returns>
    /// <remarks>
    /// Text inside a JSON string is copied unchanged, so a string such as <c>".inf"</c> stays a string.
    /// </remarks>
    public static byte[] MarkNonFiniteFloats(ReadOnlySpan<byte> utf8Json)
    {
        var output = new List<byte>(utf8Json.Length);
        bool inString = false;
        for (int i = 0; i < utf8Json.Length; i++)
        {
            byte b = utf8Json[i];
            if (inString)
            {
                output.Add(b);
                if (b == (byte)'\\' && i + 1 < utf8Json.Length)
                    output.Add(utf8Json[++i]);
                else if (b == (byte)'"')
                    inString = false;

                continue;
            }

            string? token = FindNonFiniteToken(utf8Json[i..]);
            if (token is null)
            {
                inString = b == (byte)'"';
                output.Add(b);
                continue;
            }

            output.AddRange(JsonSerializer.SerializeToUtf8Bytes(NonFiniteFloatMark + token));
            i += token.Length - 1;
        }

        return [.. output];
    }

    /// <summary>
    /// Finds the non-finite float token that a buffer opens with.
    /// </summary>
    /// <param name="utf8Json">The rest of an expectation, from a position outside any JSON string.</param>
    /// <returns>The token, or <see langword="null" /> when the buffer opens with none.</returns>
    private static string? FindNonFiniteToken(ReadOnlySpan<byte> utf8Json)
    {
        foreach (string token in s_nonFiniteTokens)
        {
            if (utf8Json.StartsWith(Encoding.ASCII.GetBytes(token)))
                return token;
        }

        return null;
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
