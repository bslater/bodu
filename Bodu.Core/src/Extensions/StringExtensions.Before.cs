// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.Before.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns the substring of <paramref name="value" /> preceding the first occurrence of <paramref name="marker" />.
    /// </summary>
    /// <param name="value">The string to inspect. Must not be <see langword="null" />.</param>
    /// <param name="marker">The marker to locate. Must not be <see langword="null" />.</param>
    /// <param name="comparison">
    /// The comparison rule used to locate <paramref name="marker" />. Defaults to
    /// <see cref="StringComparison.Ordinal" />.
    /// </param>
    /// <returns>
    /// The characters before the first occurrence of <paramref name="marker" />, or <see langword="null" /> when
    /// <paramref name="marker" /> is not found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="marker" /> is <see langword="null" />.
    /// </exception>
    public static string? Before(
        this string value,
        string marker,
        StringComparison comparison = StringComparison.Ordinal)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(marker);

        var index = value.IndexOf(marker, comparison);
        return index < 0 ? null : value[..index];
    }
}
