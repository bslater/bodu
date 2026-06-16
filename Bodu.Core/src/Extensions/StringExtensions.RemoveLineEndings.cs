// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveLineEndings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every carriage-return (<c>\r</c>) and line-feed (<c>\n</c>) character
    /// removed.
    /// </summary>
    /// <param name="value">The string to strip. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new string containing none of the <c>\r</c> or <c>\n</c> characters from <paramref name="value" />. When
    /// <paramref name="value" /> already contains no line-ending characters, the original instance is returned
    /// unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Only ASCII CR and LF are removed. Other Unicode line separators (U+2028, U+2029, NEL U+0085, vertical tab
    /// U+000B, form feed U+000C) are preserved. Use <see cref="NormalizeLineEndings(string, string)" /> with an empty
    /// string when stripping all newline-equivalent characters is required.
    /// </remarks>
    public static string RemoveLineEndings(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return value;

        StringBuilder? builder = null;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c is '\r' or '\n')
            {
                builder ??= new StringBuilder(value.Length).Append(value, 0, i);
                continue;
            }

            builder?.Append(c);
        }

        return builder is null ? value : builder.ToString();
    }
}
