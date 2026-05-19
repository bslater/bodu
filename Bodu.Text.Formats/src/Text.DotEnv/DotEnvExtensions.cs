// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Provides fluent extension methods on <see cref="string" />, <see cref="ReadOnlySpan{T}" /> of <see cref="char" />,
/// and <see cref="DotEnvDocument" /> that route through the canonical <see cref="DotEnv" /> parser and formatter.
/// </summary>
/// <remarks>
/// <para>
/// These extensions exist purely for ergonomic discoverability — typing <c>text.</c> in an editor brings up
/// <c>ParseDotEnv</c> and <c>TryParseDotEnv</c>, and typing <c>document.</c> on a <see cref="DotEnvDocument" /> brings
/// up <c>FormatDotEnv</c>. They delegate without overhead to the static <see cref="DotEnv" /> class.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Parse and round-trip via the receiver-typed extensions.
/// DotEnvDocument doc = "PORT=8080\nHOST=localhost".ParseDotEnv();
/// string text = doc.FormatDotEnv();
///]]>
/// </example>
public static class DotEnvExtensions
{
    /// <summary>
    /// Parses a DotEnv document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <returns>The parsed <see cref="DotEnvDocument" />.</returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static DotEnvDocument ParseDotEnv(this ReadOnlySpan<char> source) =>
        DotEnv.Parse(source);

    /// <summary>
    /// Parses a DotEnv document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="DotEnvDocument" />.</returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static DotEnvDocument ParseDotEnv(this ReadOnlySpan<char> source, DotEnvParseOptions options) =>
        DotEnv.Parse(source, options);

    /// <summary>
    /// Parses a DotEnv document from the specified string using default options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <returns>The parsed <see cref="DotEnvDocument" />.</returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static DotEnvDocument ParseDotEnv(this string source) =>
        DotEnv.Parse(source);

    /// <summary>
    /// Parses a DotEnv document from the specified string using the supplied options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>The parsed <see cref="DotEnvDocument" />.</returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when <paramref name="source" /> is malformed or violates the configured parsing policy.
    /// </exception>
    public static DotEnvDocument ParseDotEnv(this string source, DotEnvParseOptions options) =>
        DotEnv.Parse(source, options);

    /// <summary>
    /// Attempts to parse a DotEnv document from the specified character span using default options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseDotEnv(
        this ReadOnlySpan<char> source,
        [NotNullWhen(true)] out DotEnvDocument? document) =>
        DotEnv.TryParse(source, out document);

    /// <summary>
    /// Attempts to parse a DotEnv document from the specified character span using the supplied options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseDotEnv(
        this ReadOnlySpan<char> source,
        DotEnvParseOptions options,
        [NotNullWhen(true)] out DotEnvDocument? document) =>
        DotEnv.TryParse(source, options, out document);

    /// <summary>
    /// Attempts to parse a DotEnv document from the specified string using default options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseDotEnv(
        this string source,
        [NotNullWhen(true)] out DotEnvDocument? document) =>
        DotEnv.TryParse(source, out document);

    /// <summary>
    /// Attempts to parse a DotEnv document from the specified string using the supplied options.
    /// </summary>
    /// <param name="source">The DotEnv source text.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, contains the parsed document; otherwise,
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseDotEnv(
        this string source,
        DotEnvParseOptions options,
        [NotNullWhen(true)] out DotEnvDocument? document) =>
        DotEnv.TryParse(source, options, out document);

    /// <summary>
    /// Renders the specified <see cref="DotEnvDocument" /> back to DotEnv text.
    /// </summary>
    /// <param name="document">The document to format.</param>
    /// <returns>A string containing the formatted DotEnv representation of <paramref name="document" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="document" /> is <see langword="null" />.
    /// </exception>
    public static string FormatDotEnv(this DotEnvDocument document) =>
        DotEnv.Format(document);
}
