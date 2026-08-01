// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPattern.IsMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content> The general wildcard matcher: the classic iterative two-pointer star-backtracking algorithm generalized
/// over the compiled unit sequence. </content>
internal static partial class WildcardPattern
{
    /// <summary>
    /// Determines whether the specified value matches a compiled unit sequence.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="units">The compiled unit sequence, with consecutive stars already collapsed.</param>
    /// <param name="ignoreCase">Whether characters compare case-insensitively (ordinal simple case folding).</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> matches; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The algorithm walks the value once, remembering the most recent star; on a mismatch it rewinds to that star and
    /// widens what the star consumed by one character. With consecutive stars collapsed at compile time this is O(n·m)
    /// worst case and near-linear on typical patterns, without recursion or allocation.
    /// </remarks>
    public static bool IsMatch(ReadOnlySpan<char> value, WildcardUnit[] units, bool ignoreCase)
    {
        // Invariant: value[..t] is matched by units[..u] under some assignment of the stars seen so far, with the
        // most recent star (units[starU]) currently consuming value[starT'..starT] for its tentative width. Only the
        // MOST recent star needs remembering: widening an earlier star can never succeed where widening the latest
        // one fails, because anything an earlier star absorbs the later star could absorb instead.
        var t = 0;
        var u = 0;
        var starU = -1;
        var starT = 0;

        while (t < value.Length)
        {
            if (u < units.Length && units[u].Type == WildcardUnitType.Star)
            {
                // Tentatively let the star consume nothing; remember it for widening on later mismatches.
                starU = u++;
                starT = t;
            }
            else if (u < units.Length && UnitMatches(units[u], value[t], ignoreCase))
            {
                t++;
                u++;
            }
            else if (starU >= 0)
            {
                // Mismatch after a star: widen that star by one consumed character and retry the units after it.
                // starT only ever advances, so each (starT, u) pair is visited at most once — with consecutive stars
                // collapsed at compile time this bounds the whole match at O(n·m) instead of exponential.
                u = starU + 1;
                t = ++starT;
            }
            else
            {
                // Mismatch with no star to widen: nothing can absorb the difference.
                return false;
            }
        }

        // The value is exhausted; any remaining stars may legally consume nothing.
        while (u < units.Length && units[u].Type == WildcardUnitType.Star)
            u++;

        // A full match requires the pattern to be exhausted too — leftover non-star units mean the value was too short.
        return u == units.Length;
    }

    /// <summary>
    /// Determines whether one non-star unit matches one character of the value.
    /// </summary>
    /// <param name="unit">The unit to match with.</param>
    /// <param name="c">The character to test.</param>
    /// <param name="ignoreCase">Whether characters compare case-insensitively.</param>
    /// <returns>
    /// <see langword="true" /> when the character satisfies the unit; otherwise <see langword="false" />.
    /// </returns>
    private static bool UnitMatches(in WildcardUnit unit, char c, bool ignoreCase) =>
        unit.Type switch
        {
            WildcardUnitType.Char => c == unit.Ch || (ignoreCase && char.ToUpperInvariant(c) == char.ToUpperInvariant(unit.Ch)),
            WildcardUnitType.AnyOne => true,
            _ => ClassMatches(unit, c, ignoreCase),
        };

    /// <summary>
    /// Determines whether one character satisfies a character-class unit.
    /// </summary>
    /// <param name="unit">The class unit.</param>
    /// <param name="c">The character to test.</param>
    /// <param name="ignoreCase">Whether membership is tested case-insensitively.</param>
    /// <returns>
    /// <see langword="true" /> when the character satisfies the class; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Case-insensitive membership tests the character and both of its simple case foldings against the raw range
    /// pairs, so <c>[a-z]</c> admits <c>'A'</c> and <c>[!a]</c> rejects <c>'A'</c>. Negation applies after the folded
    /// membership test.
    /// </remarks>
    private static bool ClassMatches(in WildcardUnit unit, char c, bool ignoreCase)
    {
        var pairs = unit.ClassPairs!;
        var inSet = PairsContain(pairs, c);

        if (!inSet && ignoreCase)
        {
            // Fold the CHARACTER rather than the stored ranges: testing both simple case foldings against the raw
            // pairs makes [a-z] admit 'A' and [A-Z] admit 'a' without rewriting range bounds at compile time (which
            // cannot be done faithfully for ranges that straddle case regions, e.g. [X-c]).
            var upper = char.ToUpperInvariant(c);
            if (upper != c)
                inSet = PairsContain(pairs, upper);

            if (!inSet)
            {
                var lower = char.ToLowerInvariant(c);
                if (lower != c)
                    inSet = PairsContain(pairs, lower);
            }
        }

        // Negation applies AFTER folding, so [!a] rejects 'A' case-insensitively instead of accepting it.
        return inSet ^ unit.Negated;
    }

    /// <summary>
    /// Determines whether a character falls within any of the inclusive low/high range pairs.
    /// </summary>
    /// <param name="pairs">The range pairs, two characters per pair.</param>
    /// <param name="c">The character to test.</param>
    /// <returns>
    /// <see langword="true" /> when the character is inside a pair; otherwise <see langword="false" />.
    /// </returns>
    private static bool PairsContain(string pairs, char c)
    {
        for (var i = 0; i < pairs.Length; i += 2)
        {
            if (c >= pairs[i] && c <= pairs[i + 1])
                return true;
        }

        return false;
    }
}
