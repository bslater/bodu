// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationPattern.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Bodu.Text.Configuration;

/// <summary>
/// A compiled EditorConfig-style glob pattern that matches forward-slash-delimited paths.
/// </summary>
/// <remarks>
/// <para>
/// Patterns support the EditorConfig glob grammar:
/// <list type="bullet">
/// <item>
/// <description>
/// <c>*</c> — matches any character except <c>/</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>**</c> — matches any sequence of characters including <c>/</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>?</c> — matches a single character except <c>/</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>{a,b,c}</c> — matches any of the comma-separated alternatives (nesting permitted).
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>{n1..n2}</c> — matches any decimal integer in the inclusive range.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>[seq]</c> — matches a single character in the set; <c>[!seq]</c> matches any character not in the set.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>\</c> — escapes the next character so it is matched literally.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Anchoring follows EditorConfig: a pattern with no <c>/</c> matches at any directory depth; a pattern with <c>/</c>
/// is anchored to the start of the relative path.
/// </para>
/// </remarks>
public sealed partial class BoduConfigurationPattern
{
    private readonly Regex _regex;

    private BoduConfigurationPattern(string source, Regex regex)
    {
        this.Source = source;
        this._regex = regex;
    }

    /// <summary>
    /// Gets the source pattern as authored.
    /// </summary>
    /// <returns>The original glob expression.</returns>
    public string Source { get; }

    /// <summary>
    /// Compiles the supplied glob expression.
    /// </summary>
    /// <param name="pattern">The pattern to compile.</param>
    /// <returns>A compiled <see cref="BoduConfigurationPattern" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> is <see langword="null" />, empty, or contains only whitespace.
    /// </exception>
    /// <exception cref="BoduConfigurationParseException">
    /// The pattern contained an unbalanced brace or bracket.
    /// </exception>
    public static BoduConfigurationPattern Compile(string pattern)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(pattern);

        string regex = TranslateToRegex(pattern);
        return new BoduConfigurationPattern(pattern, new Regex(regex, RegexOptions.CultureInvariant));
    }

    /// <summary>
    /// Determines whether the supplied relative path matches this pattern.
    /// </summary>
    /// <param name="relativePath">The path to test, with forward-slash separators.</param>
    /// <returns><see langword="true" /> when the path matches; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="relativePath" /> is <see langword="null" />.</exception>
    public bool IsMatch(string relativePath)
    {
        ThrowHelper.ThrowIfNull(relativePath);

        string normalized = relativePath.Replace('\\', '/');
        return this._regex.IsMatch(normalized);
    }
}
