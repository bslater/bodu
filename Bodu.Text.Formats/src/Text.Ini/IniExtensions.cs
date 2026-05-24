// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Ini;

/// <summary>
/// Provides fluent extension methods on <see cref="string" />, <see cref="ReadOnlySpan{T}" /> of <see cref="char" />,
/// and <see cref="IniDocument" /> that route through the canonical <see cref="Ini" /> parser and formatter.
/// </summary>
/// <remarks>
/// <para>
/// These extensions exist purely for ergonomic discoverability — typing <c>text.</c> in an editor brings up
/// <c>ParseIni</c> and <c>TryParseIni</c>, and typing <c>document.</c> on an <see cref="IniDocument" /> brings up
/// <c>FormatIni</c>. They delegate without overhead to the static <see cref="Ini" /> class.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Parse and round-trip via the receiver-typed extensions.
/// IniDocument doc = "[database]\nhost=localhost".ParseIni();
/// string text = doc.FormatIni();
///
/// // Non-throwing variant.
/// if (source.ParseIni() is { Sections.Count: > 0 } parsed) { /* ... */ }
///]]>
/// </example>
public static class IniExtensions
{
    /// <summary>
    /// Parses an INI document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <returns>The parsed <see cref="IniDocument" />.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static IniDocument ParseIni(this ReadOnlySpan<char> source) =>
        Ini.Parse(source);

    /// <summary>
    /// Parses an INI document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="IniDocument" />.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static IniDocument ParseIni(this ReadOnlySpan<char> source, IniParseOptions options) =>
        Ini.Parse(source, options);

    /// <summary>
    /// Parses an INI document from the specified string using default options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <returns>The parsed <see cref="IniDocument" />.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static IniDocument ParseIni(this string source) =>
        Ini.Parse(source);

    /// <summary>
    /// Parses an INI document from the specified string using the supplied options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="IniDocument" />.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static IniDocument ParseIni(this string source, IniParseOptions options) =>
        Ini.Parse(source, options);

    /// <summary>
    /// Attempts to parse an INI document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseIni(
        this ReadOnlySpan<char> source,
        [NotNullWhen(true)] out IniDocument? document) =>
        Ini.TryParse(source, out document);

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
    public static bool TryParseIni(
        this ReadOnlySpan<char> source,
        IniParseOptions options,
        [NotNullWhen(true)] out IniDocument? document) =>
        Ini.TryParse(source, options, out document);

    /// <summary>
    /// Attempts to parse an INI document from the specified string using default options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseIni(
        this string source,
        [NotNullWhen(true)] out IniDocument? document) =>
        Ini.TryParse(source, out document);

    /// <summary>
    /// Attempts to parse an INI document from the specified string using the supplied options.
    /// </summary>
    /// <param name="source">The INI source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseIni(
        this string source,
        IniParseOptions options,
        [NotNullWhen(true)] out IniDocument? document) =>
        Ini.TryParse(source, options, out document);

    /// <summary>
    /// Renders the specified <see cref="IniDocument" /> back to INI text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>A string containing the formatted INI representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    public static string FormatIni(this IniDocument document) =>
        Ini.Format(document);
}
