// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensions.AtMost.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class IComparableExtensions
{
    /// <summary>
    /// Lowers a value to a specified inclusive upper bound, returning the bound when the value exceeds it.
    /// </summary>
    /// <typeparam name="T">The type of the value to coerce, which must implement <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to coerce.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>
    /// <paramref name="value"/> if it compares less than or equal to <paramref name="max"/>; otherwise, <paramref name="max"/>.
    /// </returns>
    /// <remarks>
    /// This is a one-sided form of <see cref="Clamp{T}(T, T?, T?)"/> for cases where only a ceiling is required.
    /// Equivalent to <c>Math.Min(value, max)</c> for numeric types.
    /// </remarks>
    public static T AtMost<T>(this T value, T max)
        where T : IComparable<T> =>
            value.CompareTo(max) <= 0 ? value : max;

    /// <summary>
    /// Lowers a value to a specified inclusive upper bound using a custom <see cref="IComparer{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to coerce.</typeparam>
    /// <param name="value">The value to coerce.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <param name="comparer">The comparer to use for comparing values.</param>
    /// <returns>
    /// <paramref name="value"/> if it compares less than or equal to <paramref name="max"/> under the specified comparer;
    /// otherwise, <paramref name="max"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is <see langword="null"/>.</exception>
    /// <remarks>This is a one-sided form of <see cref="Clamp{T}(T, T?, T?, IComparer{T})"/> for cases where only a ceiling is required.</remarks>
    public static T AtMost<T>(this T value, T max, IComparer<T> comparer)
    {
        ThrowHelper.ThrowIfNull(comparer);

        return comparer.Compare(value, max) <= 0 ? value : max;
    }
}
