// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.NormalizeLineEndings.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every CRLF (<c>\r\n</c>), CR (<c>\r</c>), and LF (<c>\n</c>)
    /// sequence replaced by <paramref name="newline" />.
    /// </summary>
    /// <param name="value">The string to normalize. Must not be <see langword="null" />.</param>
    /// <param name="newline">
    /// The replacement line terminator. Defaults to <c>"\n"</c> (LF). May be empty to strip all line endings.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// A new string in which every recognised line ending in <paramref name="value" /> has been replaced by
    /// <paramref name="newline" />. When <paramref name="value" /> contains no line endings, the original
    /// instance is returned unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="newline" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// CRLF pairs are treated as a single line ending and replaced by exactly one <paramref name="newline" />.
    /// Other Unicode line separators (U+2028, U+2029, NEL U+0085) are not touched.
    /// </remarks>
    public static string NormalizeLineEndings(this string value, string newline = "\n")
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(newline);

        if (value.Length == 0) return value;

        StringBuilder? builder = null;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\r')
            {
                builder ??= new StringBuilder(value.Length).Append(value, 0, i);
                builder.Append(newline);
                if (i + 1 < value.Length && value[i + 1] == '\n') i++;
            }
            else if (c == '\n')
            {
                builder ??= new StringBuilder(value.Length).Append(value, 0, i);
                builder.Append(newline);
            }
            else
            {
                builder?.Append(c);
            }
        }

        return builder is null ? value : builder.ToString();
    }
}
