// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ini.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.Text.Formats.Ini;

/// <summary>
/// Provides methods for parsing and formatting INI-format configuration text.
/// </summary>
/// <remarks>
/// <para>
/// The parser accepts the most common INI conventions:
/// </para>
/// <list type="bullet">
/// <item><description>Section headers are delimited by <c>[</c> and <c>]</c>.</description></item>
/// <item><description>Key/value pairs are separated by <c>=</c> or <c>:</c>. Leading and trailing whitespace on both the key and value is trimmed.</description></item>
/// <item><description>Lines whose first non-whitespace character is <c>;</c> or <c>#</c> are treated as comments and ignored.</description></item>
/// <item><description>Blank lines are ignored.</description></item>
/// </list>
/// <para>
/// Dialect variations such as case sensitivity, duplicate handling, and global-section permission are controlled
/// via <see cref="IniParseOptions" />.
/// </para>
/// </remarks>
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
    /// Global entries are written first without a section header. A blank line separates each named section from
    /// the preceding content. Key/value pairs are written with a <c> = </c> separator. Comments are not preserved
    /// because they are not part of the object model.
    /// </para>
    /// </remarks>
    public static string Format(IniDocument document)
    {
        ThrowHelper.ThrowIfNull(document);

        StringBuilder sb = new();
        bool needsBlankLine = false;

        foreach (IniEntry entry in document.GlobalSection.Entries)
        {
            sb.Append(entry.Key).Append(" = ").AppendLine(entry.Value);
            needsBlankLine = true;
        }

        foreach (IniSection section in document.Sections)
        {
            if (needsBlankLine)
                sb.AppendLine();

            sb.Append('[').Append(section.Name).AppendLine("]");

            foreach (IniEntry entry in section.Entries)
            {
                sb.Append(entry.Key).Append(" = ").AppendLine(entry.Value);
            }

            needsBlankLine = true;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Attempts to parse an INI document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.
    /// </returns>
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
    /// <returns>
    /// <see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.
    /// </returns>
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
