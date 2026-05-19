// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensions.IsOutside.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class IComparableExtensions
{
    /// <summary>
    /// Determines whether a value falls outside the inclusive range defined by two specified boundaries.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value to compare, which must implement <see cref="IComparable{T}" />.
    /// </typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="value1">The first boundary.</param>
    /// <param name="value2">The second boundary.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="value" /> is less than the smaller boundary or greater than the larger boundary;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If any of the parameters are <c>null</c>, the method returns <c>false</c>.
    /// </para>
    /// <para>
    /// The order of <paramref name="value1" /> and <paramref name="value2" /> does not matter.
    /// </para>
    /// </remarks>
    public static bool IsOutside<T>(this T value, T? value1, T? value2)
        where T : IComparable<T> => value1 is not null && value2 is not null &&
            (value1.CompareTo(value2) > 0
                ? value.CompareTo(value2) < 0 || value.CompareTo(value1) > 0
                : value.CompareTo(value1) < 0 || value.CompareTo(value2) > 0);

    /// <summary>
    /// Determines whether a value falls outside the inclusive range defined by two specified boundaries using a custom
    /// <see cref="IComparer{T}" />.
    /// </summary>
    /// <typeparam name="T">The type of the value to compare.</typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="value1">The first boundary.</param>
    /// <param name="value2">The second boundary.</param>
    /// <param name="comparer">The comparer to use for comparing values.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="value" /> is less than the smaller boundary or greater than the larger boundary
    /// based on the specified comparer; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparer" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If any of the parameters are <c>null</c>, the method returns <c>false</c>.
    /// </para>
    /// <para>
    /// The order of <paramref name="value1" /> and <paramref name="value2" /> does not matter.
    /// </para>
    /// </remarks>
    public static bool IsOutside<T>(this T value, T? value1, T? value2, IComparer<T> comparer)
        where T : struct
    {
        ThrowHelper.ThrowIfNull(comparer);

        return value1 is null || value2 is null ? false
            : comparer.Compare(value1.Value, value2.Value) > 0
                ? comparer.Compare(value, value2.Value) < 0 || comparer.Compare(value, value1.Value) > 0
                : comparer.Compare(value, value1.Value) < 0 || comparer.Compare(value, value2.Value) > 0;
    }
}
