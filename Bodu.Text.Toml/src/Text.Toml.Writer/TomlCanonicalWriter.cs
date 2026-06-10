// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlCanonicalWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Serializes the in-memory value tree assembled by <see cref="Utf8TomlWriter" /> to canonical TOML text: a table's
/// scalar and array members are emitted first as <c>key = value</c> lines, then its sub-tables under <c>[header]</c>
/// sections and its arrays of tables under <c>[[header]]</c> sections, depth-first in document order.
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
/// the RFC 3339 form matching their kind. All value formatting uses <see cref="CultureInfo.InvariantCulture" />.
/// </para>
/// </remarks>
internal static class TomlCanonicalWriter
{
    /// <summary>
    /// The number of 100-nanosecond ticks in one second, used to extract the fractional-second portion of a time.
    /// </summary>
    private const long TicksPerSecond = 10_000_000L;

    /// <summary>
    /// Writes the contents of a table to canonical TOML: inline key/value lines first, then sub-table and
    /// array-of-tables sections.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="table">The table to write.</param>
    /// <param name="path">The dotted header path of the table, empty for the document root.</param>
    /// <remarks>
    /// A member is emitted inline when it is a scalar, or an array that is not an array of tables; it is emitted as a
    /// section when it is a sub-table or an array whose every element is a table. Inline members are written before any
    /// section so that no key/value line is orphaned beneath a later <c>[header]</c>.
    /// </remarks>
    internal static void WriteTableBody(StringBuilder builder, TomlTableWriterNode table, IReadOnlyList<string> path)
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
            builder.Append(FormatKey(entry.Key)).Append(" = ");
            WriteInlineValue(builder, entry.Value);
            builder.Append('\n');
        }

        foreach (KeyValuePair<string, TomlWriterNode> entry in sectionEntries)
        {
            List<string> childPath = [.. path, entry.Key];
            string header = FormatKeyPath(childPath);

            if (entry.Value is TomlTableWriterNode subTable)
            {
                WriteHeaderLine(builder, "[" + header + "]");
                WriteTableBody(builder, subTable, childPath);
            }
            else
            {
                foreach (TomlWriterNode element in ((TomlArrayWriterNode)entry.Value).Items)
                {
                    WriteHeaderLine(builder, "[[" + header + "]]");
                    WriteTableBody(builder, (TomlTableWriterNode)element, childPath);
                }
            }
        }
    }

    /// <summary>
    /// Writes a section header, preceded by a blank line when the document already has content.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="header">The header text including its brackets.</param>
    private static void WriteHeaderLine(StringBuilder builder, string header)
    {
        if (builder.Length > 0)
            builder.Append('\n');

        builder.Append(header).Append('\n');
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
    /// <param name="builder">The destination builder.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="NotSupportedException">Thrown when the value is of an unrecognized node kind.</exception>
    private static void WriteInlineValue(StringBuilder builder, TomlWriterNode value)
    {
        switch (value)
        {
            case TomlScalarWriterNode scalar:
                WriteScalar(builder, scalar);
                break;
            case TomlArrayWriterNode array:
                WriteInlineArray(builder, array);
                break;
            case TomlTableWriterNode table:
                WriteInlineTable(builder, table);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Writes a scalar value in its canonical TOML form, selected by its token type.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="scalar">The scalar to write.</param>
    private static void WriteScalar(StringBuilder builder, TomlScalarWriterNode scalar)
    {
        switch (scalar.TokenType)
        {
            case TomlTokenType.String:
                WriteBasicString(builder, (string)scalar.Value);
                break;
            case TomlTokenType.Integer:
                builder.Append(((long)scalar.Value).ToString(CultureInfo.InvariantCulture));
                break;
            case TomlTokenType.Float:
                builder.Append(FormatFloat((double)scalar.Value));
                break;
            case TomlTokenType.Boolean:
                builder.Append((bool)scalar.Value ? "true" : "false");
                break;
            case TomlTokenType.OffsetDateTime:
                builder.Append(FormatOffsetDateTime((DateTimeOffset)scalar.Value));
                break;
            case TomlTokenType.LocalDateTime:
                DateTime localDateTime = (DateTime)scalar.Value;
                builder.Append(localDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)).Append(FractionString(localDateTime.Ticks));
                break;
            case TomlTokenType.LocalDate:
                builder.Append(((DateOnly)scalar.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            default:
                TimeOnly localTime = (TimeOnly)scalar.Value;
                builder.Append(localTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append(FractionString(localTime.Ticks));
                break;
        }
    }

    /// <summary>
    /// Writes an array in inline form as <c>[v1, v2, v3]</c>.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="array">The array to write.</param>
    private static void WriteInlineArray(StringBuilder builder, TomlArrayWriterNode array)
    {
        builder.Append('[');
        for (int i = 0; i < array.Items.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");

            WriteInlineValue(builder, array.Items[i]);
        }

        builder.Append(']');
    }

    /// <summary>
    /// Writes a table in inline form as <c>{ k = v, k2 = v2 }</c>, or <c>{}</c> when the table is empty.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="table">The table to write.</param>
    private static void WriteInlineTable(StringBuilder builder, TomlTableWriterNode table)
    {
        if (table.Items.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append("{ ");
        bool first = true;
        foreach (KeyValuePair<string, TomlWriterNode> pair in table.Items)
        {
            if (!first)
                builder.Append(", ");

            first = false;
            builder.Append(FormatKey(pair.Key)).Append(" = ");
            WriteInlineValue(builder, pair.Value);
        }

        builder.Append(" }");
    }

    /// <summary>
    /// Formats a dotted key path, quoting each segment that is not a bare key.
    /// </summary>
    /// <param name="path">The key segments.</param>
    /// <returns>The formatted dotted path.</returns>
    private static string FormatKeyPath(IReadOnlyList<string> path)
    {
        StringBuilder builder = new();
        for (int i = 0; i < path.Count; i++)
        {
            if (i > 0)
                builder.Append('.');

            builder.Append(FormatKey(path[i]));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats a single key as a bare key when possible, or as a quoted basic string otherwise.
    /// </summary>
    /// <param name="key">The key to format.</param>
    /// <returns>The formatted key.</returns>
    private static string FormatKey(string key)
    {
        if (IsBareKey(key))
            return key;

        StringBuilder builder = new();
        WriteBasicString(builder, key);
        return builder.ToString();
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
    /// Writes a string as a basic (double-quoted) TOML string, escaping control and reserved characters.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="value">The string value to write.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="value" /> contains an unpaired surrogate.
    /// </exception>
    private static void WriteBasicString(StringBuilder builder, string value)
    {
        builder.Append('"');
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    throw UnpairedSurrogateError();

                builder.Append(c).Append(value[++i]);
                continue;
            }

            if (char.IsLowSurrogate(c))
                throw UnpairedSurrogateError();

            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\t': builder.Append("\\t"); break;
                case '\n': builder.Append("\\n"); break;
                case '\f': builder.Append("\\f"); break;
                case '\r': builder.Append("\\r"); break;
                default:
                    if (c < 0x20 || c == 0x7F)
                        builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(c);
                    break;
            }
        }

        builder.Append('"');
    }

    /// <summary>
    /// Creates the exception thrown when a key or string value contains an unpaired surrogate.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static InvalidOperationException UnpairedSurrogateError() =>
        new(TomlResourceStrings.Format_Invalid_TomlUnpairedSurrogate);

    /// <summary>
    /// Formats a double as a valid TOML float, ensuring a fractional point or exponent and the special sentinels.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted float.</returns>
    /// <remarks>
    /// The shortest round-trippable spelling is taken from the <c>"R"</c> format; a <c>.0</c> suffix is appended when
    /// the result has neither a decimal point nor an exponent so that it reads back as a float rather than an integer.
    /// </remarks>
    private static string FormatFloat(double value)
    {
        if (double.IsNaN(value))
            return "nan";
        if (double.IsPositiveInfinity(value))
            return "inf";
        if (double.IsNegativeInfinity(value))
            return "-inf";

        string text = value.ToString("R", CultureInfo.InvariantCulture);
        if (text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0)
            text += ".0";

        return text;
    }

    /// <summary>
    /// Formats an offset date-time in RFC 3339 form, using <c>Z</c> for a zero offset.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted instant.</returns>
    private static string FormatOffsetDateTime(DateTimeOffset value)
    {
        StringBuilder builder = new();
        builder.Append(value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
        builder.Append(FractionString(value.Ticks));

        if (value.Offset == TimeSpan.Zero)
            builder.Append('Z');
        else
            builder.Append(value.Offset < TimeSpan.Zero ? '-' : '+').Append(value.Offset.Duration().ToString("hh':'mm", CultureInfo.InvariantCulture));

        return builder.ToString();
    }

    /// <summary>
    /// Renders the fractional-second portion of a tick count, or an empty string when there is none.
    /// </summary>
    /// <param name="ticks">The tick count.</param>
    /// <returns>
    /// The fractional string, including the leading dot, or an empty string when the fraction is zero.
    /// </returns>
    private static string FractionString(long ticks)
    {
        long fraction = ticks % TicksPerSecond;
        if (fraction == 0)
            return string.Empty;

        return "." + fraction.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
    }
}
