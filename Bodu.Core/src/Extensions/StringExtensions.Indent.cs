// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Indent.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with <paramref name="count" /> copies of <paramref name="indentChar" />
    /// prepended to every line.
    /// </summary>
    /// <param name="value">The source text. Must not be <see langword="null" />.</param>
    /// <param name="count">The number of indent characters to prepend per line. Must be non-negative.</param>
    /// <param name="indentChar">The character used to indent. Defaults to a regular space.</param>
    /// <returns>The indented text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <remarks>
    /// Line boundaries are recognised at <c>\r\n</c>, <c>\n</c>, and bare <c>\r</c>. Empty trailing lines (a trailing
    /// newline followed by nothing) are not indented. When <paramref name="count" /> is zero the input is returned
    /// unchanged.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "line1\nline2".Indent(2);  // "  line1\n  line2"
    ///]]>
    /// </code>
    /// </example>
    public static string Indent(this string value, int count, char indentChar = ' ')
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNegative(count);

        if (count == 0 || value.Length == 0) return value;

        string prefix = new string(indentChar, count);
        StringBuilder builder = new(value.Length + (count * 4));
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
