// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.SliceSafe.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns the substring of <paramref name="value" /> starting at <paramref name="startIndex" />, clamping
    /// out-of-range indices instead of throwing.
    /// </summary>
    /// <param name="value">The source string. Must not be <see langword="null" />.</param>
    /// <param name="startIndex">The zero-based starting index. May be negative or beyond the end of the string.</param>
    /// <returns>
    /// The substring beginning at the clamped <paramref name="startIndex" />. Returns the original instance when
    /// <paramref name="startIndex" /> is zero or negative; returns <see cref="string.Empty" /> when
    /// <paramref name="startIndex" /> is greater than or equal to <paramref name="value" />.Length.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string SliceSafe(this string value, int startIndex)
    {
        ThrowHelper.ThrowIfNull(value);

        if (startIndex <= 0) return value;
        if (startIndex >= value.Length) return string.Empty;
        return value.Substring(startIndex);
    }

    /// <summary>
    /// Returns a substring of <paramref name="value" /> starting at <paramref name="startIndex" /> with the requested
    /// <paramref name="length" />, clamping out-of-range arguments instead of throwing.
    /// </summary>
    /// <param name="value">The source string. Must not be <see langword="null" />.</param>
    /// <param name="startIndex">The zero-based starting index. Negative values clamp to zero.</param>
    /// <param name="length">
    /// The maximum number of characters to take. Negative values produce an empty string. The actual length returned is
    /// clamped to the remaining characters in <paramref name="value" />.
    /// </param>
    /// <returns>
    /// The substring starting at the clamped <paramref name="startIndex" /> of at most <paramref name="length" />
    /// characters.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string SliceSafe(this string value, int startIndex, int length)
    {
        ThrowHelper.ThrowIfNull(value);

        if (length <= 0) return string.Empty;
        int start = startIndex < 0 ? 0 : startIndex;
        if (start >= value.Length) return string.Empty;
        int take = Math.Min(length, value.Length - start);
        if (start == 0 && take == value.Length) return value;
        return value.Substring(start, take);
    }
}
