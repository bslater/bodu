// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlConfigurationParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Text.Toml;
using Bodu.Text.Toml.Document;

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
/// <para>
/// The document is read through the read-only <see cref="TomlDocument" /> object model from the <c>Bodu.Text.Toml</c>
/// library; each scalar is rendered with <see cref="CultureInfo.InvariantCulture" /> so the flattened representation is
/// stable regardless of the ambient culture.
/// </para>
/// </remarks>
internal static class TomlConfigurationParser
{
    /// <summary>
    /// Reads a TOML document from <paramref name="stream" /> and flattens it into a configuration map.
    /// </summary>
    /// <param name="stream">The readable stream containing UTF-8 TOML text.</param>
    /// <returns>The flattened, case-insensitive key/value map.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="FormatException">Thrown when two keys collide after case-insensitive flattening.</exception>
    public static IDictionary<string, string?> Parse(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        byte[] bytes = ReadToEnd(stream);

        using var document = TomlDocument.Parse(bytes);
        return Flatten(document.RootElement);
    }

    /// <summary>
    /// Flattens the supplied root table <paramref name="root" /> into a configuration map.
    /// </summary>
    /// <param name="root">The root table element.</param>
    /// <returns>The flattened, case-insensitive key/value map.</returns>
    /// <exception cref="FormatException">Thrown when two keys collide after case-insensitive flattening.</exception>
    public static IDictionary<string, string?> Flatten(TomlElement root)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        VisitTable(root, data, prefix: null);
        return data;
    }

    /// <summary>
    /// Reads <paramref name="stream" /> to its end and returns the bytes consumed.
    /// </summary>
    /// <param name="stream">The readable stream.</param>
    /// <returns>The bytes read from the stream.</returns>
    private static byte[] ReadToEnd(Stream stream)
    {
        if (stream is MemoryStream existing)
            return existing.ToArray();

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Visits each property of a table, recursing into nested containers.
    /// </summary>
    /// <param name="table">The table element.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="prefix">The key prefix accumulated so far, or <see langword="null" /> at the root.</param>
    private static void VisitTable(TomlElement table, IDictionary<string, string?> data, string? prefix)
    {
        foreach (TomlProperty property in table.EnumerateObject())
            VisitValue(property.Value, data, Combine(prefix, property.Name));
    }

    /// <summary>
    /// Visits each element of an array, contributing its index as a key segment.
    /// </summary>
    /// <param name="array">The array element.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="prefix">The key prefix accumulated so far.</param>
    private static void VisitArray(TomlElement array, IDictionary<string, string?> data, string prefix)
    {
        int length = array.GetArrayLength();
        for (int i = 0; i < length; i++)
            VisitValue(array[i], data, Combine(prefix, i.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Visits a single value, recursing into containers or recording a scalar.
    /// </summary>
    /// <param name="value">The value element.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="key">The fully qualified key for the value.</param>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="key" /> already exists in <paramref name="data" />.
    /// </exception>
    private static void VisitValue(TomlElement value, IDictionary<string, string?> data, string key)
    {
        switch (value.ValueKind)
        {
            case TomlValueKind.Table:
                VisitTable(value, data, key);
                break;

            case TomlValueKind.Array:
                VisitArray(value, data, key);
                break;

            default:
                if (data.ContainsKey(key))
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.Format_Invalid_TomlDuplicateConfigurationKey, key));

                data[key] = ConvertScalar(value);
                break;
        }
    }

    /// <summary>
    /// Converts a scalar TOML value to its invariant-culture configuration string representation.
    /// </summary>
    /// <param name="value">The scalar value element.</param>
    /// <returns>The invariant-culture string representation.</returns>
    private static string ConvertScalar(TomlElement value) =>
        value.ValueKind switch
        {
            TomlValueKind.String => value.GetString(),
            TomlValueKind.Integer => value.GetInt64().ToString(CultureInfo.InvariantCulture),
            TomlValueKind.Float => value.GetDouble().ToString(CultureInfo.InvariantCulture),
            TomlValueKind.Boolean => value.GetBoolean() ? "true" : "false",
            TomlValueKind.OffsetDateTime => value.GetDateTimeOffset().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture),
            TomlValueKind.LocalDateTime => value.GetDateTime().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
            TomlValueKind.LocalDate => value.GetDateOnly().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => value.GetTimeOnly().ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Joins a key prefix and a segment with the configuration key delimiter.
    /// </summary>
    /// <param name="prefix">The existing prefix, or <see langword="null" />.</param>
    /// <param name="segment">The segment to append.</param>
    /// <returns>The combined key.</returns>
    private static string Combine(string? prefix, string segment) =>
        prefix is null ? segment : prefix + ConfigurationPath.KeyDelimiter + segment;
}
