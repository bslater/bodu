// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Toml.Writer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Toml;

public static partial class Toml
{
    /// <summary>
    /// The number of 100-nanosecond ticks in one second.
    /// </summary>
    private const long WriterTicksPerSecond = 10_000_000L;

    /// <summary>
    /// Renders a <see cref="TomlTable" /> document to canonical TOML text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>The TOML representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The writer emits a block-style document: scalar values and arrays that are not arrays of tables are written
    /// inline; sub-tables are written under <c>[header]</c> sections, and arrays of tables under <c>[[header]]</c>
    /// sections. Because the document model does not record whether a table was originally authored as an inline table
    /// or a standard table, the output is canonicalized to the standard form; re-parsing the output yields an equal
    /// model.
    /// </para>
    /// </remarks>
    public static string Format(TomlTable document)
    {
        ThrowHelper.ThrowIfNull(document);

        var builder = new StringBuilder();
        WriteTableBody(builder, document, new List<string>());
        return builder.ToString();
    }

    /// <summary>
    /// Writes the contents of <paramref name="table" /> at <paramref name="path" />: inline key/values first, then
    /// sub-table and array-of-tables sections.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="table">The table to write.</param>
    /// <param name="path">The header path of <paramref name="table" />.</param>
    private static void WriteTableBody(StringBuilder builder, TomlTable table, List<string> path)
    {
        var inlineEntries = new List<KeyValuePair<string, TomlValue>>();
        var sectionEntries = new List<KeyValuePair<string, TomlValue>>();

        foreach (var pair in table)
        {
            if (pair.Value is TomlTable || IsArrayOfTables(pair.Value))
                sectionEntries.Add(pair);
            else
                inlineEntries.Add(pair);
        }

        foreach (var entry in inlineEntries)
        {
            builder.Append(FormatKey(entry.Key)).Append(" = ");
            WriteInlineValue(builder, entry.Value);
            builder.Append('\n');
        }

        foreach (var entry in sectionEntries)
        {
            var childPath = new List<string>(path) { entry.Key };
            var header = FormatKeyPath(childPath);

            if (entry.Value is TomlTable subTable)
            {
                WriteHeaderLine(builder, "[" + header + "]");
                WriteTableBody(builder, subTable, childPath);
            }
            else
            {
                foreach (var element in (TomlArray)entry.Value)
                {
                    WriteHeaderLine(builder, "[[" + header + "]]");
                    WriteTableBody(builder, (TomlTable)element, childPath);
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
    /// Indicates whether <paramref name="value" /> is a non-empty array whose every element is a table.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true" /> when the value is an array of tables.</returns>
    private static bool IsArrayOfTables(TomlValue value)
    {
        if (value is not TomlArray array || array.Count == 0)
            return false;

        foreach (var element in array)
        {
            if (element is not TomlTable)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Writes a value in its inline form (the form used for array elements and for values on a key/value line).
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="value">The value to write.</param>
    private static void WriteInlineValue(StringBuilder builder, TomlValue value)
    {
        switch (value)
        {
            case TomlString s:
                WriteBasicString(builder, s.Value);
                break;
            case TomlInteger i:
                builder.Append(i.Value.ToString(CultureInfo.InvariantCulture));
                break;
            case TomlFloat f:
                builder.Append(FormatFloat(f.Value));
                break;
            case TomlBoolean b:
                builder.Append(b.Value ? "true" : "false");
                break;
            case TomlOffsetDateTime odt:
                builder.Append(FormatOffsetDateTime(odt.Value));
                break;
            case TomlLocalDateTime ldt:
                builder.Append(ldt.Value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)).Append(FractionString(ldt.Value.Ticks));
                break;
            case TomlLocalDate ld:
                builder.Append(ld.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            case TomlLocalTime lt:
                builder.Append(lt.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append(FractionString(lt.Value.Ticks));
                break;
            case TomlArray array:
                WriteInlineArray(builder, array);
                break;
            case TomlTable table:
                WriteInlineTable(builder, table);
                break;
        }
    }

    /// <summary>
    /// Writes an array in inline form.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="array">The array.</param>
    private static void WriteInlineArray(StringBuilder builder, TomlArray array)
    {
        builder.Append('[');
        for (var i = 0; i < array.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");
            WriteInlineValue(builder, array[i]);
        }

        builder.Append(']');
    }

    /// <summary>
    /// Writes a table in inline form.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="table">The table.</param>
    private static void WriteInlineTable(StringBuilder builder, TomlTable table)
    {
        if (table.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append("{ ");
        var first = true;
        foreach (var pair in table)
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
    /// <returns>The formatted path.</returns>
    private static string FormatKeyPath(List<string> path)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < path.Count; i++)
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
    /// <param name="key">The key.</param>
    /// <returns>The formatted key.</returns>
    private static string FormatKey(string key)
    {
        if (IsBareKey(key))
            return key;

        var builder = new StringBuilder();
        WriteBasicString(builder, key);
        return builder.ToString();
    }

    /// <summary>
    /// Indicates whether <paramref name="key" /> is a non-empty bare key (<c>A-Za-z0-9_-</c>).
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true" /> when the key may be written without quoting.</returns>
    private static bool IsBareKey(string key)
    {
        if (key.Length == 0)
            return false;

        foreach (var c in key)
        {
            if (!((c is >= 'A' and <= 'Z') || (c is >= 'a' and <= 'z') || (c is >= '0' and <= '9') || c == '_' || c == '-'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Writes a string as a basic (double-quoted) TOML string, escaping control characters and the reserved characters.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="value">The string value.</param>
    private static void WriteBasicString(StringBuilder builder, string value)
    {
        builder.Append('"');
        foreach (var c in value)
        {
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
    /// Formats a double as a valid TOML float, ensuring the result carries a fractional point or exponent and rendering
    /// the special values.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The formatted float.</returns>
    private static string FormatFloat(double value)
    {
        if (double.IsNaN(value))
            return "nan";
        if (double.IsPositiveInfinity(value))
            return "inf";
        if (double.IsNegativeInfinity(value))
            return "-inf";

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0)
            text += ".0";
        return text;
    }

    /// <summary>
    /// Formats an offset date-time in RFC 3339 form.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The formatted instant.</returns>
    private static string FormatOffsetDateTime(DateTimeOffset value)
    {
        var builder = new StringBuilder();
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
    /// <returns>The fractional string, including the leading dot, or an empty string.</returns>
    private static string FractionString(long ticks)
    {
        var fraction = ticks % WriterTicksPerSecond;
        if (fraction == 0)
            return string.Empty;

        return "." + fraction.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
    }
}
