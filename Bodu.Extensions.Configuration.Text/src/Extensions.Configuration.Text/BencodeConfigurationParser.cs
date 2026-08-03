// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConfigurationParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Text.Bencode;
using Bodu.Text.Bencode.Document;

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Flattens a parsed Bencode document into the colon-delimited key/value map consumed by
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration" />.
/// </summary>
/// <remarks>
/// <para>
/// The document root must be a dictionary; an integer, byte-string, or list root is rejected because it cannot
/// contribute named configuration keys. Nested dictionaries contribute a
/// <see cref="ConfigurationPath.KeyDelimiter" /> segment per level; list elements contribute their zero-based index
/// as a segment, mirroring the behaviour of the framework JSON configuration provider. Keys are compared
/// case-insensitively, so two Bencode keys that differ only in case map to the same configuration key and are
/// rejected as a duplicate.
/// </para>
/// <para>
/// The document is read through the read-only <see cref="BencodeDocument" /> object model from the
/// <c>Bodu.Text.Bencode</c> library with its strict canonical defaults, so unsorted or duplicate dictionary keys are
/// rejected at the parse. Integers are rendered with <see cref="CultureInfo.InvariantCulture" /> across the full
/// unsigned 64-bit range the format supports. Byte strings are decoded as UTF-8; content that is not valid UTF-8 is
/// decoded with U+FFFD replacement rather than rejected, matching the BCL decoding convention.
/// </para>
/// </remarks>
internal static class BencodeConfigurationParser
{
    /// <summary>
    /// Reads a Bencode document from <paramref name="stream" /> and flattens it into a configuration map.
    /// </summary>
    /// <param name="stream">The readable stream containing a Bencode document.</param>
    /// <returns>The flattened, case-insensitive key/value map.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">Thrown when the stream contents are not a valid Bencode document.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the document root is not a dictionary, or when two keys collide after case-insensitive flattening.
    /// </exception>
    public static IDictionary<string, string?> Parse(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        byte[] bytes = ReadToEnd(stream);

        using var document = BencodeDocument.Parse(bytes);
        return Flatten(document.RootElement);
    }

    /// <summary>
    /// Flattens the supplied root dictionary <paramref name="root" /> into a configuration map.
    /// </summary>
    /// <param name="root">The root dictionary element.</param>
    /// <returns>The flattened, case-insensitive key/value map.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="root" /> is not a dictionary, or when two keys collide after case-insensitive
    /// flattening.
    /// </exception>
    public static IDictionary<string, string?> Flatten(BencodeElement root)
    {
        if (root.ValueKind != BencodeValueKind.Object)
            throw new FormatException(string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.Format_Invalid_BencodeRootNotDictionary, root.ValueKind));

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        VisitDictionary(root, data, prefix: null);
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
    /// Visits each property of a dictionary, recursing into nested containers.
    /// </summary>
    /// <param name="dictionary">The dictionary element.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="prefix">The key prefix accumulated so far, or <see langword="null" /> at the root.</param>
    private static void VisitDictionary(BencodeElement dictionary, IDictionary<string, string?> data, string? prefix)
    {
        foreach (BencodeProperty property in dictionary.EnumerateObject())
            VisitValue(property.Value, data, Combine(prefix, property.Name));
    }

    /// <summary>
    /// Visits each element of a list, contributing its index as a key segment.
    /// </summary>
    /// <param name="list">The list element.</param>
    /// <param name="data">The destination map.</param>
    /// <param name="prefix">The key prefix accumulated so far.</param>
    private static void VisitList(BencodeElement list, IDictionary<string, string?> data, string prefix)
    {
        int length = list.GetArrayLength();
        for (int i = 0; i < length; i++)
            VisitValue(list[i], data, Combine(prefix, i.ToString(CultureInfo.InvariantCulture)));
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
    private static void VisitValue(BencodeElement value, IDictionary<string, string?> data, string key)
    {
        switch (value.ValueKind)
        {
            case BencodeValueKind.Object:
                VisitDictionary(value, data, key);
                break;

            case BencodeValueKind.Array:
                VisitList(value, data, key);
                break;

            default:
                if (data.ContainsKey(key))
                    throw new FormatException(string.Format(CultureInfo.CurrentCulture, ConfigurationTextResourceStrings.Format_Invalid_BencodeDuplicateConfigurationKey, key));

                data[key] = ConvertScalar(value);
                break;
        }
    }

    /// <summary>
    /// Converts a scalar Bencode value to its invariant-culture configuration string representation.
    /// </summary>
    /// <param name="value">The scalar value element.</param>
    /// <returns>The invariant-culture string representation.</returns>
    private static string ConvertScalar(BencodeElement value)
    {
        if (value.ValueKind == BencodeValueKind.ByteString)
            return value.GetString();

        // A parseable Bencode integer always fits the signed or the unsigned 64-bit range, so the conversion is
        // total: values beyond long.MaxValue fall through to the unsigned read.
        return value.TryGetInt64(out long signed)
            ? signed.ToString(CultureInfo.InvariantCulture)
            : value.GetUInt64().ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Joins a key prefix and a segment with the configuration key delimiter.
    /// </summary>
    /// <param name="prefix">The existing prefix, or <see langword="null" />.</param>
    /// <param name="segment">The segment to append.</param>
    /// <returns>The combined key.</returns>
    private static string Combine(string? prefix, string segment) =>
        prefix is null ? segment : prefix + ConfigurationPath.KeyDelimiter + segment;
}
