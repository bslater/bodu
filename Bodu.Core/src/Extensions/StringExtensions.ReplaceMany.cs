// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ReplaceMany.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every key from <paramref name="replacements" /> replaced by its
    /// associated value, applied sequentially in dictionary enumeration order under ordinal comparison.
    /// </summary>
    /// <param name="value">The string to transform. Must not be <see langword="null" />.</param>
    /// <param name="replacements">
    /// The map of search-and-replace pairs. Must not be <see langword="null" />; keys must not be
    /// <see langword="null" /> or empty; values may be <see langword="null" /> or empty.
    /// </param>
    /// <returns>
    /// A new string with all replacements applied. When <paramref name="replacements" /> is empty, the original
    /// instance is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="replacements" /> is <see langword="null" />, or when
    /// any key in <paramref name="replacements" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any key in <paramref name="replacements" /> is the empty string.
    /// </exception>
    /// <remarks>
    /// Replacements are applied sequentially — output of an earlier replacement is visible to a later one. Pre-order
    /// keys to avoid cascading substitutions when independence is required.
    /// </remarks>
    public static string ReplaceMany(this string value, IReadOnlyDictionary<string, string> replacements)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(replacements);

        if (replacements.Count == 0) return value;

        string current = value;
        foreach (KeyValuePair<string, string> pair in replacements)
        {
            ThrowHelper.ThrowIfNull(pair.Key);
            if (pair.Key.Length == 0) throw new ArgumentException("Replacement keys must not be empty.", nameof(replacements));
            current = current.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
        }

        return current;
    }
}
