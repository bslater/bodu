// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ini.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.Text.Ini;

/// <summary>
/// Provides methods for parsing and formatting INI-format configuration text.
/// </summary>
/// <remarks>
/// <para>
/// The parser accepts the most common INI conventions:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Section headers are delimited by <c>[</c> and <c>]</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Key/value pairs are separated by <c>=</c> or <c>:</c>. Leading and trailing whitespace on both the key and value is
/// trimmed.
/// </description>
/// </item>
/// <item>
/// <description>
/// Lines whose first non-whitespace character is <c>;</c> or <c>#</c> are treated as comments and ignored.
/// </description>
/// </item>
/// <item>
/// <description>
/// Blank lines are ignored.
/// </description>
/// </item>
/// </list>
/// <para>
/// Dialect variations such as case sensitivity, duplicate handling, and global-section permission are controlled via
/// <see cref="IniParseOptions" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Parse a document and read a typed value out of a known section.
/// IniDocument doc = Ini.Parse("""
///     [database]
///     host=localhost
///     port=5432
///     """);
///
/// IniSection db = doc["database"];
/// string host  = db["host"];
/// int    port  = db.GetInt32("port", fallback: 5432);
///
/// Round-trip: emit the document back to a string.
/// string text = Ini.Format(doc);
///
/// Dialect override: case-sensitive section names.
/// IniDocument strict = Ini.Parse(source, new IniParseOptions { CaseSensitiveSections = true });
///]]>
/// </example>
public static partial class Ini
{
    /// <summary>
    /// Parses an INI document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <returns>The parsed <see cref="IniDocument" />.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static IniDocument Parse(ReadOnlySpan<char> source) =>
        Parse(source, IniParseOptions.Default);

    /// <summary>
    /// Parses an INI document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="IniDocument" />.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static IniDocument Parse(ReadOnlySpan<char> source, IniParseOptions options)
    {
        Parser parser = new(source, options);
        return parser.Parse();
    }

    /// <summary>
    /// Renders an <see cref="IniDocument" /> back to INI text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>A string containing the formatted INI representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Global entries are written first without a section header. A blank line separates each named section from the
    /// preceding content. Key/value pairs are written with a <c> = </c> separator. Comments are not preserved because
    /// they are not part of the object model.
    /// </para>
    /// </remarks>
    public static string Format(IniDocument document)
    {
        ThrowHelper.ThrowIfNull(document);

        StringBuilder sb = new();
        var needsBlankLine = false;

        WriteEntries(sb, document.GlobalSection.Entries);
        if (document.GlobalSection.Entries.Count > 0)
            needsBlankLine = true;

        foreach (IniSection section in document.Sections)
        {
            if (needsBlankLine)
                sb.AppendLine();

            foreach (IniComment comment in section.LeadingComments)
                sb.Append(comment.Prefix).AppendLine(comment.Text);

            sb.Append('[').Append(section.Name).AppendLine("]");

            WriteEntries(sb, section.Entries);

            needsBlankLine = true;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Writes the supplied entries to <paramref name="sb" />, including any leading-comment trivia and an optional
    /// inline comment captured on each entry.
    /// </summary>
    /// <param name="sb">The destination buffer.</param>
    /// <param name="entries">The entries to emit, in order.</param>
    private static void WriteEntries(StringBuilder sb, IReadOnlyList<IniEntry> entries)
    {
        foreach (IniEntry entry in entries)
        {
            foreach (IniComment comment in entry.LeadingComments)
                sb.Append(comment.Prefix).AppendLine(comment.Text);

            sb.Append(entry.Key).Append(" = ").Append(entry.Value);
            if (entry.InlineComment is { } inline)
                sb.Append(' ').Append(inline.Prefix).Append(inline.Text);
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Attempts to parse an INI document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> source,
        [NotNullWhen(true)] out IniDocument? document) =>
        TryParse(source, IniParseOptions.Default, out document);

    /// <summary>
    /// Attempts to parse an INI document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> source,
        IniParseOptions options,
        [NotNullWhen(true)] out IniDocument? document)
    {
        try
        {
            document = Parse(source, options);
            return true;
        }
        catch (IniFormatException)
        {
            document = null;
            return false;
        }
    }
}
