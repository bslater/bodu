// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Truncate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> truncated to at most <paramref name="maxLength" /> characters.
    /// </summary>
    /// <param name="value">The string to truncate. Must not be <see langword="null" />.</param>
    /// <param name="maxLength">The maximum length to retain. Must be non-negative.</param>
    /// <returns>
    /// <paramref name="value" /> when its length is at most <paramref name="maxLength" />; otherwise the
    /// first <paramref name="maxLength" /> characters.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength" /> is negative.
    /// </exception>
    public static string Truncate(this string value, int maxLength)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNegative(maxLength);

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    /// <summary>
    /// Returns <paramref name="value" /> truncated to at most <paramref name="maxLength" /> characters,
    /// appending <paramref name="ellipsis" /> to indicate truncation.
    /// </summary>
    /// <param name="value">The string to truncate. Must not be <see langword="null" />.</param>
    /// <param name="maxLength">
    /// The maximum total length to retain, including <paramref name="ellipsis" />. Must be at least
    /// <paramref name="ellipsis" />.Length.
    /// </param>
    /// <param name="ellipsis">
    /// The suffix appended when truncation occurs. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> when its length is at most <paramref name="maxLength" />; otherwise the
    /// first <c>maxLength - ellipsis.Length</c> characters followed by <paramref name="ellipsis" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="ellipsis" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength" /> is smaller than <paramref name="ellipsis" />.Length.
    /// </exception>
    public static string Truncate(this string value, int maxLength, string ellipsis)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(ellipsis);
        if (maxLength < ellipsis.Length) throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "maxLength must be at least ellipsis.Length.");

        if (value.Length <= maxLength) return value;
        return string.Concat(value.AsSpan(0, maxLength - ellipsis.Length), ellipsis);
    }
}
