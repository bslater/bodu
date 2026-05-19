// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every occurrence of <paramref name="valueToRemove" /> removed.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="valueToRemove">The substring to remove. Must not be <see langword="null" /> or empty.</param>
    /// <param name="comparison">
    /// The comparison rule used to locate occurrences of <paramref name="valueToRemove" />. Defaults to
    /// <see cref="StringComparison.Ordinal" />.
    /// </param>
    /// <returns>
    /// A new string with the matches removed. When <paramref name="valueToRemove" /> does not appear, the
    /// original instance is returned unchanged.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="valueToRemove" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="valueToRemove" /> is the empty string.
    /// </exception>
    /// <remarks>
    /// This is the substring counterpart to the BCL <see cref="string.Remove(int)" /> family, which is
    /// index-based. Use <see cref="string.TrimStart(char[])" />/<see cref="string.TrimEnd(char[])" /> when the
    /// goal is to strip individual characters.
    /// </remarks>
    public static string Remove(
        this string value,
        string valueToRemove,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(valueToRemove);
        if (valueToRemove.Length == 0) throw new ArgumentException("valueToRemove must not be empty.", nameof(valueToRemove));

        return value.Replace(valueToRemove, string.Empty, comparison);
    }
}
