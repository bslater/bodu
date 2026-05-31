// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Delimited;

/// <summary>
/// Provides fluent extension methods on <see cref="string" />, <see cref="ReadOnlySpan{T}" /> of <see cref="char" />,
/// and <see cref="DelimitedDocument" /> that route through the canonical <see cref="Delimited" /> parser and formatter.
/// </summary>
/// <remarks>
/// <para>
/// These extensions exist purely for ergonomic discoverability — typing <c>text.</c> in an editor brings up
/// <c>ParseDelimited</c> and <c>TryParseDelimited</c>, and typing <c>document.</c> on a
/// <see cref="DelimitedDocument" /> brings up <c>FormatDelimited</c>. They delegate without overhead to the static
/// <see cref="Delimited" /> class.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Parse a CSV document and round-trip back to text via the receiver-typed extensions.
/// DelimitedDocument doc = "name,age\nAda,42".ParseDelimited();
/// string text = doc.FormatDelimited();
///]]>
/// </example>
public static class DelimitedExtensions
{
    /// <summary>
    /// Parses a delimited-text document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <returns>The parsed <see cref="DelimitedDocument" />.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when <paramref name="source" /> contains a structural error, such as an unterminated quoted field.
    /// </exception>
    public static DelimitedDocument ParseDelimited(this ReadOnlySpan<char> source) =>
        Delimited.Parse(source);

    /// <summary>
    /// Parses a delimited-text document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="DelimitedDocument" />.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when <paramref name="source" /> contains a structural error.
    /// </exception>
    public static DelimitedDocument ParseDelimited(this ReadOnlySpan<char> source, DelimitedParseOptions options) =>
        Delimited.Parse(source, options);

    /// <summary>
    /// Parses a delimited-text document from the specified string using default options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <returns>The parsed <see cref="DelimitedDocument" />.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when <paramref name="source" /> contains a structural error, such as an unterminated quoted field.
    /// </exception>
    public static DelimitedDocument ParseDelimited(this string source) =>
        Delimited.Parse(source);

    /// <summary>
    /// Parses a delimited-text document from the specified string using the supplied options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="DelimitedDocument" />.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when <paramref name="source" /> contains a structural error.
    /// </exception>
    public static DelimitedDocument ParseDelimited(this string source, DelimitedParseOptions options) =>
        Delimited.Parse(source, options);

    /// <summary>
    /// Attempts to parse a delimited-text document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseDelimited(
        this ReadOnlySpan<char> source,
        [NotNullWhen(true)] out DelimitedDocument? document) =>
        Delimited.TryParse(source, out document);

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
    public static bool TryParseDelimited(
        this ReadOnlySpan<char> source,
        DelimitedParseOptions options,
        [NotNullWhen(true)] out DelimitedDocument? document) =>
        Delimited.TryParse(source, options, out document);

    /// <summary>
    /// Attempts to parse a delimited-text document from the specified string using default options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseDelimited(
        this string source,
        [NotNullWhen(true)] out DelimitedDocument? document) =>
        Delimited.TryParse(source, out document);

    /// <summary>
    /// Attempts to parse a delimited-text document from the specified string using the supplied options.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseDelimited(
        this string source,
        DelimitedParseOptions options,
        [NotNullWhen(true)] out DelimitedDocument? document) =>
        Delimited.TryParse(source, options, out document);

    /// <summary>
    /// Renders the specified <see cref="DelimitedDocument" /> back to delimited text using default options.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>A string containing the formatted representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    public static string FormatDelimited(this DelimitedDocument document) =>
        Delimited.Format(document);

    /// <summary>
    /// Renders the specified <see cref="DelimitedDocument" /> back to delimited text using the supplied options.
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
    public static string FormatDelimited(this DelimitedDocument document, DelimitedParseOptions options) =>
        Delimited.Format(document, options);
}
