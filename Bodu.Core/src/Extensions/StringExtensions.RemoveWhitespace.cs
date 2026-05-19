// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveWhitespace.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every white-space character removed.
    /// </summary>
    /// <param name="value">The string to strip. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new string containing only the non-white-space characters from <paramref name="value" />. When
    /// <paramref name="value" /> already contains no white-space, the original instance is returned unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// White-space is detected via <see cref="char.IsWhiteSpace(char)" /> so Unicode separators (NBSP, line
    /// terminators, tab) are all removed.
    /// </remarks>
    public static string RemoveWhitespace(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return value;

        StringBuilder? builder = null;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsWhiteSpace(c))
            {
                builder ??= new StringBuilder(value.Length).Append(value, 0, i);
                continue;
            }

            builder?.Append(c);
        }

        return builder is null ? value : builder.ToString();
    }
}
