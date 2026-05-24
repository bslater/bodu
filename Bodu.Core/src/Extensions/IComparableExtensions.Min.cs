// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensions.Min.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class IComparableExtensions
{
    /// <summary>
    /// Returns the smaller of two values using their natural ordering.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the values to compare, which must implement <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>
    /// <paramref name="value" /> if it compares less than or equal to <paramref name="other" />; otherwise,
    /// <paramref name="other" />.
    /// </returns>
    /// <remarks>
    /// When the two values compare equal, <paramref name="value" /> is returned.
    /// </remarks>
    public static T Min<T>(this T value, T other)
        where T : IComparable<T> =>
            value.CompareTo(other) <= 0 ? value : other;

    /// <summary>
    /// Returns the smaller of two values using a custom <see cref="IComparer{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the values to compare.</typeparam>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <param name="comparer">The comparer to use for comparing values.</param>
    /// <returns>
    /// <paramref name="value" /> if it compares less than or equal to <paramref name="other" /> under the specified
    /// comparer; otherwise, <paramref name="other" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparer" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// When the two values compare equal under <paramref name="comparer" />, <paramref name="value" /> is returned.
    /// </remarks>
    public static T Min<T>(this T value, T other, IComparer<T> comparer)
    {
        ThrowHelper.ThrowIfNull(comparer);

        return comparer.Compare(value, other) <= 0 ? value : other;
    }
}
