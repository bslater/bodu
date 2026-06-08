// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Text.Toml;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Flattens a parsed TOML document into the colon-delimited key/value map consumed by
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration" />.
/// </summary>
/// <remarks>
/// <para>
/// Nested tables contribute a <see cref="ConfigurationPath.KeyDelimiter" /> segment per level; array elements
/// contribute their zero-based index as a segment, mirroring the behaviour of the framework JSON configuration
/// provider. Keys are compared case-insensitively, so two TOML keys that differ only in case map to the same
/// configuration key and are rejected as a duplicate.
/// </para>
/// </remarks>
internal static class TomlConfigurationParser
{
    /// <summary>
    /// Reads a TOML document from <paramref name="stream" /> and flattens it into a configuration map.
    /// </summary>
    /// <param name="stream">The readable stream containing UTF-8 TOML text.</param>
    /// <returns>The flattened, case-insensitive key/value map.</returns>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="FormatException">Thrown when two keys collide after case-insensitive flattening.</exception>
    public static IDictionary<string, string?> Parse(Stream stream) =>
        Flatten(Toml.Parse(stream));

    /// <summary>
    /// Flattens <paramref name="root" /> into a configuration map.
    /// </summary>
    /// <param name="root">The root table.</param>
    /// <returns>The flattened, case-insensitive key/value map.</returns>
    /// <exception cref="FormatException">Thrown when two keys collide after case-insensitive flattening.</exception>
    public static IDictionary<string, string?> Flatten(TomlTable root)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        VisitTable(root, data, prefix: null);
        return data;
    }

    /// <summary>
    /// Visits each entry of a table, recursing into nested containers.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="prefix">The key prefix accumulated so far, or <see langword="null" /> at the root.</param>
    private static void VisitTable(TomlTable table, IDictionary<string, string?> data, string? prefix)
    {
        foreach (var pair in table)
            VisitValue(pair.Value, data, Combine(prefix, pair.Key));
    }

    /// <summary>
    /// Visits each element of an array, contributing its index as a key segment.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="prefix">The key prefix accumulated so far.</param>
    private static void VisitArray(TomlArray array, IDictionary<string, string?> data, string prefix)
    {
        for (var i = 0; i < array.Count; i++)
            VisitValue(array[i], data, Combine(prefix, i.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Visits a single value, recursing into containers or recording a scalar.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="key">The fully qualified key for the value.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="key" /> already exists in <paramref name="data" />.</exception>
    private static void VisitValue(TomlValue value, IDictionary<string, string?> data, string key)
    {
        switch (value)
        {
            case TomlTable table:
                VisitTable(table, data, key);
                break;
            case TomlArray array:
                VisitArray(array, data, key);
                break;
            default:
                if (data.ContainsKey(key))
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.Format_Invalid_TomlDuplicateConfigurationKey, key));
                data[key] = ConvertScalar(value);
                break;
        }
    }

    /// <summary>
    /// Converts a scalar TOML value to its configuration string representation.
    /// </summary>
    /// <param name="value">The scalar value.</param>
    /// <returns>The string representation.</returns>
    private static string ConvertScalar(TomlValue value) =>
        value.ToString() ?? string.Empty;

    /// <summary>
    /// Joins a key prefix and a segment with the configuration key delimiter.
    /// </summary>
    /// <param name="prefix">The existing prefix, or <see langword="null" />.</param>
    /// <param name="segment">The segment to append.</param>
    /// <returns>The combined key.</returns>
    private static string Combine(string? prefix, string segment) =>
        prefix is null ? segment : prefix + ConfigurationPath.KeyDelimiter + segment;
}
