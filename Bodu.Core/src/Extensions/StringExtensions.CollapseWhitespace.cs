// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.CollapseWhitespace.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every run of one or more white-space characters collapsed to a single
    /// space character (U+0020).
    /// </summary>
    /// <param name="value">The string to collapse. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new string where consecutive white-space characters have been replaced by a single space. When
    /// <paramref name="value" /> contains no runs of two or more white-space characters and no non-space white-space
    /// characters, the original <paramref name="value" /> is returned unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Leading and trailing white-space is collapsed but not removed. Combine with <see cref="string.Trim()" /> to also
    /// strip the edges. White-space is detected via <see cref="char.IsWhiteSpace(char)" /> so Unicode separators (NBSP,
    /// EN SPACE, line terminators, tab) all collapse to a single ASCII space.
    /// </remarks>
    public static string CollapseWhitespace(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return value;

        // Lazy first-mismatch pattern (as RemoveWhitespace and NormalizeLineEndings use): scan without allocating
        // until the first character that must change — a run of two or more white-space characters or a
        // non-space white-space character — so the unchanged path is a true zero-allocation return.
        int firstChange = -1;
        bool prevWasWhite = false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool isWhite = char.IsWhiteSpace(c);
            if (isWhite && (prevWasWhite || c != ' '))
            {
                firstChange = i;
                break;
            }

            prevWasWhite = isWhite;
        }

        if (firstChange < 0) return value;

        var builder = new StringBuilder(value.Length);
        builder.Append(value, 0, firstChange);
        prevWasWhite = firstChange > 0 && char.IsWhiteSpace(value[firstChange - 1]);
        for (int i = firstChange; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsWhiteSpace(c))
            {
                if (!prevWasWhite) builder.Append(' ');
                prevWasWhite = true;
            }
            else
            {
                builder.Append(c);
                prevWasWhite = false;
            }
        }

        return builder.ToString();
    }
}
