// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Between.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns the substring of <paramref name="value" /> between the first occurrence of
    /// <paramref name="start" /> and the next occurrence of <paramref name="end" /> after it.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="start">The opening marker. Must not be <see langword="null" />.</param>
    /// <param name="end">The closing marker. Must not be <see langword="null" />.</param>
    /// <param name="comparison">
    /// The comparison rule used to locate the markers. Defaults to <see cref="StringComparison.Ordinal" />.
    /// </param>
    /// <returns>
    /// The substring between the markers, or <see langword="null" /> when either marker is not present in
    /// the expected order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" />, <paramref name="start" />, or <paramref name="end" /> is
    /// <see langword="null" />.
    /// </exception>
    public static string? Between(
        this string value,
        string start,
        string end,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(start);
        ThrowHelper.ThrowIfNull(end);

        int startIndex = value.IndexOf(start, comparison);
        if (startIndex < 0) return null;

        int contentStart = startIndex + start.Length;
        int endIndex = value.IndexOf(end, contentStart, comparison);
        if (endIndex < 0) return null;

        return value.Substring(contentStart, endIndex - contentStart);
    }
}
