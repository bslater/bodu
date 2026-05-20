// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.EnsureEndsWith.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> guaranteed to end with <paramref name="suffix" />, appending the suffix only
    /// when it is not already present.
    /// </summary>
    /// <param name="value">The string to qualify. Must not be <see langword="null" />.</param>
    /// <param name="suffix">The suffix that the result must end with. Must not be <see langword="null" />.</param>
    /// <param name="comparison">
    /// The comparison rule used to test whether <paramref name="value" /> already ends with <paramref name="suffix" />.
    /// Defaults to <see cref="StringComparison.Ordinal" />.
    /// </param>
    /// <returns>
    /// <paramref name="value" /> when it already ends with <paramref name="suffix" /> under
    /// <paramref name="comparison" />; otherwise a new string equal to <paramref name="value" /> concatenated with
    /// <paramref name="suffix" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="suffix" /> is <see langword="null" />.
    /// </exception>
    public static string EnsureEndsWith(
        this string value,
        string suffix,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(suffix);

        return value.EndsWith(suffix, comparison) ? value : value + suffix;
    }
}
