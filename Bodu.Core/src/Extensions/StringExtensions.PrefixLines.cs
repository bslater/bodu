// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.PrefixLines.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with <paramref name="prefix" /> prepended to every line.
    /// </summary>
    /// <param name="value">The source text. Must not be <see langword="null" />.</param>
    /// <param name="prefix">The string to prepend to each line. Must not be <see langword="null" />.</param>
    /// <returns>The prefixed text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when either argument is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Line boundaries are recognised at <c>\r\n</c>, <c>\n</c>, and bare <c>\r</c>. An empty
    /// <paramref name="prefix" /> returns the input unchanged. Commonly used to add a comment marker to a
    /// block of source — e.g. <c>"line1\nline2".PrefixLines("// ")</c>.
    /// </remarks>
    public static string PrefixLines(this string value, string prefix)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(prefix);

        if (prefix.Length == 0 || value.Length == 0) return value;

        StringBuilder builder = new(value.Length + prefix.Length * 4);
        bool atLineStart = true;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (atLineStart)
            {
                builder.Append(prefix);
                atLineStart = false;
            }
            builder.Append(c);
            if (c == '\n')
            {
                atLineStart = true;
            }
            else if (c == '\r')
            {
                if (i + 1 < value.Length && value[i + 1] == '\n')
                {
                    builder.Append('\n');
                    i++;
                }
                atLineStart = true;
            }
        }
        return builder.ToString();
    }
}
