// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnv.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.Text.Formats;

/// <summary>
/// Provides methods for parsing and formatting DotEnv-format configuration text.
/// </summary>
/// <remarks>
/// <para>
/// The parser accepts the most common DotEnv conventions:
/// </para>
/// <list type="bullet">
/// <item><description>Blank lines and lines whose first non-whitespace character is <c>#</c> are ignored.</description></item>
/// <item><description>All other lines are parsed as <c>KEY=VALUE</c> assignments. Keys must match <c>[A-Za-z_][A-Za-z0-9_]*</c>.</description></item>
/// <item><description>Values may be unquoted, single-quoted (literal), or double-quoted (with escape sequences).</description></item>
/// <item><description>Double-quoted values may span multiple lines; single-quoted values may not.</description></item>
/// <item><description>Variable interpolation is not performed at parse time; values are returned as fully resolved strings.</description></item>
/// </list>
/// <para>
/// Dialect variations such as duplicate-key handling, <c>export</c> prefix recognition, and inline comments are
/// controlled via <see cref="DotEnvParseOptions" />.
/// </para>
/// </remarks>
public static partial class DotEnv
{

    /// <summary>
    /// Parses a DotEnv document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <returns>The parsed <see cref="DotEnvDocument" />.</returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static DotEnvDocument Parse(ReadOnlySpan<char> source) =>
        Parse(source, DotEnvParseOptions.Default);

    /// <summary>
    /// Parses a DotEnv document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="DotEnvDocument" />.</returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static DotEnvDocument Parse(ReadOnlySpan<char> source, DotEnvParseOptions options)
    {
        Parser parser = new(source, options);
        return parser.Parse();
    }

    /// <summary>
    /// Renders a <see cref="DotEnvDocument" /> back to DotEnv text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>A string containing the formatted DotEnv representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Empty values are written as <c>KEY=</c> (no quotes). Values containing only safe characters
    /// (<c>[A-Za-z0-9_.,:/@+\-]</c>) are written unquoted. All other values are double-quoted with the
    /// following escape sequences applied: <c>"</c> → <c>\"</c>, <c>\</c> → <c>\\</c>, newline → <c>\n</c>,
    /// tab → <c>\t</c>, carriage return → <c>\r</c>, <c>$</c> → <c>\$</c>.
    /// </para>
    /// <para>
    /// Comments and blank lines from the original source are not preserved because they are not part of the object
    /// model.
    /// </para>
    /// </remarks>
    public static string Format(DotEnvDocument document)
    {
        ThrowHelper.ThrowIfNull(document);

        StringBuilder sb = new();

        foreach (DotEnvEntry entry in document.Entries)
        {
            sb.Append(entry.Key).Append('=');
            AppendFormattedValue(sb, entry.Value);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Attempts to parse a DotEnv document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.
    /// </returns>
    public static bool TryParse(
        ReadOnlySpan<char> source,
        [NotNullWhen(true)] out DotEnvDocument? document) =>
        TryParse(source, DotEnvParseOptions.Default, out document);

    /// <summary>
    /// Attempts to parse a DotEnv document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
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
        DotEnvParseOptions options,
        [NotNullWhen(true)] out DotEnvDocument? document)
    {
        try
        {
            document = Parse(source, options);
            return true;
        }
        catch (DotEnvFormatException)
        {
            document = null;
            return false;
        }
    }

    /// <summary>
    /// Appends the formatted representation of <paramref name="value" /> to <paramref name="sb" />, applying
    /// quoting and escaping as required.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder" /> to append to.</param>
    /// <param name="value">The value string to format.</param>
    private static void AppendFormattedValue(StringBuilder sb, string value)
    {
        if (value.Length == 0)
            return;

        if (IsUnquotable(value))
        {
            sb.Append(value);
            return;
        }

        sb.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"': sb.Append('\\').Append('"'); break;
                case '\\': sb.Append('\\').Append('\\'); break;
                case '\n': sb.Append('\\').Append('n'); break;
                case '\t': sb.Append('\\').Append('t'); break;
                case '\r': sb.Append('\\').Append('r'); break;
                case '$': sb.Append('\\').Append('$'); break;
                default: sb.Append(c); break;
            }
        }

        sb.Append('"');
    }

    /// <summary>
    /// Returns <see langword="true" /> when every character in <paramref name="value" /> is safe to write
    /// unquoted, i.e., matches <c>[A-Za-z0-9_.,:/@+\-]</c>.
    /// </summary>
    /// <param name="value">The non-empty value string to test.</param>
    /// <returns>
    /// <see langword="true" /> if the value requires no quoting; otherwise, <see langword="false" />.
    /// </returns>
    private static bool IsUnquotable(string value)
    {
        foreach (char c in value)
        {
            if (!IsUnquotableChar(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="c" /> is a character that is safe to include in an
    /// unquoted value.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true" /> if <paramref name="c" /> is safe unquoted; otherwise, <see langword="false" />.</returns>
    private static bool IsUnquotableChar(char c) =>
        (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
        c == '_' || c == '.' || c == ',' || c == ':' || c == '/' || c == '@' || c == '+' || c == '-';

}
