// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.TruncateMiddle.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> truncated to at most <paramref name="maxLength" /> characters by keeping the
    /// beginning and end intact and inserting <paramref name="separator" /> in the middle.
    /// </summary>
    /// <param name="value">The string to truncate. Must not be <see langword="null" />.</param>
    /// <param name="maxLength">
    /// The maximum total length to retain, including <paramref name="separator" />. Must be at least
    /// <paramref name="separator" />.Length.
    /// </param>
    /// <param name="separator">
    /// The marker inserted in place of the omitted middle. Defaults to the ellipsis character <c>"…"</c>. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> when its length is at most <paramref name="maxLength" />; otherwise a new string
    /// composed of the leading portion, <paramref name="separator" />, and the trailing portion of
    /// <paramref name="value" /> such that the total length equals <paramref name="maxLength" />. When the remaining
    /// budget cannot be evenly split, the extra character goes to the prefix.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="separator" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength" /> is smaller than <paramref name="separator" />.Length.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// "hello-world-foo-bar".TruncateMiddle(11);          // "hello…o-bar"
    /// "abcdefghij".TruncateMiddle(7, "...");             // "ab...ij"
    ///]]>
    /// </code>
    /// </example>
    public static string TruncateMiddle(this string value, int maxLength, string separator = "…")
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(separator);
        if (maxLength < separator.Length) throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, ResourceStrings.Arg_OutOfRange_MaxLengthLessThanMarker);

        if (value.Length <= maxLength) return value;

        int budget = maxLength - separator.Length;
        int prefixLength = (budget + 1) / 2;
        int suffixLength = budget - prefixLength;
        return string.Concat(
            value.AsSpan(0, prefixLength),
            separator,
            value.AsSpan(value.Length - suffixLength, suffixLength));
    }
}
