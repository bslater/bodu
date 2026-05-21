// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Outdent.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with up to <paramref name="count" /> leading occurrences of
    /// <paramref name="indentChar" /> stripped from each line.
    /// </summary>
    /// <param name="value">The source text. Must not be <see langword="null" />.</param>
    /// <param name="count">
    /// The maximum number of leading indent characters to remove per line. Must be non-negative.
    /// </param>
    /// <param name="indentChar">The indent character to strip. Defaults to a regular space.</param>
    /// <returns>The outdented text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <remarks>
    /// The inverse of <see cref="Indent(string, int, char)" />. Lines that contain fewer than <paramref name="count" />
    /// leading <paramref name="indentChar" /> characters are stripped of however many are present — no exception is
    /// raised. Line boundaries follow the same rules as <see cref="Indent(string, int, char)" /> (<c>\r\n</c>,
    /// <c>\n</c>, bare <c>\r</c>).
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "    line1\n    line2".Outdent(2);  // "  line1\n  line2"
    ///]]>
    /// </code>
    /// </example>
    public static string Outdent(this string value, int count, char indentChar = ' ')
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNegative(count);

        if (count == 0 || value.Length == 0) return value;

        StringBuilder builder = new(value.Length);
        int lineStart = 0;
        for (int i = 0; i <= value.Length; i++)
        {
            if (i == value.Length || value[i] == '\n' || value[i] == '\r')
            {
                AppendOutdentedLine(builder, value, lineStart, i, indentChar, count);
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

    /// <summary>
    /// Appends the substring <c>value[start..end]</c> to <paramref name="builder" /> with up to
    /// <paramref name="count" /> leading <paramref name="indentChar" /> characters removed.
    /// </summary>
    /// <param name="builder">The destination builder.</param>
    /// <param name="value">The source string.</param>
    /// <param name="start">The start of the line (inclusive).</param>
    /// <param name="end">The end of the line (exclusive — at the line break or end of string).</param>
    /// <param name="indentChar">The indent character to strip.</param>
    /// <param name="count">The maximum number of indent characters to strip.</param>
    private static void AppendOutdentedLine(
        StringBuilder builder,
        string value,
        int start,
        int end,
        char indentChar,
        int count)
    {
        int skipped = 0;
        int cursor = start;
        while (cursor < end && skipped < count && value[cursor] == indentChar)
        {
            cursor++;
            skipped++;
        }

        if (cursor < end) builder.Append(value, cursor, end - cursor);
    }
}
