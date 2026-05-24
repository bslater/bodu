// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPattern.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
/// <para>
/// Compilation parses the glob once into a culture-invariant <see cref="Regex" />; subsequent
/// <see cref="IsMatch(string)" /> calls are allocation-free over the compiled state. Cache compiled patterns when the
/// same glob is matched repeatedly against many paths.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Compile once, match many times.
/// ConfigurationPattern csFiles = ConfigurationPattern.Compile("**/*.cs");
/// Console.WriteLine(csFiles.IsMatch("src/Foo.cs"));     // true
/// Console.WriteLine(csFiles.IsMatch("docs/notes.md"));  // false
///
/// // Alternation + numeric range.
/// ConfigurationPattern markup = ConfigurationPattern.Compile("**/*.{md,mdx,txt}");
/// ConfigurationPattern logs   = ConfigurationPattern.Compile("logs/run-{1..99}.log");
///]]>
/// </example>
public sealed partial class ConfigurationPattern
{
    private readonly Regex _regex;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPattern" /> class with the supplied source and
    /// compiled regular expression.
    /// </summary>
    /// <param name="source">The glob expression as authored.</param>
    /// <param name="regex">The compiled regular expression equivalent to <paramref name="source" />.</param>
    private ConfigurationPattern(string source, Regex regex)
    {
        Source = source;
        _regex = regex;
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
    /// <returns>A compiled <see cref="ConfigurationPattern" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> is <see langword="null" />, empty, or contains only whitespace.
    /// </exception>
    /// <exception cref="ConfigurationParseException">The pattern contained an unbalanced brace or bracket.</exception>
    public static ConfigurationPattern Compile(string pattern)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(pattern);

        var regex = TranslateToRegex(pattern);

        return new ConfigurationPattern(pattern, new Regex(regex, RegexOptions.CultureInvariant));
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

        var normalized = relativePath.Replace('\\', '/');
        return _regex.IsMatch(normalized);
    }
}
