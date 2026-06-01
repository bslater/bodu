// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Delimited.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.Text.Delimited;

/// <summary>
/// Provides methods for parsing and formatting delimited-text documents (CSV, TSV, and similar formats).
/// </summary>
/// <remarks>
/// <para>
/// The parser conforms to RFC 4180 with the following extensions:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// The delimiter, quote character, and header behaviour are configurable via <see cref="DelimitedParseOptions" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// Quoted fields may span multiple lines; literal newlines are preserved.
/// </description>
/// </item>
/// <item>
/// <description>
/// Two consecutive quote characters inside a quoted field represent a single literal quote.
/// </description>
/// </item>
/// <item>
/// <description>
/// Blank lines (zero characters before the newline) are silently skipped.
/// </description>
/// </item>
/// <item>
/// <description>
/// Optionally, lines whose first character is a configurable comment character are skipped.
/// </description>
/// </item>
/// </list>
/// </remarks>
/// <example>
///<![CDATA[
/// Parse a CSV document with a header row (the default).
/// DelimitedDocument doc = Delimited.Parse("""
///     name,age,city
///     Ada,42,London
///     Linus,55,Helsinki
///     """);
///
/// foreach (DelimitedRow row in doc.Rows)
///     Console.WriteLine($"{row["name"]} ({row.GetInt32("age")}) — {row["city"]}");
///
/// TSV without a header.
/// DelimitedDocument tsv = Delimited.Parse(source, new DelimitedParseOptions
/// {
///     Delimiter  = '\t',
///     HasHeader  = false,
/// });
///]]>
/// </example>
public static partial class Delimited
{
    /// <summary>
    /// Parses a delimited-text document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <returns>The parsed <see cref="DelimitedDocument" />.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when <paramref name="source" /> contains a structural error, such as an unterminated quoted field.
    /// </exception>
    public static DelimitedDocument Parse(ReadOnlySpan<char> source) =>
        Parse(source, DelimitedParseOptions.Default);

    /// <summary>
    /// Parses a delimited-text document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="DelimitedDocument" />.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when <paramref name="source" /> contains a structural error.
    /// </exception>
    public static DelimitedDocument Parse(ReadOnlySpan<char> source, DelimitedParseOptions options)
    {
        Parser parser = new(source, options);
        return parser.Parse();
    }

    /// <summary>
    /// Renders a <see cref="DelimitedDocument" /> back to delimited text using default options (comma delimiter,
    /// double-quote character).
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>A string containing the formatted representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    public static string Format(DelimitedDocument document) =>
        Format(document, DelimitedParseOptions.Default);

    /// <summary>
    /// Renders a <see cref="DelimitedDocument" /> back to delimited text using the supplied options.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <param name="options">
    /// Options whose <see cref="DelimitedParseOptions.Delimiter" /> and <see cref="DelimitedParseOptions.Quote" />
    /// properties govern the output format.
    /// </param>
    /// <returns>A string containing the formatted representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Fields that contain the delimiter character, the quote character, a carriage return, or a line feed are
    /// surrounded with the quote character; any literal occurrences of the quote character within such a field are
    /// doubled. Fields that require no quoting are written verbatim.
    /// </para>
    /// <para>
    /// When <see cref="DelimitedDocument.Headers" /> is non-empty, a header row is written first. Comment lines and
    /// blank lines from the original source are not preserved because they are not part of the object model.
    /// </para>
    /// </remarks>
    public static string Format(DelimitedDocument document, DelimitedParseOptions options)
    {
        ThrowHelper.ThrowIfNull(document);

        var delimiter = options.Delimiter;
        var quote = options.Quote;

        StringBuilder sb = new();

        if (document.Headers.Count > 0)
        {
            WriteRow(sb, document.Headers, delimiter, quote);
            sb.Append('\n');
        }

        foreach (DelimitedRow row in document.Rows)
        {
            WriteRow(sb, row.Fields, delimiter, quote);
            sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a forward-only <see cref="DelimitedWriter" /> that writes to the specified <see cref="TextWriter" />
    /// using default options.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="TextWriter" /> to write delimited text to. Owned by the returned writer.
    /// </param>
    /// <returns>A <see cref="DelimitedWriter" /> ready to accept rows.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static DelimitedWriter CreateWriter(TextWriter writer) =>
        new(writer);

    /// <summary>
    /// Creates a forward-only <see cref="DelimitedWriter" /> that writes to the specified <see cref="TextWriter" />
    /// using the supplied options.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="TextWriter" /> to write delimited text to. Owned by the returned writer.
    /// </param>
    /// <param name="options">
    /// Options whose <see cref="DelimitedParseOptions.Delimiter" /> and <see cref="DelimitedParseOptions.Quote" />
    /// properties govern the output format.
    /// </param>
    /// <returns>A <see cref="DelimitedWriter" /> ready to accept rows.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static DelimitedWriter CreateWriter(TextWriter writer, DelimitedParseOptions options) =>
        new(writer, options);

    /// <summary>
    /// Creates a forward-only <see cref="DelimitedReader" /> that reads from the specified <see cref="TextReader" />
    /// using default options.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="TextReader" /> to read delimited text from. Owned by the returned reader.
    /// </param>
    /// <returns>A <see cref="DelimitedReader" /> positioned before the first row.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public static DelimitedReader CreateReader(TextReader reader) =>
        new(reader);

    /// <summary>
    /// Creates a forward-only <see cref="DelimitedReader" /> that reads from the specified <see cref="TextReader" />
    /// using the supplied options.
    /// </summary>
    /// <param name="reader">
    /// The <see cref="TextReader" /> to read delimited text from. Owned by the returned reader.
    /// </param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>A <see cref="DelimitedReader" /> positioned before the first row.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public static DelimitedReader CreateReader(TextReader reader, DelimitedParseOptions options) =>
        new(reader, options);

    /// <summary>
    /// Attempts to parse a delimited-text document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> source,
        [NotNullWhen(true)] out DelimitedDocument? document) =>
        TryParse(source, DelimitedParseOptions.Default, out document);

    /// <summary>
    /// Attempts to parse a delimited-text document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> source,
        DelimitedParseOptions options,
        [NotNullWhen(true)] out DelimitedDocument? document)
    {
        try
        {
            document = Parse(source, options);
            return true;
        }
        catch (DelimitedFormatException)
        {
            document = null;
            return false;
        }
    }

    /// <summary>
    /// Writes the fields of one row to <paramref name="sb" />, separating them with <paramref name="delimiter" /> and
    /// quoting where required.
    /// </summary>
    /// <param name="sb">The builder to append to.</param>
    /// <param name="fields">The field values to write.</param>
    /// <param name="delimiter">The field separator character.</param>
    /// <param name="quote">The quoting character.</param>
    private static void WriteRow(StringBuilder sb, IReadOnlyList<string> fields, char delimiter, char quote)
    {
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0)
                sb.Append(delimiter);

            AppendField(sb, fields[i], delimiter, quote);
        }
    }

    /// <summary>
    /// Appends a single field value to <paramref name="sb" />, quoting and doubling quote characters as required.
    /// </summary>
    /// <param name="sb">The builder to append to.</param>
    /// <param name="field">The field value to append.</param>
    /// <param name="delimiter">The field separator character.</param>
    /// <param name="quote">The quoting character.</param>
    private static void AppendField(StringBuilder sb, string field, char delimiter, char quote)
    {
        var needsQuoting = field.Length == 0 ||
                            field.IndexOf(delimiter) >= 0 ||
                            field.IndexOf(quote) >= 0 ||
                            field.IndexOf('\n') >= 0 ||
                            field.IndexOf('\r') >= 0;

        if (!needsQuoting)
        {
            sb.Append(field);
            return;
        }

        sb.Append(quote);

        foreach (var c in field)
        {
            if (c == quote)
                sb.Append(quote); // RFC 4180 doubled-quote escape

            sb.Append(c);
        }

        sb.Append(quote);
    }
}
