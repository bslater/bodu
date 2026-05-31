// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensions.AtLeast.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class IComparableExtensions
{
    /// <summary>
    /// Raises a value to a specified inclusive lower bound, returning the bound when the value falls below it.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value to coerce, which must implement <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="value">The value to coerce.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <returns>
    /// <paramref name="value" /> if it compares greater than or equal to <paramref name="min" />; otherwise,
    /// <paramref name="min" />.
    /// </returns>
    /// <remarks>
    /// This is a one-sided form of <see cref="Clamp{T}(T, T?, T?)" /> for cases where only a floor is required.
    /// Equivalent to <c>Math.Max(value, min)</c> for numeric types.
    /// </remarks>
    public static T AtLeast<T>(this T value, T min)
        where T : IComparable<T> =>
            value.CompareTo(min) >= 0 ? value : min;

    /// <summary>
    /// Raises a value to a specified inclusive lower bound using a custom <see cref="IComparer{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the value to coerce.</typeparam>
    /// <param name="value">The value to coerce.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="comparer">The comparer to use for comparing values.</param>
    /// <returns>
    /// <paramref name="value" /> if it compares greater than or equal to <paramref name="min" /> under the specified
    /// comparer; otherwise, <paramref name="min" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparer" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This is a one-sided form of <see cref="Clamp{T}(T, T?, T?, IComparer{T})" /> for cases where only a floor is
    /// required.
    /// </remarks>
    public static T AtLeast<T>(this T value, T min, IComparer<T> comparer)
    {
        ThrowHelper.ThrowIfNull(comparer);

        return comparer.Compare(value, min) >= 0 ? value : min;
    }
}
