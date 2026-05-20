// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.SplitLines.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a sequence of lines from <paramref name="value" />, splitting on every CRLF, CR, or LF boundary.
    /// </summary>
    /// <param name="value">The string to split. Must not be <see langword="null" />.</param>
    /// <param name="removeEmptyLines">
    /// When <see langword="true" />, empty lines are skipped. Defaults to <see langword="false" /> so callers receive a
    /// 1:1 mapping with the original line boundaries.
    /// </param>
    /// <returns>
    /// An enumerable yielding each line of <paramref name="value" /> with the terminator removed. An empty input
    /// returns an empty sequence.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// CRLF is treated as a single line ending. Unlike <see cref="string.Split(char[])" />, a trailing line terminator
    /// does not produce a final empty line — the contract matches typical "lines in a file" reading semantics.
    /// </remarks>
    public static IEnumerable<string> SplitLines(this string value, bool removeEmptyLines = false)
    {
        ThrowHelper.ThrowIfNull(value);

        return SplitLinesIterator(value, removeEmptyLines);
    }

    /// <summary>
    /// Iterates the lines of <paramref name="value" />, honouring CRLF/CR/LF boundaries and respecting the
    /// <paramref name="removeEmptyLines" /> filter.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <param name="removeEmptyLines">Whether to skip empty lines.</param>
    /// <returns>The line sequence.</returns>
    private static IEnumerable<string> SplitLinesIterator(string value, bool removeEmptyLines)
    {
        if (value.Length == 0) yield break;

        int lineStart = 0;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\r' || c == '\n')
            {
                string line = value.Substring(lineStart, i - lineStart);
                if (!removeEmptyLines || line.Length > 0) yield return line;

                if (c == '\r' && i + 1 < value.Length && value[i + 1] == '\n') i++;
                lineStart = i + 1;
            }
        }

        if (lineStart < value.Length)
        {
            string tail = value.Substring(lineStart);
            if (!removeEmptyLines || tail.Length > 0) yield return tail;
        }
    }
}
