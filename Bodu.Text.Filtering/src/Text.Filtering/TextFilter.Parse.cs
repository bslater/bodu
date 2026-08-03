// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilter.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Filtering;

/// <content> Parsing of raw pattern lines using the gitignore file conventions. </content>
public sealed partial class TextFilter
{
    /// <summary>
    /// Builds a filter from raw pattern lines using the gitignore file conventions, with default options.
    /// </summary>
    /// <param name="lines">
    /// The lines to parse. Must not be <see langword="null" /> or contain <see langword="null" /> entries.
    /// </param>
    /// <returns>The compiled filter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lines" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="lines" /> contains a <see langword="null" /> entry, or a parsed pattern is empty or malformed.
    /// </exception>
    /// <remarks>
    /// Each line is trimmed and parsed as a wildcard pattern: blank lines and lines starting with <c>#</c> are skipped,
    /// a leading <c>!</c> makes the pattern an exclude, and <c>\!</c> / <c>\#</c> escape a literal leading <c>!</c> or
    /// <c>#</c>. Regular-expression patterns cannot be expressed in line form — declare them via
    /// <see cref="TextFilterPattern" /> or <see cref="TextFilterBuilder" />.
    /// </remarks>
    public static TextFilter Parse(IEnumerable<string> lines) =>
        Parse(lines, null);

    /// <summary>
    /// Builds a filter from raw pattern lines using the gitignore file conventions.
    /// </summary>
    /// <param name="lines">
    /// The lines to parse. Must not be <see langword="null" /> or contain <see langword="null" /> entries.
    /// </param>
    /// <param name="options">The build options, or <see langword="null" /> for defaults.</param>
    /// <returns>The compiled filter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lines" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="lines" /> contains a <see langword="null" /> entry, or a parsed pattern is empty or malformed.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The options carry an invalid value; see <see cref="Build(IEnumerable{TextFilterPattern}, TextFilterOptions?)" />.
    /// </exception>
    /// <remarks>
    /// Each line is trimmed and parsed as a wildcard pattern: blank lines and lines starting with <c>#</c> are skipped,
    /// a leading <c>!</c> makes the pattern an exclude, and <c>\!</c> / <c>\#</c> escape a literal leading <c>!</c> or
    /// <c>#</c>. Combine with <see cref="TextFilterEvaluationMode.LastMatchWins" /> for gitignore-faithful ordered-rule
    /// semantics.
    /// </remarks>
    public static TextFilter Parse(IEnumerable<string> lines, TextFilterOptions? options)
    {
        ThrowHelper.ThrowIfNull(lines);

        var patterns = new List<TextFilterPattern>();
        foreach (var line in lines)
        {
            if (line is null) throw new ArgumentException(FilteringResourceStrings.Arg_Invalid_PatternsContainsNull, nameof(lines));

            if (TryParseLine(line, out var pattern))
                patterns.Add(pattern);
        }

        return Build(patterns, options);
    }

    /// <summary>
    /// Parses one raw line into a pattern using the gitignore conventions.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <param name="pattern">Receives the parsed pattern when the line carries one.</param>
    /// <returns>
    /// <see langword="true" /> when the line produced a pattern; <see langword="false" /> for blank and comment lines.
    /// </returns>
    /// <exception cref="ArgumentException">The line's pattern text is empty (for example a lone <c>!</c>).</exception>
    internal static bool TryParseLine(string line, [NotNullWhen(true)] out TextFilterPattern? pattern)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed[0] == '#')
        {
            pattern = null;
            return false;
        }

        pattern = trimmed[0] == '!'
            ? TextFilterPattern.Exclude(trimmed[1..])
            : TextFilterPattern.Include(trimmed);

        return true;
    }
}
