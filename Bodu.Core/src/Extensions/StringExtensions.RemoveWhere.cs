// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveWhere.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with every character for which <paramref name="predicate" /> returns
    /// <see langword="true" /> removed.
    /// </summary>
    /// <param name="value">The string to filter. Must not be <see langword="null" />.</param>
    /// <param name="predicate">The selector evaluated for each character. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A new string with the matching characters removed. When no character matches, the original instance is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    public static string RemoveWhere(this string value, Func<char, bool> predicate)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(predicate);

        return value.KeepWhere(c => !predicate(c));
    }
}
