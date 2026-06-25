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
        foreach (var property in actual.EnumerateMapping())
            entries[property.Name] = property.Value;

        var expectedCount = 0;
        foreach (var property in expected.EnumerateObject())
        {
            expectedCount++;
            if (!entries.TryGetValue(property.Name, out var child) || !Matches(property.Value, child))
                return false;
        }

        return expectedCount == entries.Count;
    }

    private static bool MatchesArray(JsonElement expected, YamlElement actual)
    {
        if (actual.ValueKind != YamlValueKind.Sequence)
            return false;

        var index = 0;
        var length = actual.GetSequenceLength();
        foreach (var element in expected.EnumerateArray())
        {
            if (index >= length || !Matches(element, actual[index]))
                return false;

            index++;
        }

        return index == length;
    }

    private static bool MatchesNumber(JsonElement expected, YamlElement actual)
    {
        var target = expected.GetDouble();
        return actual.ValueKind switch
        {
            YamlValueKind.Integer => actual.GetInt64() == target,
            YamlValueKind.Float => NumbersEqual(actual.GetDouble(), target),
            _ => false,
        };
    }

    private static bool NumbersEqual(double a, double b) =>
        a == b || (double.IsNaN(a) && double.IsNaN(b)) ||
        string.Equals(a.ToString("R", CultureInfo.InvariantCulture), b.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
