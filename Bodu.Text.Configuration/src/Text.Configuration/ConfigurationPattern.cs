// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPattern.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
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
/// Compile once, match many times.
/// ConfigurationPattern csFiles = ConfigurationPattern.Compile("**/*.cs");
/// Console.WriteLine(csFiles.IsMatch("src/Foo.cs"));     // true
/// Console.WriteLine(csFiles.IsMatch("docs/notes.md"));  // false
///
/// Alternation + numeric range.
/// ConfigurationPattern markup = ConfigurationPattern.Compile("**/*.{md,mdx,txt}");
/// ConfigurationPattern logs   = ConfigurationPattern.Compile("logs/run-{1..99}.log");
///]]>
/// </example>
public sealed partial class ConfigurationPattern
{
    /// <summary>
    /// Maximum number of distinct (pattern, comparison) pairs retained in the shared compile cache before the
    /// cache is cleared. The cache exists to amortize regex compilation across repeated resolve calls; the
    /// crude eviction strategy is deliberate — patterns are typically tens, not thousands, and clearing the
    /// cache on overflow keeps memory bounded without an explicit LRU.
    /// </summary>
    private const int CompileCacheCapacity = 512;

    private static readonly ConcurrentDictionary<(string Pattern, StringComparison Comparison), ConfigurationPattern> CompileCache = new();

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
    /// Compiles the supplied glob expression using ordinal (case-sensitive) matching.
    /// </summary>
    /// <param name="pattern">The pattern to compile.</param>
    /// <returns>A compiled <see cref="ConfigurationPattern" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> is <see langword="null" />, empty, or contains only whitespace.
    /// </exception>
    /// <exception cref="ConfigurationParseException">The pattern contained an unbalanced brace or bracket.</exception>
    public static ConfigurationPattern Compile(string pattern) =>
        Compile(pattern, StringComparison.Ordinal);

    /// <summary>
    /// Compiles the supplied glob expression under the requested string comparison. Case-insensitive
    /// comparisons (<see cref="StringComparison.OrdinalIgnoreCase" />,
    /// <see cref="StringComparison.InvariantCultureIgnoreCase" />,
    /// <see cref="StringComparison.CurrentCultureIgnoreCase" />) compile the regex with
    /// <see cref="RegexOptions.IgnoreCase" />; case-sensitive comparisons do not.
    /// </summary>
    /// <param name="pattern">The pattern to compile.</param>
    /// <param name="comparison">The comparison applied during matching.</param>
    /// <returns>A compiled <see cref="ConfigurationPattern" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> is <see langword="null" />, empty, or contains only whitespace.
    /// </exception>
    /// <exception cref="ConfigurationParseException">The pattern contained an unbalanced brace or bracket.</exception>
    /// <remarks>
    /// Compiled patterns are memoized in a bounded process-wide cache so that repeated resolve calls against
    /// the same document avoid re-walking the glob grammar. The cache key is the pair
    /// <c>(pattern, comparison)</c>; on overflow the cache is cleared in bulk.
    /// </remarks>
    public static ConfigurationPattern Compile(string pattern, StringComparison comparison)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(pattern);

        // Reject pathological lengths before consulting the cache or allocating the regex builder. The cache
        // is keyed on the pattern string itself, so over-length inputs must never enter it.
        if (pattern.Length > MaxPatternLength)
        {
            throw new ConfigurationParseException(new ConfigurationDiagnostic(
                ConfigurationDiagnosticSeverity.Error,
                ConfigurationDiagnosticCode.PatternTooLong,
                string.Format(
                    CultureInfo.InvariantCulture,
                    ConfigurationResourceStrings.Format_Invalid_PatternTooLong,
                    pattern.Length,
                    MaxPatternLength),
                ConfigurationSourceLocation.None));
        }

        (string pattern, StringComparison comparison) key = (pattern, comparison);
        if (CompileCache.TryGetValue(key, out ConfigurationPattern? cached))
            return cached;

        // Build outside the cache so that a throwing TranslateToRegex (e.g. UnbalancedBrace) does not poison
        // the dictionary with a faulted Lazy entry.
        RegexOptions options = RegexOptions.CultureInvariant;
        if (IsCaseInsensitive(comparison))
            options |= RegexOptions.IgnoreCase;

        Regex regex = new(TranslateToRegex(pattern), options);
        ConfigurationPattern compiled = new(pattern, regex);

        if (CompileCache.Count > CompileCacheCapacity)
            CompileCache.Clear();

        CompileCache.TryAdd(key, compiled);
        return compiled;
    }

    /// <summary>
    /// Determines whether the supplied <see cref="StringComparison" /> represents a case-insensitive
    /// comparison. The three case-insensitive variants map onto <see cref="RegexOptions.IgnoreCase" />; the
    /// case-sensitive variants leave that flag unset.
    /// </summary>
    /// <param name="comparison">The comparison to classify.</param>
    /// <returns><see langword="true" /> when <paramref name="comparison" /> ignores case.</returns>
    private static bool IsCaseInsensitive(StringComparison comparison) =>
        comparison is StringComparison.OrdinalIgnoreCase
            or StringComparison.CurrentCultureIgnoreCase
            or StringComparison.InvariantCultureIgnoreCase;

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
