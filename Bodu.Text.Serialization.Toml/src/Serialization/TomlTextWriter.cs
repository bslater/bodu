// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlTextWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Text.Serialization.Toml.Syntax;

namespace Bodu.Text.Serialization.Toml;

/// <summary>
/// Serializes a <see cref="TomlTableSyntax" /> document model to canonical TOML text: inline scalars and arrays first,
/// then sub-tables under <c>[header]</c> sections and arrays of tables under <c>[[header]]</c> sections.
/// </summary>
internal static class TomlTextWriter
{
    /// <summary>
    /// The number of 100-nanosecond ticks in one second.
    /// </summary>
    private const long TicksPerSecond = 10_000_000L;

    /// <summary>
    /// Serializes a table to canonical TOML text.
    /// </summary>
    /// <param name="document">The root table.</param>
    /// <returns>The TOML text.</returns>
    internal static string Write(TomlTableSyntax document)
    {
        StringBuilder builder = new();
        WriteTableBody(builder, document, []);
        return builder.ToString();
    }

    /// <summary>
    /// Writes the contents of a table: inline key/values first, then sub-table and array-of-tables sections.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="table">The table to write.</param>
    /// <param name="path">The header path of the table.</param>
    private static void WriteTableBody(StringBuilder builder, TomlTableSyntax table, List<string> path)
    {
        List<KeyValuePair<string, TomlSyntaxNode>> inlineEntries = [];
        List<KeyValuePair<string, TomlSyntaxNode>> sectionEntries = [];

        foreach (KeyValuePair<string, TomlSyntaxNode> pair in table.Items)
        {
            if (pair.Value is TomlTableSyntax || IsArrayOfTables(pair.Value))
                sectionEntries.Add(pair);
            else
                inlineEntries.Add(pair);
        }

        foreach (KeyValuePair<string, TomlSyntaxNode> entry in inlineEntries)
        {
            builder.Append(FormatKey(entry.Key)).Append(" = ");
            WriteInlineValue(builder, entry.Value);
            builder.Append('\n');
        }

        foreach (KeyValuePair<string, TomlSyntaxNode> entry in sectionEntries)
        {
            List<string> childPath = [.. path, entry.Key];
            var header = FormatKeyPath(childPath);

            if (entry.Value is TomlTableSyntax subTable)
            {
                WriteHeaderLine(builder, "[" + header + "]");
                WriteTableBody(builder, subTable, childPath);
            }
            else
            {
                foreach (TomlSyntaxNode element in ((TomlArraySyntax)entry.Value).Items)
                {
                    WriteHeaderLine(builder, "[[" + header + "]]");
                    WriteTableBody(builder, (TomlTableSyntax)element, childPath);
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
    /// <returns><see langword="true" /> when the value is an array of tables.</returns>
    private static bool IsArrayOfTables(TomlSyntaxNode value)
    {
        if (value is not TomlArraySyntax array || array.Count == 0)
            return false;

        foreach (TomlSyntaxNode element in array.Items)
        {
            if (element is not TomlTableSyntax)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Writes a value in its inline form.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="NotSupportedException">Thrown when the value is of an unsupported kind.</exception>
    private static void WriteInlineValue(StringBuilder builder, TomlSyntaxNode value)
    {
        switch (value)
        {
            case TomlScalarSyntax scalar:
                WriteScalar(builder, scalar);
                break;
            case TomlArraySyntax array:
                WriteInlineArray(builder, array);
                break;
            case TomlTableSyntax table:
                WriteInlineTable(builder, table);
                break;
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Writes a scalar value in its inline form.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="scalar">The scalar to write.</param>
    private static void WriteScalar(StringBuilder builder, TomlScalarSyntax scalar)
    {
        switch (scalar.Kind)
        {
            case TomlSyntaxKind.String:
                WriteBasicString(builder, (string)scalar.Value);
                break;
            case TomlSyntaxKind.Integer:
                builder.Append(((long)scalar.Value).ToString(CultureInfo.InvariantCulture));
                break;
            case TomlSyntaxKind.Float:
                builder.Append(FormatFloat((double)scalar.Value));
                break;
            case TomlSyntaxKind.Boolean:
                builder.Append((bool)scalar.Value ? "true" : "false");
                break;
            case TomlSyntaxKind.OffsetDateTime:
                builder.Append(FormatOffsetDateTime((DateTimeOffset)scalar.Value));
                break;
            case TomlSyntaxKind.LocalDateTime:
                var ldt = (DateTime)scalar.Value;
                builder.Append(ldt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture)).Append(FractionString(ldt.Ticks));
                break;
            case TomlSyntaxKind.LocalDate:
                builder.Append(((DateOnly)scalar.Value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                break;
            default:
                var lt = (TimeOnly)scalar.Value;
                builder.Append(lt.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append(FractionString(lt.Ticks));
                break;
        }
    }

    /// <summary>
    /// Writes an array in inline form.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="array">The array.</param>
    private static void WriteInlineArray(StringBuilder builder, TomlArraySyntax array)
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
    private static void WriteInlineTable(StringBuilder builder, TomlTableSyntax table)
    {
        if (table.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append("{ ");
        var first = true;
        foreach (KeyValuePair<string, TomlSyntaxNode> pair in table.Items)
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
        StringBuilder builder = new();
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

        StringBuilder builder = new();
        WriteBasicString(builder, key);
        return builder.ToString();
    }

    /// <summary>
    /// Indicates whether a key is a non-empty bare key (<c>A-Za-z0-9_-</c>).
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
    /// Writes a string as a basic (double-quoted) TOML string, escaping control and reserved characters.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="value">The string value.</param>
    /// <exception cref="InvalidOperationException">Thrown when the value contains an unpaired surrogate.</exception>
    private static void WriteBasicString(StringBuilder builder, string value)
    {
        builder.Append('"');
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
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
        new(TomlSerializationResourceStrings.Op_Invalid_TomlUnpairedSurrogateWrite);

    /// <summary>
    /// Formats a double as a valid TOML float, ensuring a fractional point or exponent and the special values.
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
    /// <returns>The fractional string, including the leading dot, or an empty string.</returns>
    private static string FractionString(long ticks)
    {
        var fraction = ticks % TicksPerSecond;
        if (fraction == 0)
            return string.Empty;

        return "." + fraction.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
    }
}
