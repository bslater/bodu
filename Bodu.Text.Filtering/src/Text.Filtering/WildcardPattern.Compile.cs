// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPattern.Compile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Filtering;

/// <content> Compilation of one brace-free pattern into a <see cref="WildcardProgram" />: tokenization into units, then
/// classification into the cheapest strategy the shape permits. </content>
internal static partial class WildcardPattern
{
    /// <summary>
    /// Compiles one brace-free wildcard pattern into its cost-tier strategy.
    /// </summary>
    /// <param name="pattern">The pattern to compile, with brace alternations already expanded away.</param>
    /// <returns>The compiled program.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> contains an unterminated or empty character class, or ends with a dangling escape
    /// character.
    /// </exception>
    public static WildcardProgram Compile(string pattern)
    {
        var units = Tokenize(pattern);

        return Classify(units);
    }

    /// <summary>
    /// Tokenizes a brace-free pattern into its unit sequence, collapsing consecutive <c>*</c> metacharacters into one
    /// <see cref="WildcardUnitType.Star" /> unit.
    /// </summary>
    /// <param name="pattern">The pattern to tokenize.</param>
    /// <returns>The unit sequence.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pattern" /> contains an unterminated or empty character class, or ends with a dangling escape
    /// character.
    /// </exception>
    private static List<WildcardUnit> Tokenize(string pattern)
    {
        var units = new List<WildcardUnit>(pattern.Length);
        var i = 0;

        while (i < pattern.Length)
        {
            var c = pattern[i];
            switch (c)
            {
                case '\\':
                    if (i == pattern.Length - 1) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_DanglingEscape, pattern), nameof(pattern));

                    units.Add(WildcardUnit.ForChar(pattern[i + 1]));
                    i += 2;
                    break;

                case '*':
                    // Consecutive stars are semantically one star; collapsing them bounds the matcher's backtracking.
                    if (units.Count == 0 || units[^1].Type != WildcardUnitType.Star)
                        units.Add(WildcardUnit.Star);
                    i++;
                    break;

                case '?':
                    units.Add(WildcardUnit.AnyOne);
                    i++;
                    break;

                case '[':
                    units.Add(TokenizeCharacterClass(pattern, ref i));
                    break;

                default:
                    units.Add(WildcardUnit.ForChar(c));
                    i++;
                    break;
            }
        }

        return units;
    }

    /// <summary>
    /// Tokenizes the character class opening at <paramref name="i" /> into a <see cref="WildcardUnitType.Class" />
    /// unit, advancing <paramref name="i" /> past the closing <c>]</c>.
    /// </summary>
    /// <param name="pattern">The pattern being tokenized.</param>
    /// <param name="i">The index of the opening <c>[</c>; advanced past the class on return.</param>
    /// <returns>The class unit.</returns>
    /// <exception cref="ArgumentException">The class is unterminated or has no members.</exception>
    /// <remarks>
    /// Members are single characters or <c>lo-hi</c> ranges; <c>\</c> escapes the next character inside the class, and
    /// a leading <c>!</c> or <c>^</c> negates the class. A <c>-</c> that is the first or last member character is a
    /// literal. A closing <c>]</c> can only be a member when escaped (<c>[\]]</c>). Membership pairs are stored as
    /// inclusive low/high bounds; a range whose low bound exceeds its high bound matches nothing.
    /// </remarks>
    private static WildcardUnit TokenizeCharacterClass(string pattern, ref int i)
    {
        var j = i + 1;
        var negated = false;
        if (j < pattern.Length && (pattern[j] == '!' || pattern[j] == '^'))
        {
            negated = true;
            j++;
        }

        var pairs = new StringBuilder();
        while (j < pattern.Length && pattern[j] != ']')
        {
            var lo = ReadClassCharacter(pattern, ref j);

            // A '-' forms a range only when it sits BETWEEN two members: the member just read and a following
            // character that is not the closing ']'. This one lookahead rule gives '-' its three glob meanings for
            // free — leading ('[-a]') and trailing ('[a-]') dashes fall through to the single-member branch.
            if (j < pattern.Length - 1 && pattern[j] == '-' && pattern[j + 1] != ']')
            {
                j++;
                var hi = ReadClassCharacter(pattern, ref j);
                pairs.Append(lo).Append(hi);
            }
            else
            {
                // Single members are stored as degenerate ranges (lo == hi) so matching needs only one code path.
                pairs.Append(lo).Append(lo);
            }
        }

        if (j >= pattern.Length) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_UnterminatedCharacterClass, pattern), nameof(pattern));
        if (pairs.Length == 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_EmptyCharacterClass, pattern), nameof(pattern));

        i = j + 1;

        return WildcardUnit.ForClass(pairs.ToString(), negated);
    }

    /// <summary>
    /// Reads one member character of a character class, honoring <c>\</c> escapes, and advances <paramref name="j" />.
    /// </summary>
    /// <param name="pattern">The pattern being tokenized.</param>
    /// <param name="j">The index of the member character; advanced past it on return.</param>
    /// <returns>The member character.</returns>
    /// <exception cref="ArgumentException">
    /// An escape at the end of the pattern leaves the class unterminated.
    /// </exception>
    private static char ReadClassCharacter(string pattern, ref int j)
    {
        if (pattern[j] != '\\')
            return pattern[j++];

        if (j == pattern.Length - 1) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FilteringResourceStrings.Arg_Invalid_UnterminatedCharacterClass, pattern), nameof(pattern));

        var c = pattern[j + 1];
        j += 2;

        return c;
    }

    /// <summary>
    /// Classifies a unit sequence into the cheapest strategy its shape permits and materializes the corresponding
    /// <see cref="WildcardProgram" />.
    /// </summary>
    /// <param name="units">The tokenized unit sequence.</param>
    /// <returns>The compiled program.</returns>
    private static WildcardProgram Classify(List<WildcardUnit> units)
    {
        // The cheap strategies below are only valid for patterns made purely of literal characters and stars: a '?'
        // or a class anywhere means the literal fragments are not fixed text, so those shapes go to the general
        // matcher regardless of their star layout.
        var stars = 0;
        var complex = false;
        foreach (var unit in units)
        {
            if (unit.Type == WildcardUnitType.Star)
                stars++;
            else if (unit.Type != WildcardUnitType.Char)
                complex = true;
        }

        if (!complex)
        {
            // With stars collapsed at tokenization, the star COUNT plus the star POSITIONS fully determine the
            // shape: 0 stars = literal; 1 star at an edge = prefix/suffix; 1 star inside = prefix-and-suffix;
            // 2 stars hugging both edges = contains. Everything else (3+ stars, or 2 stars not at both edges)
            // has an unanchored middle segment and needs the general matcher.
            if (stars == 0)
                return new WildcardProgram(WildcardMatchKind.Literal, LiteralRun(units, 0, units.Count), string.Empty, string.Empty, string.Empty, []);

            if (stars == 1 && units.Count == 1)
                return new WildcardProgram(WildcardMatchKind.MatchAll, string.Empty, string.Empty, string.Empty, string.Empty, []);

            if (stars == 1)
            {
                var star = IndexOfStar(units, 0);
                if (star == 0)
                    return new WildcardProgram(WildcardMatchKind.Suffix, string.Empty, string.Empty, LiteralRun(units, 1, units.Count), string.Empty, []);
                if (star == units.Count - 1)
                    return new WildcardProgram(WildcardMatchKind.Prefix, string.Empty, LiteralRun(units, 0, star), string.Empty, string.Empty, []);

                return new WildcardProgram(WildcardMatchKind.PrefixAndSuffix, string.Empty, LiteralRun(units, 0, star), LiteralRun(units, star + 1, units.Count), string.Empty, []);
            }

            if (stars == 2 && units[0].Type == WildcardUnitType.Star && units[^1].Type == WildcardUnitType.Star)
                return new WildcardProgram(WildcardMatchKind.Contains, string.Empty, string.Empty, string.Empty, LiteralRun(units, 1, units.Count - 1), []);
        }

        return new WildcardProgram(WildcardMatchKind.General, string.Empty, string.Empty, string.Empty, string.Empty, [.. units]);
    }

    /// <summary>
    /// Concatenates the characters of a run of <see cref="WildcardUnitType.Char" /> units into a string.
    /// </summary>
    /// <param name="units">The unit sequence.</param>
    /// <param name="start">The inclusive start index of the run.</param>
    /// <param name="end">The exclusive end index of the run.</param>
    /// <returns>The literal text of the run.</returns>
    private static string LiteralRun(List<WildcardUnit> units, int start, int end)
    {
        var chars = new char[end - start];
        for (var i = start; i < end; i++)
            chars[i - start] = units[i].Ch;

        return new string(chars);
    }

    /// <summary>
    /// Finds the index of the first <see cref="WildcardUnitType.Star" /> unit at or after <paramref name="start" />.
    /// </summary>
    /// <param name="units">The unit sequence.</param>
    /// <param name="start">The index to search from.</param>
    /// <returns>The index of the first star unit, or <c>-1</c> when none exists.</returns>
    private static int IndexOfStar(List<WildcardUnit> units, int start)
    {
        for (var i = start; i < units.Count; i++)
        {
            if (units[i].Type == WildcardUnitType.Star)
                return i;
        }

        return -1;
    }
}
