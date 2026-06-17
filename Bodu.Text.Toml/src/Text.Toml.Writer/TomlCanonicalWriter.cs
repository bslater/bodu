// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlCanonicalWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Serializes the in-memory value tree assembled by <see cref="Utf8TomlWriter" /> to canonical TOML, emitting UTF-8
/// bytes directly: a table's scalar and array members are emitted first as <c>key = value</c> lines, then its
/// sub-tables under <c>[header]</c> sections and its arrays of tables under <c>[[header]]</c> sections, depth-first in
/// document order.
/// </summary>
/// <remarks>
/// <para>
/// Whether a table becomes a <c>[header]</c> block or an inline <c>{ … }</c> depends on where it sits in the finished
/// document, so emission is layout-driven rather than incremental: a sub-table member of a table surfaces as a header
/// block, whereas a table that appears as an array element is written inline. Arrays of non-table values are inline (
/// <c>[1, 2, 3]</c>); an array whose every element is a table surfaces as a run of <c>[[header]]</c> blocks.
/// </para>
/// <para>
/// Keys are bare when they match the bare-key grammar (<c>[A-Za-z0-9_-]+</c>) and basic-quoted otherwise. Strings are
/// basic-quoted with escaping; integers are decimal; floats render <c>inf</c>, <c>-inf</c>, and <c>nan</c> and
/// otherwise use their shortest round-trippable spelling with a guaranteed fractional point or exponent; date-times use
/// the RFC 3339 form matching their kind. All value formatting uses <see cref="CultureInfo.InvariantCulture" />, and
/// every formatted value is ASCII, so the bytes are written without a UTF-16 intermediate.
/// </para>
/// </remarks>
internal static class TomlCanonicalWriter
{
    /// <summary>The number of 100-nanosecond ticks in one second, used to extract the fractional-second portion of a time.</summary>
    private const long TicksPerSecond = 10_000_000L;

    /// <summary>
    /// Writes the contents of a table to canonical TOML: inline key/value lines first, then sub-table and
    /// array-of-tables sections.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="table">The table to write.</param>
    /// <param name="path">The dotted header path of the table, empty for the document root.</param>
    /// <remarks>
    /// A member is emitted inline when it is a scalar, or an array that is not an array of tables; it is emitted as a
    /// section when it is a sub-table or an array whose every element is a table. Inline members are written before any
    /// section so that no key/value line is orphaned beneath a later <c>[header]</c>.
    /// </remarks>
    internal static void WriteTableBody(ref TomlUtf8Emitter emitter, TomlTableWriterNode table, IReadOnlyList<string> path)
    {
        List<KeyValuePair<string, TomlWriterNode>> inlineEntries = [];
        List<KeyValuePair<string, TomlWriterNode>> sectionEntries = [];

        foreach (KeyValuePair<string, TomlWriterNode> pair in table.Items)
        {
            if (pair.Value is TomlTableWriterNode || IsArrayOfTables(pair.Value))
                sectionEntries.Add(pair);
            else
                inlineEntries.Add(pair);
        }

        foreach (KeyValuePair<string, TomlWriterNode> entry in inlineEntries)
        {
            WriteKey(ref emitter, entry.Key);
            emitter.Append(" = "u8);
            WriteInlineValue(ref emitter, entry.Value);
            emitter.Append((byte)'\n');
        }

        foreach (KeyValuePair<string, TomlWriterNode> entry in sectionEntries)
        {
            List<string> childPath = [.. path, entry.Key];

            if (entry.Value is TomlTableWriterNode subTable)
            {
                WriteHeaderLine(ref emitter, childPath, arrayOfTables: false);
                WriteTableBody(ref emitter, subTable, childPath);
            }
            else
            {
                foreach (TomlWriterNode element in ((TomlArrayWriterNode)entry.Value).Items)
                {
                    WriteHeaderLine(ref emitter, childPath, arrayOfTables: true);
                    WriteTableBody(ref emitter, (TomlTableWriterNode)element, childPath);
                }
            }
        }
    }

    /// <summary>
    /// Writes a section header line, preceded by a blank line when the document already has content.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="path">The dotted key path of the header.</param>
    /// <param name="arrayOfTables">
    /// <see langword="true" /> to write an <c>[[array-of-tables]]</c> header; otherwise a <c>[table]</c> header.
    /// </param>
    private static void WriteHeaderLine(ref TomlUtf8Emitter emitter, IReadOnlyList<string> path, bool arrayOfTables)
    {
        if (emitter.HasContent)
            emitter.Append((byte)'\n');

        emitter.Append(arrayOfTables ? "[["u8 : "["u8);
        for (int i = 0; i < path.Count; i++)
        {
            if (i > 0)
                emitter.Append((byte)'.');

            WriteKey(ref emitter, path[i]);
        }

        emitter.Append(arrayOfTables ? "]]\n"u8 : "]\n"u8);
    }

    /// <summary>
    /// Indicates whether a value is a non-empty array whose every element is a table.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is an array of tables; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool IsArrayOfTables(TomlWriterNode value)
    {
        if (value is not TomlArrayWriterNode array || array.Items.Count == 0)
            return false;

        foreach (TomlWriterNode element in array.Items)
        {
            if (element is not TomlTableWriterNode)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Writes a value in its inline form, dispatching on the node kind.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="NotSupportedException">Thrown when the value is of an unrecognized node kind.</exception>
    private static void WriteInlineValue(ref TomlUtf8Emitter emitter, TomlWriterNode value)
    {
        switch (value)
        {
            case TomlScalarWriterNode scalar:
                WriteScalar(ref emitter, scalar);
                break;
            case TomlArrayWriterNode array:
                WriteInlineArray(ref emitter, array);
                break;
            case TomlTableWriterNode table:
                WriteInlineTable(ref emitter, table);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Writes a scalar value in its canonical TOML form, selected by its token type.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="scalar">The scalar to write.</param>
    private static void WriteScalar(ref TomlUtf8Emitter emitter, TomlScalarWriterNode scalar)
    {
        Span<char> buffer = stackalloc char[40];
        int written;

        switch (scalar.TokenType)
        {
            case TomlTokenType.String:
                WriteBasicString(ref emitter, (string)scalar.Value);
                break;

            case TomlTokenType.Integer:
                _ = ((long)scalar.Value).TryFormat(buffer, out written, default, CultureInfo.InvariantCulture);
                emitter.AppendAscii(buffer[..written]);
                break;

            case TomlTokenType.Float:
                WriteFloat(ref emitter, (double)scalar.Value);
                break;

            case TomlTokenType.Boolean:
                emitter.Append((bool)scalar.Value ? "true"u8 : "false"u8);
                break;

            case TomlTokenType.OffsetDateTime:
                WriteOffsetDateTime(ref emitter, (DateTimeOffset)scalar.Value);
                break;

            case TomlTokenType.LocalDateTime:
                var localDateTime = (DateTime)scalar.Value;
                _ = localDateTime.TryFormat(buffer, out written, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                emitter.AppendAscii(buffer[..written]);
                WriteFraction(ref emitter, localDateTime.Ticks);
                break;

            case TomlTokenType.LocalDate:
                _ = ((DateOnly)scalar.Value).TryFormat(buffer, out written, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                emitter.AppendAscii(buffer[..written]);
                break;

            default:
                var localTime = (TimeOnly)scalar.Value;
                _ = localTime.TryFormat(buffer, out written, "HH:mm:ss", CultureInfo.InvariantCulture);
                emitter.AppendAscii(buffer[..written]);
                WriteFraction(ref emitter, localTime.Ticks);
                break;
        }
    }

    /// <summary>
    /// Writes an array in inline form as <c>[v1, v2, v3]</c>.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="array">The array to write.</param>
    private static void WriteInlineArray(ref TomlUtf8Emitter emitter, TomlArrayWriterNode array)
    {
        emitter.Append((byte)'[');
        for (int i = 0; i < array.Items.Count; i++)
        {
            if (i > 0)
                emitter.Append(", "u8);

            WriteInlineValue(ref emitter, array.Items[i]);
        }

        emitter.Append((byte)']');
    }

    /// <summary>
    /// Writes a table in inline form as <c>{ k = v, k2 = v2 }</c>, or <c>{}</c> when the table is empty.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="table">The table to write.</param>
    private static void WriteInlineTable(ref TomlUtf8Emitter emitter, TomlTableWriterNode table)
    {
        if (table.Items.Count == 0)
        {
            emitter.Append("{}"u8);
            return;
        }

        emitter.Append("{ "u8);
        bool first = true;
        foreach (KeyValuePair<string, TomlWriterNode> pair in table.Items)
        {
            if (!first)
                emitter.Append(", "u8);

            first = false;
            WriteKey(ref emitter, pair.Key);
            emitter.Append(" = "u8);
            WriteInlineValue(ref emitter, pair.Value);
        }

        emitter.Append(" }"u8);
    }

    /// <summary>
    /// Writes a single key as a bare key when possible, or as a quoted basic string otherwise.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="key">The key to write.</param>
    private static void WriteKey(ref TomlUtf8Emitter emitter, string key)
    {
        if (IsBareKey(key))
            emitter.AppendAscii(key);
        else
            WriteBasicString(ref emitter, key);
    }

    /// <summary>
    /// Indicates whether a key is a non-empty bare key composed solely of <c>A-Za-z0-9_-</c>.
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="key" /> may be written without quoting; otherwise,
    /// <see langword="false" />.
    /// </returns>
    private static bool IsBareKey(string key)
    {
        if (key.Length == 0)
            return false;

        foreach (char c in key)
        {
            if (!((c is >= 'A' and <= 'Z') || (c is >= 'a' and <= 'z') || (c is >= '0' and <= '9') || c == '_' || c == '-'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Writes a string as a basic (double-quoted) TOML string, escaping control and reserved characters and encoding
    /// the remainder as UTF-8.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="value">The string value to write.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="value" /> contains an unpaired surrogate.
    /// </exception>
    private static void WriteBasicString(ref TomlUtf8Emitter emitter, string value)
    {
        emitter.Append((byte)'"');
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    throw UnpairedSurrogateError();

                emitter.Append(new Rune(c, value[++i]));
                continue;
            }

            if (char.IsLowSurrogate(c))
                throw UnpairedSurrogateError();

            switch (c)
            {
                case '"': emitter.Append("\\\""u8); break;
                case '\\': emitter.Append("\\\\"u8); break;
                case '\b': emitter.Append("\\b"u8); break;
                case '\t': emitter.Append("\\t"u8); break;
                case '\n': emitter.Append("\\n"u8); break;
                case '\f': emitter.Append("\\f"u8); break;
                case '\r': emitter.Append("\\r"u8); break;
                default:
                    if (c < 0x20 || c == 0x7F)
                    {
                        Span<char> hex = stackalloc char[4];
                        _ = ((int)c).TryFormat(hex, out _, "X4", CultureInfo.InvariantCulture);
                        emitter.Append("\\u"u8);
                        emitter.AppendAscii(hex);
                    }
                    else if (c < 0x80)
                    {
                        emitter.Append((byte)c);
                    }
                    else
                    {
                        emitter.Append(new Rune(c));
                    }

                    break;
            }
        }

        emitter.Append((byte)'"');
    }

    /// <summary>
    /// Creates the exception thrown when a key or string value contains an unpaired surrogate.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static InvalidOperationException UnpairedSurrogateError() =>
        new(TomlResourceStrings.Format_Invalid_TomlUnpairedSurrogate);

    /// <summary>
    /// Writes a double as a valid TOML float, ensuring a fractional point or exponent and the special sentinels.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="value">The value to write.</param>
    /// <remarks>
    /// The shortest round-trippable spelling is taken from the <c>"R"</c> format; a <c>.0</c> suffix is appended when
    /// the result has neither a decimal point nor an exponent so that it reads back as a float rather than an integer.
    /// </remarks>
    private static void WriteFloat(ref TomlUtf8Emitter emitter, double value)
    {
        if (double.IsNaN(value))
        {
            emitter.Append("nan"u8);
            return;
        }

        if (double.IsPositiveInfinity(value))
        {
            emitter.Append("inf"u8);
            return;
        }

        if (double.IsNegativeInfinity(value))
        {
            emitter.Append("-inf"u8);
            return;
        }

        Span<char> buffer = stackalloc char[40];
        _ = value.TryFormat(buffer, out int written, "R", CultureInfo.InvariantCulture);

        ReadOnlySpan<char> text = buffer[..written];
        emitter.AppendAscii(text);
        if (text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0)
            emitter.Append(".0"u8);
    }

    /// <summary>
    /// Writes an offset date-time in RFC 3339 form, using <c>Z</c> for a zero offset.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="value">The value to write.</param>
    private static void WriteOffsetDateTime(ref TomlUtf8Emitter emitter, DateTimeOffset value)
    {
        Span<char> buffer = stackalloc char[40];
        _ = value.TryFormat(buffer, out int written, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        emitter.AppendAscii(buffer[..written]);
        WriteFraction(ref emitter, value.Ticks);

        if (value.Offset == TimeSpan.Zero)
        {
            emitter.Append((byte)'Z');
        }
        else
        {
            emitter.Append(value.Offset < TimeSpan.Zero ? (byte)'-' : (byte)'+');
            _ = value.Offset.Duration().TryFormat(buffer, out written, "hh':'mm", CultureInfo.InvariantCulture);
            emitter.AppendAscii(buffer[..written]);
        }
    }

    /// <summary>
    /// Writes the fractional-second portion of a tick count, or nothing when there is none.
    /// </summary>
    /// <param name="emitter">The destination emitter.</param>
    /// <param name="ticks">The tick count.</param>
    private static void WriteFraction(ref TomlUtf8Emitter emitter, long ticks)
    {
        long fraction = ticks % TicksPerSecond;
        if (fraction == 0)
            return;

        Span<char> digits = stackalloc char[7];
        _ = fraction.TryFormat(digits, out _, "D7", CultureInfo.InvariantCulture);

        int length = 7;
        while (length > 1 && digits[length - 1] == '0')
            length--;

        emitter.Append((byte)'.');
        emitter.AppendAscii(digits[..length]);
    }
}
