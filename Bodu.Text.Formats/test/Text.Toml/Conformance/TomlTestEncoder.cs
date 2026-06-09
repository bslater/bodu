// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTestEncoder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Nodes;

namespace Bodu.Text.Toml;

/// <summary>
/// Encodes a <see cref="TomlValue" /> tree into the tagged-JSON representation used by the <c>toml-test</c> conformance
/// suite, where every scalar is rendered as <c>{ "type": ..., "value": ... }</c> and tables and arrays map to JSON
/// objects and arrays.
/// </summary>
internal static class TomlTestEncoder
{
    /// <summary>
    /// Encodes a value into its tagged-JSON node.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The tagged-JSON node.</returns>
    public static JsonNode Encode(TomlValue value) =>
        value switch
        {
            TomlTable table => EncodeTable(table),
            TomlArray array => EncodeArray(array),
            _ => EncodeScalar(value),
        };

    /// <summary>
    /// Encodes a table into a JSON object.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <returns>The JSON object.</returns>
    private static JsonObject EncodeTable(TomlTable table)
    {
        var node = new JsonObject();
        foreach (KeyValuePair<string, TomlValue> pair in table)
            node[pair.Key] = Encode(pair.Value);
        return node;
    }

    /// <summary>
    /// Encodes an array into a JSON array.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <returns>The JSON array.</returns>
    private static JsonArray EncodeArray(TomlArray array)
    {
        var node = new JsonArray();
        foreach (TomlValue item in array)
            node.Add(Encode(item));
        return node;
    }

    /// <summary>
    /// Encodes a scalar into a <c>{ "type", "value" }</c> tagged-JSON object.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    /// <returns>The tagged-JSON object.</returns>
    private static JsonObject EncodeScalar(TomlValue value) =>
        value switch
        {
            TomlString s => Leaf("string", s.Value),
            TomlInteger i => Leaf("integer", i.Value.ToString(CultureInfo.InvariantCulture)),
            TomlFloat f => Leaf("float", EncodeFloat(f.Value)),
            TomlBoolean b => Leaf("bool", b.Value ? "true" : "false"),
            TomlOffsetDateTime odt => Leaf("datetime", odt.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture)),
            TomlLocalDateTime ldt => Leaf("datetime-local", ldt.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture)),
            TomlLocalDate ld => Leaf("date-local", ld.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            TomlLocalTime lt => Leaf("time-local", lt.Value.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture)),
            _ => throw new InvalidOperationException($"Unexpected TOML value kind '{value.Kind}'."),
        };

    /// <summary>
    /// Builds a tagged-JSON leaf object.
    /// </summary>
    /// <param name="type">The toml-test type tag.</param>
    /// <param name="value">The string-form value.</param>
    /// <returns>The leaf object.</returns>
    private static JsonObject Leaf(string type, string value) =>
        new() { ["type"] = type, ["value"] = value };

    /// <summary>
    /// Renders a double in the tagged-JSON float form.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The string form.</returns>
    private static string EncodeFloat(double value) =>
        double.IsNaN(value) ? "nan"
        : double.IsPositiveInfinity(value) ? "inf"
        : double.IsNegativeInfinity(value) ? "-inf"
        : value.ToString("R", CultureInfo.InvariantCulture);
}
