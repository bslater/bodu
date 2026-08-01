// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPattern.ExpandBraces.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Filtering;

/// <content> Brace-alternation expansion: rewrites <c>{a,b}</c> groups into the flat list of alternative patterns.
/// </content>
internal static partial class WildcardPattern
{
    /// <summary>
    /// Expands every brace alternation in <paramref name="pattern" /> into the flat list of alternative patterns.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to expand.</param>
    /// <returns>
    /// The expanded alternatives in left-to-right order; a single-element list containing <paramref name="pattern" />
    /// itself when it holds no alternation.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> contains an unterminated <c>{</c> group, nests groups deeper than
    /// <see cref="MaxBraceNestingDepth" />, or expands to more than <see cref="MaxBraceExpansion" /> alternatives.
    /// </exception>
    /// <remarks>
    /// Escaped braces (<c>\{</c>, <c>\}</c>) and braces inside character classes are treated as literal content. A
    /// group with no comma still expands (<c>{abc}</c> becomes <c>abc</c>), and empty alternatives are legal (<c>a{b,}</c>
    /// expands to <c>ab</c> and <c>a</c>). An unmatched closing <c>}</c> is a literal character.
    /// </remarks>
    public static IReadOnlyList<string> ExpandBraces(string pattern)
    {
        if (!pattern.Contains('{', StringComparison.Ordinal))
            return [pattern];

        var results = new List<string>();
        ExpandInto(pattern, pattern, results);

        return results;
    }

    /// <summary>
    /// Recursively expands the first brace group of <paramref name="candidate" /> and accumulates fully expanded
    /// alternatives into <paramref name="results" />.
    /// </summary>
    /// <param name="pattern">The originally supplied pattern, used in error messages.</param>
    /// <param name="candidate">The partially expanded pattern to continue expanding.</param>
    /// <param name="results">The accumulator receiving fully expanded alternatives.</param>
    private static void ExpandInto(string pattern, string candidate, List<string> results)
    {
        var open = FindFirstBrace(candidate);
        if (open < 0)
        {
            if (results.Count >= MaxBraceExpansion) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_BraceExpansionTooLarge, pattern, MaxBraceExpansion), nameof(pattern));

            results.Add(candidate);
            return;
        }

        var close = FindMatchingClose(pattern, candidate, open, out List<int> commas);
        var suffixStart = close + 1;
        var prefix = candidate[..open];
        var suffix = candidate[suffixStart..];

        // Alternative segments are delimited by the group's top-level commas; a comma-free group is one alternative.
        var segmentStart = open + 1;
        for (var i = 0; i <= commas.Count; i++)
        {
            var segmentEnd = i < commas.Count ? commas[i] : close;
            ExpandInto(pattern, string.Concat(prefix, candidate[segmentStart..segmentEnd], suffix), results);
            segmentStart = segmentEnd + 1;
        }
    }

    /// <summary>
    /// Finds the index of the first unescaped <c>{</c> that is not inside a character class, or <c>-1</c> when none
    /// exists.
    /// </summary>
    /// <param name="candidate">The pattern text to scan.</param>
    /// <returns>The index of the first expandable <c>{</c>, or <c>-1</c>.</returns>
    private static int FindFirstBrace(string candidate)
    {
        for (var i = 0; i < candidate.Length; i++)
        {
            switch (candidate[i])
            {
                case '\\':
                    i++;
                    break;

                case '[':
                    i = SkipCharacterClass(candidate, i);
                    break;

                case '{':
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the closing <c>}</c> matching the group opened at <paramref name="open" /> and records the positions of
    /// the group's top-level commas.
    /// </summary>
    /// <param name="pattern">The originally supplied pattern, used in error messages.</param>
    /// <param name="candidate">The pattern text being scanned.</param>
    /// <param name="open">The index of the opening <c>{</c>.</param>
    /// <param name="commas">Receives the indexes of commas at the group's own nesting level.</param>
    /// <returns>The index of the matching <c>}</c>.</returns>
    /// <exception cref="ArgumentException">
    /// The group is unterminated, or nests deeper than <see cref="MaxBraceNestingDepth" />.
    /// </exception>
    private static int FindMatchingClose(string pattern, string candidate, int open, out List<int> commas)
    {
        commas = [];
        var depth = 1;

        for (var i = open + 1; i < candidate.Length; i++)
        {
            switch (candidate[i])
            {
                case '\\':
                    i++;
                    break;

                case '[':
                    i = SkipCharacterClass(candidate, i);
                    break;

                case '{':
                    depth++;
                    if (depth > MaxBraceNestingDepth) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_BraceNestingTooDeep, pattern, MaxBraceNestingDepth), nameof(pattern));
                    break;

                case '}':
                    depth--;
                    if (depth == 0)
                        return i;
                    break;

                case ',':
                    if (depth == 1)
                        commas.Add(i);
                    break;
            }
        }

        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_UnterminatedBrace, pattern), nameof(pattern));
    }

    /// <summary>
    /// Advances past a character class opened at <paramref name="start" />, returning the index of its closing <c>]</c>
    /// (or the final index of the text when the class is unterminated — the compiler reports that error).
    /// </summary>
    /// <param name="candidate">The pattern text being scanned.</param>
    /// <param name="start">The index of the opening <c>[</c>.</param>
    /// <returns>The index scanning should continue after.</returns>
    private static int SkipCharacterClass(string candidate, int start)
    {
        var i = start + 1;
        if (i < candidate.Length && (candidate[i] == '!' || candidate[i] == '^'))
            i++;

        while (i < candidate.Length && candidate[i] != ']')
            i += candidate[i] == '\\' ? 2 : 1;

        return i < candidate.Length ? i : candidate.Length - 1;
    }
}
