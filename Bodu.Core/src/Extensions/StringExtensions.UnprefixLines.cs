// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.UnprefixLines.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with one occurrence of <paramref name="prefix" /> removed from the start of
    /// every line that begins with it.
    /// </summary>
    /// <param name="value">The source text. Must not be <see langword="null" />.</param>
    /// <param name="prefix">The prefix to strip from each line. Must not be <see langword="null" />.</param>
    /// <returns>The text with the prefix removed from each prefixed line.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either argument is <see langword="null" />.</exception>
    /// <remarks>
    /// The inverse of <see cref="PrefixLines(string, string)" />. Lines that do not begin with
    /// <paramref name="prefix" /> are emitted unchanged. Comparison is ordinal. Line boundaries follow the usual
    /// <c>\r\n</c> / <c>\n</c> / <c>\r</c> recognition.
    /// </remarks>
    public static string UnprefixLines(this string value, string prefix)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(prefix);

        if (prefix.Length == 0 || value.Length == 0) return value;

        StringBuilder builder = new(value.Length);
        int lineStart = 0;
        for (int i = 0; i <= value.Length; i++)
        {
            if (i == value.Length || value[i] == '\n' || value[i] == '\r')
            {
                int start = lineStart;
                int end = i;
                if (end - start >= prefix.Length
                    && string.CompareOrdinal(value, start, prefix, 0, prefix.Length) == 0)
                {
                    start += prefix.Length;
                }

                if (start < end) builder.Append(value, start, end - start);
                if (i == value.Length) break;
                builder.Append(value[i]);
                if (value[i] == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
                {
                    builder.Append('\n');
                    i++;
                }

                lineStart = i + 1;
            }
        }

        return builder.ToString();
    }
}
