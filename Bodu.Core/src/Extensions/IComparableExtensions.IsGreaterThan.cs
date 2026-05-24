// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensions.IsGreaterThan.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class IComparableExtensions
{
    /// <summary>
    /// Determines whether a value is strictly greater than a specified reference value.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value to compare, which must implement <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="other">The reference value to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="value" /> is strictly greater than <paramref name="other" />;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// If <paramref name="other" /> is <see langword="null" />, the method returns <see langword="false" />.
    /// </remarks>
    public static bool IsGreaterThan<T>(this T value, T? other)
        where T : IComparable<T> =>
            other is not null && value.CompareTo(other) > 0;

    /// <summary>
    /// Determines whether a value is strictly greater than a specified reference value using a custom
    /// <see cref="IComparer{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the value to compare.</typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="other">The reference value to compare against.</param>
    /// <param name="comparer">The comparer to use for comparing values.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="value" /> is strictly greater than <paramref name="other" /> based on
    /// the specified comparer; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparer" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// If <paramref name="other" /> is <see langword="null" />, the method returns <see langword="false" />.
    /// </remarks>
    public static bool IsGreaterThan<T>(this T value, T? other, IComparer<T> comparer)
        where T : struct
    {
        ThrowHelper.ThrowIfNull(comparer);

        return other is not null && comparer.Compare(value, other.Value) > 0;
    }
}
